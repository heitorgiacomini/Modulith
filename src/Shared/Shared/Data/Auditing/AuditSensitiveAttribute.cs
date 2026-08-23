namespace Shared.Data.Auditing;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditSensitiveAttribute : Attribute
{
}
