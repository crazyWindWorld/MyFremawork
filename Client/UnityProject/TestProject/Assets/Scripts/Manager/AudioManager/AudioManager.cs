using System;
using System.Collections.Generic;
using UnityEngine;
using Fuel.Singleton;

namespace Fuel.Manager.AudioManager
{
    public sealed partial class AudioManager : Singleton<AudioManager>
    {
        private const string AudioPath ="Audio/";
        /// <summary>
        /// 所有缓存的clip
        /// </summary>
        private readonly Dictionary<string, AudioClip> _allClips = new Dictionary<string, AudioClip>();
#if UNITY_EDITOR
        public Dictionary<string, AudioClip> AllClips => _allClips;
#endif
        /// <summary>
        /// 当前BGM
        /// </summary>
        private AudioSourceData _bgm = null;
#if UNITY_EDITOR
        public AudioSourceData BGM => _bgm;
#endif
        private string _bgmPath;
        
        private GoAudioSource _bgsGoAudioSource = null;
#if UNITY_EDITOR
        public GoAudioSource BgsGoAudioSource => _bgsGoAudioSource;
#endif 
        
        /// <summary>
        /// 用于go的sound
        /// </summary>
        private readonly Dictionary<int, GoAudioSource> _goSources = new Dictionary<int, GoAudioSource>();

        public Dictionary<int, GoAudioSource> GoSources => _goSources;

        private GameObject _bgmRoot;
        private GameObject _bgsRoot;
        private GameObject _soundSoundEffectRoot;
        private static int _autoID = 0;
        public static int AutoID
        {
            get => _autoID;
            private set
            {
                _autoID = Mathf.Clamp(value, 0, int.MaxValue);
                _autoID = _autoID == int.MaxValue ? 0 : _autoID;
            }
        }
        #region 音量控制

        /// <summary>
        /// 音效音量
        /// </summary>
        private float _soundVolume = 1;
        public float SoundVolume=> _soundVolume;
        private float _tmpMusicVolume;
        /// <summary>
        /// 音乐音量
        /// </summary>
        private float _musicVolume = 1;
        public float MusicVolume => _musicVolume;

        /// <summary>
        /// 音乐静音
        /// </summary>
        bool m_musicMute = false;

        /// <summary>
        /// 音乐静音
        /// </summary>
        public bool MusicMute
        {
            get => m_musicMute;
            set
            {
                m_musicMute = value;
                Mute(AudioType.BGM, m_musicMute);
            }
        }

        /// <summary>
        /// 音乐是否开启
        /// </summary>
        public bool IsMusicOn
        {
            set => _musicVolume = value ? 1 : 0;
        }
        #endregion

        protected override void Init()
        {
            var parent = GameObject.Find("MonoSingles");
            if (parent == null)
            {
                parent = new GameObject("MonoSingles");
                GameObject.DontDestroyOnLoad(parent);
            }

            var audioManager = new GameObject("AudioManager");
            if (parent != null)
            {
                audioManager.transform.parent = parent.transform;
            }
            
            _bgmRoot = new GameObject("BGM")
            {
                transform =
                {
                    parent = audioManager.transform
                }
            };
            _bgmRoot.AddComponent<AudioSource>();

            _bgsRoot = new GameObject("BGS")
            {
                transform =
                {
                    parent = audioManager.transform
                }
            };
            _bgsRoot.AddComponent<AudioSource>();

            _soundSoundEffectRoot = new GameObject("MusicEffectAndSoundEffect")
            {
                transform =
                {
                    parent = audioManager.transform
                }
            };
        }

