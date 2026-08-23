
namespace Shared.DDD;

public abstract class Entity<T> : IEntity<T> where T : struct
{
  public T Id { get; set; }
}
