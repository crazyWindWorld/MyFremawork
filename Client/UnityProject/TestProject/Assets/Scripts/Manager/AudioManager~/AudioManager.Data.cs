
using System.Collections.Generic;

namespace Fuel.Manager.Audio
{
    public sealed partial class AudioManager
    {
        /// <summary>
        /// 声音大类枚举
        /// </summary>
        public enum AudioType
        {
            /// <summary>
            /// 背景音乐
            /// </summary>
            BGM = 1,

            /// <summary>
            /// 环境音乐 例：鸟叫、雷声
            /// </summary>
            BGS = 2,

            /// <summary>
            /// 气氛音效 例：战斗胜利、转场
            /// </summary>
            ME = 3,

            /// <summary>
            /// 普通音效 例：点击、打击
            /// </summary>
            SE = 4,
        }

        public struct SoundParams
        {
            /// <summary>
            /// 声音大类
            /// </summary>
            public AudioType Type { get; set; }

            /// <summary>
            /// 是否循环
            /// </summary>
            public bool IsLoop { get; set; }

            /// <summary>
            /// 声音混合量
            /// 0-> 2D
            /// 1-> 3D
            /// </summary>
            public float SpatialBlend { get; set; }

            /// <summary>
            /// 声音最小距离
            /// </summary>
            public float MinDistance { get; set; }

            /// <summary>
            /// 声音最大距离
            /// </summary>
            public float MaxDistance { get; set; }
        }

        public sealed class AudioClipData
        {
            /// <summary>
            /// 路径
            /// </summary>
            public string Path { get; set; }

            public SoundParams PlayParams { get; set; } = new SoundParams();
        }

        // 对象池管理
        private static readonly Stack<SoundParams> _soundParamsPool = new Stack<SoundParams>();
        private static readonly Stack<SoundParams> _audioClipDataPool = new Stack<SoundParams>();

        /// <summary>
        /// 从对象池获取 SoundParams
        /// </summary>
        public static SoundParams GetSoundParams()
        {
            if (_soundParamsPool.Count > 0)
            {
                return _soundParamsPool.Pop();
            }
            return new SoundParams();
        }

        /// <summary>
        /// 回收 SoundParams 到对象池
        /// </summary>
        public static void ReturnSoundParams(SoundParams soundParams)
        {
            _soundParamsPool.Push(soundParams);
        }

        /// <summary>
        /// 获取 AudioClipData 对象（简化版，避免重复创建）
        /// </summary>
        public static AudioClipData GetAudioClipData(string path, AudioType type, bool isLoop = false)
        {
            var audioClipData = new AudioClipData
            {
                Path = path,
                PlayParams = GetSoundParams()
            };
            audioClipData.PlayParams.Type = type;
            audioClipData.PlayParams.IsLoop = isLoop;
            return audioClipData;
        }

        /// <summary>
        /// 回收 AudioClipData 到对象池
        /// </summary>
        public static void ReturnAudioClipData(AudioClipData audioClipData)
        {
            ReturnSoundParams(audioClipData.PlayParams);
        }
    }
}