        public void Dispose()
        {
            GameObject.Destroy(_bgmRoot);
            GameObject.Destroy(_bgsRoot);
            GameObject.Destroy(_soundSoundEffectRoot);
            _bgm.Dispose();
            _bgsGoAudioSource.Dispose();

            foreach (var source in _goSources)
            {
                source.Value.Dispose();
            }
            foreach (var item in _allClips)
            {
                GameObject.Destroy(item.Value);
            }
            _allClips.Clear();
        }
        /// <summary>
        /// 获取audioClip文件
        /// </summary>
        /// <param name="clipName"></param>
        /// <param name="cb"></param>
        private void GetClip(string clipName, Action<AudioClip> cb)
        {
            if (clipName.StartsWith(AudioPath))
            {
                Debug.LogWarning($"路径错误，请修改对应配置：{clipName}为：{clipName.Replace(AudioPath, "")}");
                clipName = clipName.Replace(AudioPath, ""); 
            }
            if (_allClips.TryGetValue(clipName, out AudioClip audioClipExist))
            {
                cb?.Invoke(audioClipExist);
            }
            else
            {
                var clip = GetAudioClip(clipName);
                if (clip == null)
                {
                    Debug.LogError($"路径错误，{clipName} 没找到对应的sound");
                    cb?.Invoke(null);
                }
                cb?.Invoke(clip);
            }
        }

        /// <summary>
        /// 资源引用
        /// </summary>
        /// <param name="clipName">资源相对路径</param>
        private AudioClip GetAudioClip(string clipName)
        {
            string audioPath = AudioPath + clipName;
            AudioClip audioClipNew = Resources.Load<AudioClip>(audioPath);
            if (audioClipNew != null)
            {
                _allClips.TryAdd(clipName, audioClipNew);
            }
            return audioClipNew;
        }

        /// <summary>
        /// 获取音效 AsData
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private AudioSourceData GetSoundSourceData(GameObject go)
        {
            var instID = go.GetInstanceID();
            _goSources.TryGetValue(instID, out var goAudio);
            if (goAudio == null)
            {
                goAudio = new GoAudioSource(go);
                _goSources.Add(instID, goAudio);
            }
            return goAudio.GetAudioSourceData();
        }
        
        /// <summary>
        /// 静音
        /// </summary>
        private void Mute(AudioType type, bool bMute)
        {
            switch (type)
            {
                case AudioType.BGM:
                    _bgm?.Mute(bMute);
                    break;
                case AudioType.BGS:
                    _bgsGoAudioSource?.Mute(bMute);
                    break;
                case AudioType.ME:
                case AudioType.SE:
                    foreach (var goSource in _goSources)
                    {
                        goSource.Value.Mute(bMute);
                    }
                    break;
            }
        }
        /// <summary>
        /// 暂停
        /// </summary>
        /// <param name="type">音频类型</param>
        /// <param name="fadeTime">淡入淡出时间</param>
        private void Pause(AudioType type,float fadeTime = 0)
        {
            switch (type)
            {
                case AudioType.BGM:
                    _bgm.Pause(fadeTime);
                    break;
                case AudioType.BGS:
                    _bgsGoAudioSource.Pause(fadeTime);
                    break;
                case AudioType.ME:
                case AudioType.SE:
                    foreach (var goSource in _goSources)
                    {
                        goSource.Value.Pause(fadeTime);
                    }
                    break;
            }
        }
        /// <summary>
        /// 恢复播放
        /// </summary>
        /// <param name="type">音频类型</param>
        /// <param name="fadeTime">淡入淡出时间</param>
        private void UnPause(AudioType type, float fadeTime = 0)
        {
            switch (type)
            {
                case AudioType.BGM:
                    _bgm.UnPause(fadeTime);
                    break;
                case AudioType.BGS:
                    _bgsGoAudioSource.UnPause(fadeTime);
                    break;
                case AudioType.ME:
                case AudioType.SE:
                    foreach (var goSource in _goSources)
                    {
                        goSource.Value.UnPause(fadeTime);
                    }
                    break;
            }
        }
        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="type">音频类型</param>
        /// <param name="fadeTime">淡入淡出时间</param>
        private void Stop(AudioType type, float fadeTime)
        {
            switch (type)
            {
                case AudioType.BGM:
                    _bgm.Stop(fadeTime);
                    break;
                case AudioType.BGS:
                    _bgsGoAudioSource.StopAll(fadeTime);
                    break;
                case AudioType.ME:
                case AudioType.SE:
                    foreach (var item in _goSources)
                    {
                        if (item.Key == _soundSoundEffectRoot.GetInstanceID())
                        {
                            item.Value.ClearRes(3);
                        }
                        else
                        {
                            item.Value.ClearRes();
                        }
                    }
                    ClearClip();
                    break;
            }
        }
        
        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="type">音频类型</param>
        /// <param name="volume">音量大小</param>
        /// <param name="fadeTime">淡入淡出时间</param>
        private void SetVolume(AudioType type, float volume, float fadeTime)
        {
            switch (type)
            {
                case AudioType.BGM:
                    _bgm?.SetVolume(volume, fadeTime);
                    break;
                case AudioType.BGS:
                    _bgsGoAudioSource?.SetVolume(volume, fadeTime);
                    break;
                case AudioType.ME:
                case AudioType.SE:
                    foreach (var goSource in _goSources)
                    {
                        goSource.Value.SetVolume(volume,fadeTime);
                    }
                    break;
            }
        }

