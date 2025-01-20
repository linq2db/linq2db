namespace LinqToDB.EntityFrameworkCore.Tests.Models.Shared
{
	public sealed class Child : IHasWriteableId<Child, long>
	{
		public Id<Child, long> Id { get; set; }
		public Id<Entity, long> ParentId { get; set; }
		public string Name { get; set; } = null!;
		public Entity Parent { get; set; } = null!;
	}
}
