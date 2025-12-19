
namespace Shared.DDD;

public abstract class Entity<T> : IEntity<T> where T : struct
{
    public T Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public String? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public String? LastModifiedBy { get; set; }
}
