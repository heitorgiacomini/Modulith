namespace Shared.Data.Auditing;

public interface IAuditTrail
{
  void Write(AuditEventV1 auditEvent);
}
