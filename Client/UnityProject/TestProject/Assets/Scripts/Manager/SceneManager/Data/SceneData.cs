using System.Collections.Generic;

namespace Fuel.Scene
{
    /// <summary>
    /// 场景数据基类，用于场景间传递数据
    /// </summary>
    public class SceneData
    {
        /// <summary>
        /// 来源场景ID
        /// </summary>
        public string FromSceneId { get; set; }

        /// <summary>
        /// 附加参数字典，可用于传递自定义数据
        /// </summary>
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();

        public SceneData() { }

        public SceneData(string fromSceneId)
        {
            FromSceneId = fromSceneId;
        }
    }
}
