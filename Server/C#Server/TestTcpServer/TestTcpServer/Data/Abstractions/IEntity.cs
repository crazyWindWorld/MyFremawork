namespace GameServer.Data.Abstractions;

/// <summary>实体标记接口，所有 DB 实体必须实现</summary>
public interface IEntity
{
    long Id { get; set; }
}