        private void ClearClip()
        {
            List<string> clipPathsList = new List<string>();
            foreach (var clipPath in _allClips)
            {
                if (clipPath.Key != _bgmPath)
                {
                    clipPathsList.Add(clipPath.Key);
                }
            }
            foreach (var clipPath in clipPathsList)
            {
                GameObject.Destroy(_allClips[clipPath]);
                _allClips.Remove(clipPath);
            }
        }
        #region 音频控制公共管理接口

        /// <summary>
        /// 暂停BGM
        /// </summary>
        public void PauseBGM(float fadeTime = 0)
        {
            Pause(AudioType.BGM, fadeTime);
        }

        /// <summary>
        /// 暂停BGS
        /// </summary>
        public void PauseBgs(float fadeTime = 0)
        {
            Pause(AudioType.BGS, fadeTime);
        }

        /// <summary>
        /// 继续播放BGM
        /// </summary>
        public void UnPauseBGM(float fadeTime = 0)
        {
            UnPause(AudioType.BGM, fadeTime);
        }

        /// <summary>
        /// 继续播放BGS
        /// </summary>
        public void UnPauseBgs(float fadeTime = 0)
        {
            UnPause(AudioType.BGS, fadeTime);
        }

        /// <summary>
        /// 静音BGM
        /// </summary>
        public void MuteBGM()
        {
            Mute(AudioType.BGM, true);
        }

        /// <summary>
        /// 静音BGS
        /// </summary>
        public void MuteBgs()
        {
            Mute(AudioType.BGS, true);
        }

        /// <summary>
        /// 停止播放BGM
        /// </summary>
        /// <param name="fadeSeconds">淡出时长</param>
        /// <param name="fadeFinish">完成回调</param>
        public void StopBGM(float fadeSeconds, Action fadeFinish = null)
        {
            _bgmPath = string.Empty;
            if (_bgm != null)
                _bgm.Stop(fadeSeconds, fadeFinish);
            else
            {
                fadeFinish?.Invoke();
            }
        }

        /// <summary>
        /// 停止播放BGS
        /// </summary>
        /// <param name="instanceId">实例Id</param>
        /// <param name="fadeSeconds">淡入淡出时间</param>
        /// <param name="fadeFinish">完成时间</param>
        public void StopBgs(int instanceId,float fadeSeconds = 0,Action fadeFinish = null)
        {
            if (_bgsGoAudioSource == null || !_bgsGoAudioSource.StopByInstanceId(instanceId,fadeSeconds,fadeFinish)||instanceId == -1)
            {
                fadeFinish?.Invoke();
            }
        }

        /// <summary>
        /// 停止播放所有BGS
        /// </summary>
        /// <param name="fadeSeconds">淡入淡出时间</param>
        public void StopBgs(float fadeSeconds = 0)
        {
            if (_bgsGoAudioSource != null)
            {
                _bgsGoAudioSource.StopAll(fadeSeconds);
            }
        }

        /// <summary>
        /// 设置BGM音量
        /// </summary>
        /// <param name="volume">音量</param>
        /// <param name="fadeSeconds">淡入时长</param>
        public void SetBGMVolume(float volume, float fadeSeconds = 0)
        {
            _musicVolume = Mathf.Clamp01(volume);
            SetVolume(AudioType.BGM, volume, fadeSeconds);
        }

