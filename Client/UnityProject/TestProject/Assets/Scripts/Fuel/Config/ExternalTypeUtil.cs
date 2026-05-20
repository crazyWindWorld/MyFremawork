using UnityEngine;

namespace cfg
{
    /// <summary>
    /// Luban 外部类型转换工具
    /// 由 TableKit 工具自动生成
    /// </summary>
    public static class ExternalTypeUtil
    {
        public static Vector2 NewVector2(vector2 v) => new(v.X, v.Y);
        public static Vector3 NewVector3(vector3 v) => new(v.X, v.Y, v.Z);
        public static Vector4 NewVector4(vector4 v) => new(v.X, v.Y, v.Z, v.W);
    }
}
