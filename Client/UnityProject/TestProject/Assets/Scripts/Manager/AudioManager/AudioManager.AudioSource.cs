using System;
using UnityEngine;

namespace Fuel.Manager.AudioManager
{

    public sealed partial class AudioManager
    {
        public sealed class AudioSourceData
        {
            private AudioType _audioType;

            private readonly AudioSource _as;
            public AudioSource As => _as;

            private AudioClip _clip;
            private float _fadeSeconds;//声音淡入淡出时间，以秒为单位，0不淡入淡出

            /// <summary>
            /// 当前设置的音量
            /// </summary>
            private float _volume;

#if UNITY_EDITOR
            public float Volume => _volume;
#endif
            /// <summary>
            /// 标记脏回收
            /// </summary>
            public bool IsDirty;
            /// <summary>
            /// 淡入后淡出前的音量
            /// </summary>
            private float _fadeOffsetVolume;
            /// <summary>
            /// 淡入淡出的目标音量
            /// </summary>
            private float _fadeTargetVolume;
            /// <summary>
            /// 播放完成回调
            /// </summary>
            private Action _onComplete;
            /// <summary>
            /// 是否需要fade
            /// </summary>
            private bool _isSetVolume;
            /// <summary>
            /// 区分淡入淡出
            /// </summary>
            private int _fadeDir;
            /// <summary>
            /// 非循环音频的回收时间
            /// </summary>
            private float _recycleTime; // 回收时间
            /// <summary>
            /// 停止淡入淡出的完成回调
            /// </summary>
            private Action _onStopFadeFinish;
            /// <summary>
            /// 当前音频状态
            /// </summary>
            public AudioSourceState State;

            private bool m_loop;
            public int InstanceID{get; private set; }

            public AudioSourceData(AudioSource @as, int instanceID)
            {
                ResetInnerData();
                _as = @as;
                InstanceID = instanceID;
            }
            /// <summary>
            /// 重置内部参数
            /// </summary>
            private void ResetInnerData()
            {
                _fadeSeconds = 0;
                _volume = 0;
                _fadeOffsetVolume = 0;
                _onComplete = null;
                _isSetVolume = false;
                _fadeDir = 0;
                _recycleTime = 0;
                m_loop = false;
                State = AudioSourceState.None;
            }
            
            /// <summary>
            /// 释放资源
            /// </summary>
            public void Dispose()
            {
                _as.Stop();
                _as.clip = null;
                GameObject.Destroy(_as);
            }

            #region 控制接口
            /// <summary>
            /// 播放音频
            /// </summary>
            /// <param name="clip">音频资源</param>
            /// <param name="volume">声音大小</param>
            /// <param name="soundParams">音频额外恒定参数</param>
            /// <param name="fadeSeconds">淡入淡出时长</param>
            /// <param name="onComplete">播放完成回调</param>
            public void Play(AudioClip clip, float volume, SoundParams soundParams, float fadeSeconds = 0, Action onComplete = null)
            {
                State = AudioSourceState.Playing;
                IsDirty = false;
                _clip = clip;
                _audioType = soundParams.Type;
                _onComplete = onComplete;
                m_loop = soundParams.IsLoop;
                _as.volume = 0;
                _as.clip = _clip;
                _as.loop = m_loop;
                _as.spatialBlend = soundParams.SpatialBlend;
                _as.minDistance = soundParams.MinDistance;
                _as.maxDistance = soundParams.MaxDistance;
                _fadeOffsetVolume = volume;
                _fadeTargetVolume = volume;
                switch (_audioType)
                {
                    case AudioType.BGM:
                    case AudioType.BGS:
                    case AudioType.ME:
                        _as.Play();
                        break;
                    case AudioType.SE:
                        _as.PlayOneShot(_clip);
                        break;
                }
                SetVolume(volume, fadeSeconds);
                if (!soundParams.IsLoop)
                {
                    if (_audioType == AudioType.SE)
                    {
                        _recycleTime = _clip.length + 0.05f;
                    }
                    else
                    {
                        _recycleTime = _clip.length + 0.1f;
                    }
                }
                else
                {
                    _recycleTime = 0;
                }
            }
            /// <summary>
            /// 停止播放
            /// </summary>
            /// <param name="fadeTime"></param>
            /// <param name="fadeFinish">完成回调</param>
            public void Stop(float fadeTime = 0,Action fadeFinish = null)
            {
                _onStopFadeFinish = fadeFinish;
                _fadeTargetVolume = 0;
                _fadeOffsetVolume = _volume;
                State = AudioSourceState.Stopped;
                if (fadeTime == 0)
                {
                    if (_as != null)
                        _as.Stop();
                    _onStopFadeFinish?.Invoke();
                    IsDirty = true;
                }
                else
                {
                    AutoSetFade(_fadeTargetVolume, fadeTime);
                }
            }
            /// <summary>
            /// 静音
            /// </summary>
            /// <param name="bMute">true:静音 false:取消静音 </param>
            /// <param name="fadeTime">淡入淡出时间，以秒为单位，0不淡入淡出 </param>
            public void Mute(bool bMute, float fadeTime = 0)
            {
                if (bMute)
                {
                    _fadeTargetVolume = 0;
                    AutoSetFade(_fadeTargetVolume, fadeTime);
                }
                else
                {
                    _fadeTargetVolume = _volume;
                    AutoSetFade(_fadeTargetVolume, fadeTime);
                }
            }