        /// <summary>
        /// 设置BGM音量
        /// </summary>
        /// <param name="volume">音量</param>
        /// <param name="fadeSeconds">淡入时长</param>
        public void SetBGSVolume(float volume, float fadeSeconds = 0)
        {
            _musicVolume = Mathf.Clamp01(volume);
            SetVolume(AudioType.BGS, volume, fadeSeconds);
        }

        /// <summary>
        /// 设置背景音乐和背景音效音量
        /// </summary>
        public void SetBGMAndBGSVolume(float volume, float fadeSeconds = 0)
        {
            _musicVolume = Mathf.Clamp01(volume);
            SetVolume(AudioType.BGM, volume, fadeSeconds);
            SetVolume(AudioType.BGS, volume, fadeSeconds);
        }

        /// <summary>
        /// 暂停音效
        /// </summary>
        public void PauseSound(float fadeSeconds = 0)
        {
            Pause(AudioType.SE, fadeSeconds);
        }

        /// <summary>
        /// 继续播放音效
        /// </summary>
        public void UnPauseSound(float fadeSeconds = 0)
        {
            UnPause(AudioType.SE, fadeSeconds);
        }

        /// <summary>
        /// 静音音效
        /// </summary>
        public void MuteSound()
        {
            Mute(AudioType.SE, true);
        }

        /// <summary>
        /// 关闭音效
        /// </summary>
        public void StopSound()
        {
            Stop(AudioType.SE, 0);
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="volume">音量</param>
        /// <param name="fadeTime">淡入淡出时长</param>
        public void SetSoundVolume(float volume,float fadeTime = 0)
        {
            _soundVolume = Mathf.Clamp01(volume);
            SetVolume(AudioType.SE, volume, fadeTime);
        }

        /// <summary>
        /// 停止所有声音
        /// </summary>
        /// <param name="fadeSeconds"></param>
        public void SetAllStop(float fadeSeconds)
        {
            Stop(AudioType.BGM, fadeSeconds);
            Stop(AudioType.BGS, fadeSeconds);
            Stop(AudioType.ME, 0);
            Stop(AudioType.SE, 0);
        }

        /// <summary>
        /// 静音所有
        /// </summary>
        public void SetAllMute()
        {
            Mute(AudioType.BGM, true);
            Mute(AudioType.BGS, true);
            Mute(AudioType.ME, true);
            Mute(AudioType.SE, true);
        }

        /// <summary>
        /// 取消所有静音
        /// </summary>
        public void CancelAllMute()
        {
            Mute(AudioType.BGM, true);
            Mute(AudioType.BGS, true);
            Mute(AudioType.ME, true);
            Mute(AudioType.SE, true);
        }

        /// <summary>
        /// 设置所有声音音量
        /// </summary>
        /// <param name="volume">音量大小</param>
        /// <param name="fadeSeconds">渐入时长</param>
        public void SetAllVolume(float volume, float fadeSeconds = 0)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _soundVolume = Mathf.Clamp01(volume);
            SetVolume(AudioType.BGM, volume, fadeSeconds);
            SetVolume(AudioType.BGS, volume, fadeSeconds);
            SetVolume(AudioType.ME, volume, 0);
            SetVolume(AudioType.SE, volume, 0);
        }
        
        /// <summary>
        /// 停止播放音频
        /// </summary>
        /// <param name="instanceId">实例id</param>
        /// <param name="fadeTime">渐出时长</param>
        /// <param name="onFaceComponent">完成回调</param>
        public void StopByInstanceId(int instanceId, float fadeTime, Action onFaceComponent)
        {
            foreach (var item in _goSources)
            {
                if (item.Value.StopByInstanceId(instanceId, fadeTime, onFaceComponent))
                {
                    return;
                }
            }
        }
        #endregion
        
