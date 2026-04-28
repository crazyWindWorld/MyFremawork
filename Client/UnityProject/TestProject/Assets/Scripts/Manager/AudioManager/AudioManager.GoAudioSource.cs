using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fuel.Manager.AudioManager
{
    public sealed partial class AudioManager
    {
        public sealed class GoAudioSource
        {
            public int InstanceId;
            private readonly Stack<AudioSourceData> m_pool;
            private readonly List<AudioSourceData> m_currentSource;
#if UNITY_EDITOR
            public List<AudioSourceData> CurrentSource => m_currentSource;
#endif
            private readonly GameObject m_root;
            public GoAudioSource(GameObject go)
            {
                m_root = go;
                InstanceId = go.GetInstanceID();
                m_pool = new Stack<AudioSourceData>();
                m_currentSource = new List<AudioSourceData>();
            }
            public void Update(float dt)
            {
                for (var i = 0; i < m_currentSource.Count; i++)
                {
                    if (m_currentSource[i].IsDirty)//回收标记
                    {
                        m_pool.Push(m_currentSource[i]);
                        m_currentSource.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        m_currentSource[i].Update(dt);
                    }
                }
            }

            /// <summary>
            /// 从池中获取一个AudioSourceData
            /// </summary>
            public AudioSourceData GetAudioSourceData()
            {
                AudioSourceData result;
                if (m_pool.Count > 0)
                {
                    result = m_pool.Pop();
                }
                else
                {
                    result = new AudioSourceData(m_root.AddComponent<AudioSource>(), ++AutoID);
                }
                m_currentSource.Add(result);
                return result;
            }

            /// <summary>
            /// 停止播放音频
            /// </summary>
            /// <param name="instanceId">实例id</param>
            /// <param name="fadeTime">渐出时长</param>
            /// <param name="onFaceComponent">完成回调</param>
            /// <returns></returns>
            public bool StopByInstanceId(int instanceId,float fadeTime = 0f,Action onFaceComponent = null)
            {
                foreach (var audioSourceData in m_currentSource)
                {
                    if (audioSourceData.InstanceID==instanceId)
                    {
                        audioSourceData.Stop(fadeTime,onFaceComponent);
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// 停止所有音频
            /// </summary>
            /// <param name="fadeTime"></param>
            public void StopAll(float fadeTime = 0f)
            {
                foreach (var audioSourceData in m_currentSource)
                {
                    audioSourceData.Stop(fadeTime);
                }
            }
            
            #region 操作接口
            public void Mute(bool mute)
            {
                foreach (var audioSourceData in m_currentSource)
                {
                    audioSourceData.Mute(mute);
                }
            }
            public void Pause(float fadeTime)
            {
                foreach (var audioSourceData in m_currentSource)
                {
                    audioSourceData.Pause(fadeTime);
                }
            }

            public void SetVolume(float volume, float fadeTime)
            {
                foreach (var audioSourceData in m_currentSource)
                {
                    audioSourceData.SetVolume(volume,fadeTime);
                }
            }
            public void UnPause(float fadeTime)
            {
                foreach (var audioSourceData in m_currentSource)
                {
                    audioSourceData.UnPause(fadeTime);
                }
            }
            #endregion


            public void Dispose()
            {
                foreach (var audioSourceData in m_currentSource)
                {
                    audioSourceData.Dispose();
                }
                m_currentSource.Clear();
                while (m_pool.TryPop(out var audioSourceData))
                {
                    audioSourceData.Dispose();
                }
            }
            
            /// <summary>
            /// 清理资源
            /// </summary>
            /// <param name="retainCount">保留As数量</param>
            public void ClearRes(int retainCount = 0)
            {
                retainCount = retainCount < 0 ? 0 : retainCount;
                foreach (var audioSourceData in m_currentSource)
                {
                    audioSourceData.Dispose();
                    GameObject.Destroy(audioSourceData.As);
                }
                m_currentSource.Clear();
                while (m_pool.Count > retainCount)
                {
                    m_pool.Pop().Dispose();
                }
            }
        }
    }
}