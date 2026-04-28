using System;
using System.Collections.Generic;

public static partial class ProtoCmds
{
    //Tips：这里的字典在自动导出proto时生成partial文件去填充，禁止手动修改！
    private static readonly Dictionary<Type, ushort> TypeCmdMap = new Dictionary<Type, ushort>();
    public static ushort GetCmdId<T>()
    {
        return TypeCmdMap.TryGetValue(typeof(T), out var id) ? id : (ushort)0;
    }

    public static ushort GetCmdId(Type type)
    {
        return TypeCmdMap.TryGetValue(type, out var id) ? id : (ushort)0;
    }
}