        /// <summary>
        /// 播放Bgm音乐
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="fadeSeconds">渐入渐出时间</param>
        /// <param name="onComplete">回收时回调</param>
        /// <param name="replay">如果正在播放相同的音乐，是否重新播放</param>
        public void PlayBGM(string path, float fadeSeconds = 0, Action onComplete = null,bool replay = false)
        {
            if (string.Equals(_bgmPath, path)&& !replay)
            {
                return;
            }
            AudioClipData sound = new AudioClipData
            {
                Path = path,
                PlayParams =
                {
                    Type = AudioType.BGM,
                    IsLoop = true
                }
            };
            if (sound.PlayParams.Type == AudioType.BGM)
            {
                StopBGM(fadeSeconds, () =>
                {
                    _bgm ??= new AudioSourceData(_bgmRoot.GetComponent<AudioSource>(), -1);
                    GetClip(sound.Path, (clip) =>
                    {
                        _bgmPath = path;
                        _bgm.Play(clip, _musicVolume, sound.PlayParams, fadeSeconds, onComplete);
                    });
                });
            }
            else
            {
                Debug.LogError($"AudioCategory == {sound.PlayParams.Type}. PlayBGM Type isn't BGM");
            }
        }

        /// <summary>
        /// 播放Bgs背景音乐
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="fadeSeconds">淡入淡出时间</param>
        /// <param name="onlyOne">只存在一个BGS</param>
        public int PlayBgs(string path, float fadeSeconds = 0,bool onlyOne = true)
        {
            if (onlyOne)
                StopBgs(fadeSeconds);
            AudioClipData audioClipData = new AudioClipData
            {
                Path = path,
                PlayParams =
                {
                    Type = AudioType.BGS,
                    IsLoop = true
                }
            };
            int instanceId = -1;
            GetClip(path, (clip) =>
            {
                if (_bgsGoAudioSource == null)
                {
                    _bgsGoAudioSource = new GoAudioSource(_bgsRoot);
                }

                var sourceData = _bgsGoAudioSource.GetAudioSourceData();
                instanceId = sourceData.InstanceID;
                sourceData.Play(clip,_soundVolume,audioClipData.PlayParams,fadeSeconds);
            });
            return instanceId;
        }
        