            /// <summary>
            /// 暂停
            /// </summary>
            public void Pause(float fadeTime = 0)
            {
                if (State == AudioSourceState.Paused|| State == AudioSourceState.None)
                {
                    return;
                }
                State = AudioSourceState.Paused;
                if (fadeTime == 0)
                {
                    _as.Pause();
                }
                _fadeTargetVolume = 0;
                AutoSetFade(_fadeTargetVolume, fadeTime);
            }

            /// <summary>
            /// 继续播放
            /// </summary>
            public void UnPause(float fadeTime = 0)
            {
                _fadeTargetVolume = _volume;
                State = AudioSourceState.Playing;
                if (_as != null)
                {
                    _as.UnPause();
                }
                AutoSetFade(_fadeTargetVolume, fadeTime);
            }
            #endregion
            /// <summary>
            /// 渐入渐出回调
            /// </summary>
            private void OnFadeComplete()
            {
                _isSetVolume = false;
                switch (State)
                {
                    case AudioSourceState.Paused:
                        _as.Pause();
                        break;
                    case AudioSourceState.Stopped:
                        _as.Stop();
                        IsDirty = true;
                        _onStopFadeFinish?.Invoke();
                        break;
                }
            }
            
            /// <summary>
            /// 自动处理淡入淡出
            /// </summary>
            /// <param name="volume">目标音量</param>
            /// <param name="fadeTime">淡入淡出时间，以秒为单位，0不淡入淡出</param>
            private void AutoSetFade(float volume, float fadeTime)
            {
                _fadeSeconds = fadeTime;
                if (fadeTime == 0)
                {
                    _isSetVolume = false;
                    if (_as)
                    {
                        _as.volume = volume;
                    }
                    OnFadeComplete();
                }
                else
                {
                    _isSetVolume = true;
                    _fadeDir = _as.volume - volume > 0 ? -1 : 1;
                }
            }

            /// <summary>
            /// 设置音量
            /// </summary>
            /// <param name="volume">目标音量</param>
            /// <param name="fadeTime">淡入淡出时间，以秒为单位，0不淡入淡出</param>
            public void SetVolume(float volume, float fadeTime)
            {
                _volume = volume;
                _fadeTargetVolume  = volume;
                if (State == AudioSourceState.Paused || State == AudioSourceState.Stopped || _as.volume.Equals(volume))
                {
                    return;
                }
                AutoSetFade(_fadeTargetVolume, fadeTime);
            }
            
            /// <summary>
            /// 处理渐变
            /// </summary>
            private void TickFade(float dt)
            {
                if (!_isSetVolume || State == AudioSourceState.None|| _fadeSeconds == 0 ) return;
                _as.volume += _fadeOffsetVolume / _fadeSeconds * dt * _fadeDir;
                if (_fadeDir > 0)//渐入
                {
                    if (_as.volume >= _fadeTargetVolume)
                    {
                        _as.volume = _fadeTargetVolume;
                        _fadeSeconds = 0;
                        OnFadeComplete();
                    }
                }
                else if (_fadeDir < 0)//渐出
                {
                    if (_as.volume <= _fadeTargetVolume)
                    {
                        _as.volume = _fadeTargetVolume;
                        _fadeSeconds = 0;
                        OnFadeComplete();
                    }
                }
            }
            /// <summary>
            /// 处理回收
            /// </summary>
            private void TickRecycle(float dt)
            {
                if (IsDirty||m_loop) return;
                if(_recycleTime < 0)
                {
                    _recycleTime = 0;
                    IsDirty = true;
                    _onComplete?.Invoke();
                    return;
                }
                _recycleTime -= dt;
            }

            public void Update(float dt)
            {
                TickFade(dt);
                TickRecycle(dt);
            }
        }
    }
}