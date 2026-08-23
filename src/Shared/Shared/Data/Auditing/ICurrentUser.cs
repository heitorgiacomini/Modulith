namespace Shared.Data.Auditing;

public interface ICurrentUser
{
  Guid? Id { get; }
  string? UserName { get; }
  bool IsAuthenticated { get; }
  string? TraceId { get; }
}