        /// <summary>
        /// 播放音效2D
        /// </summary>
        /// <param name="soundParams">音效参数</param>
        /// <param name="fadeSeconds">fade时间</param>
        /// <param name="onComplete">播放完成回调</param>
        /// <returns></returns>
        private int PlaySound(AudioClipData soundParams, float fadeSeconds = 0, Action onComplete = null)
        {
            var audioClip = soundParams;
            AudioSourceData source = null;
            if (audioClip.PlayParams.Type != AudioType.ME&& audioClip.PlayParams.Type != AudioType.SE)
            {
                Debug.LogError($"AudioType == {audioClip.PlayParams.Type}. PlaySound Type isn't ME or SE");
                return -1;
            }
            GetClip(audioClip.Path, (clip) =>
            {
                if (clip==null)
                {
                    Debug.LogError($"{audioClip.Path}没有该资源");
                    return;
                }
                source = GetSoundSourceData(_soundSoundEffectRoot);
                source.Play(clip, _soundVolume, audioClip.PlayParams, fadeSeconds, onComplete);
            });
            if (source == null)
            {
                return -1;
            }
            return source.InstanceID;
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="fadeSeconds">淡入淡出时间</param>
        /// <param name="onComplete">回收时回调</param>
        /// <param name="isLoop">是否循环播放</param>
        /// <returns></returns>
        public int PlaySound(string path, float fadeSeconds = 0, Action onComplete = null,bool isLoop = false)
        {
            AudioClipData audioClipData = new AudioClipData
            {
                Path = path,
                PlayParams =
                {
                    Type = AudioType.SE,
                    IsLoop = isLoop
                }
            };
            return PlaySound(audioClipData, fadeSeconds,onComplete); 
        }

    
        #region TODO 临时接口 后续需要优化
        public int PlayAudioByAs(AudioSource audioSource, string clipName,float fadeTime = 0, Action onComplete = null)
        {
            if (audioSource?.gameObject == null)
            {
                return -1;
            }
            SoundParams soundParams = new SoundParams
            {
                Type = AudioType.SE,
                IsLoop = false
            };
            AudioSourceData source = null;
            source = GetSoundSourceData(audioSource.gameObject);
            GetClip(clipName, (clip) =>
            {
                source.Play(clip, _soundVolume, soundParams, fadeTime, onComplete);
            });
            return source.InstanceID;
        }
        /// <summary>
        /// 一次性播放一段音效
        /// </summary>
        /// <param name="audioSource"></param>
        /// <param name="clipName"></param>
        public void PlayAudioOneShot(AudioSource audioSource, string clipName)
        {
            //PlayAudioByAS( audioSource,  clipName);
            if (audioSource == null)
                PlaySound(clipName);
            else
            {
                GetClip(clipName, (clip) =>
                {
                    audioSource.loop = false;
                    audioSource.volume = _soundVolume;
                    audioSource.PlayOneShot(clip);
                });
            }
        }

        /// <summary>
        /// 一次性播放一段音效
        /// </summary>
        /// <param name="audioSource"></param>
        /// <param name="clip"></param>
        /// <param name="process">快进时长</param>
        public void PlayAudioOneShot(AudioSource audioSource, AudioClip clip,float process = 0)
        {
            if (audioSource == null)
                return;
            audioSource.loop = false;
            audioSource.volume = _soundVolume;
            audioSource.PlayOneShot(clip);
            audioSource.time = process;
        }
        #endregion

        #region 临时降低音乐音量
        /// <summary>
        /// 临时设置音乐音量
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="fadeSeconds"></param>
        public void SetTempMusicVolume(float volume, float fadeSeconds = 2)
        {
            _tmpMusicVolume = _musicVolume;
            _musicVolume = volume;
            SetVolume(AudioType.BGM, volume, fadeSeconds);
        }
        /// <summary>
        /// 恢复音乐音量
        /// </summary>
        /// <param name="fadeSeconds"></param>
        public void ResetTempMusicVolume(float fadeSeconds = 2)
        {
            _musicVolume = _tmpMusicVolume;
            SetVolume(AudioType.BGM, _musicVolume, fadeSeconds);
        }
        #endregion

        #region 场景物体的音频管理
        /// <summary>
        /// 播放物体音效
        /// </summary>
        /// <param name="go">挂载物体</param>
        /// <param name="soundParams">音效设置</param>
        /// <param name="fadeTime">淡入淡出时间</param>
        /// <param name="onComplete">播放完成回调</param>
        /// <returns></returns>
        public int PlaySound(GameObject go, AudioClipData soundParams, float fadeTime = 0, Action onComplete = null)
        {
            if (go == null)
            {
                Debug.LogError("PlaySound GameObject is null");
                return -1;
            }
            var audioClip = soundParams;
            AudioSourceData source = null;
            if (audioClip.PlayParams.Type == AudioType.ME || audioClip.PlayParams.Type == AudioType.SE)
            {
                source = GetSoundSourceData(go);
            }
            else
            {
                Debug.LogError($"AudioType == {audioClip.PlayParams.Type}. PlaySound Type isn't ME or SE");
                return -1;
            }

            GetClip(audioClip.Path, (clip) =>
            {
                source.Play(clip, _soundVolume, audioClip.PlayParams, fadeTime, onComplete);
            });

            return source.InstanceID;
        }
        /// <summary>
        /// 清除GameObject上的所有音音频
        /// </summary>
        public void ClearAudioByGo(GameObject go)
        {
            int instanceId = go.GetInstanceID();
            if (_goSources.TryGetValue(instanceId, out var audioSourceData))
            {
                audioSourceData.Dispose();
                _goSources.Remove(instanceId);
            }
        }

        /// <summary>
        /// 暂停某个GameObject上的所有音频
        /// </summary>
        public void PauseByGo(GameObject go,float fadeTime = 0)
        {
            int instanceId = go.GetInstanceID();
            if (_goSources.TryGetValue(instanceId, out var audioSourceData))
            {
                audioSourceData.Pause(fadeTime);
            }
        }
        /// <summary>
        /// 继续某个GameObject上的所有音频
        /// </summary>
        public void UnPauseByGo(GameObject go, float fadeTime = 0)
        {
            int instanceId = go.GetInstanceID();
            if (_goSources.TryGetValue(instanceId, out var audioSourceData))
            {
                audioSourceData.UnPause(fadeTime);
            }
        }
        #endregion

        public void Update(float dt)
        {
            _bgm?.Update(dt);
            _bgsGoAudioSource?.Update(dt);
            foreach (var goSource in _goSources)
            {
                goSource.Value.Update(dt);
            }
        }
    }
}

