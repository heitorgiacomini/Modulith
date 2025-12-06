
namespace Shared.DDD
{
    public abstract class Entity<T> : IEntity<T> where T : class
    {
        public required T Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
    }
}
