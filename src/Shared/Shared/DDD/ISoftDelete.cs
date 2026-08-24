namespace Shared.DDD;

public interface ISoftDelete
{
  bool IsDeleted { get; set; }
}
