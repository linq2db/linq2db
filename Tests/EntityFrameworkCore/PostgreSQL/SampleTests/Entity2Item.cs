namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.SampleTests
{
	public sealed class Entity2Item
	{
		public Id<Entity, long> EntityId { get; set; }
		public Entity Entity { get; set; } = null!;
		public Id<Item, long> ItemId { get; set; }

		public Entity2Item()
		{
		}

		public Item Item { get; set; } = null!;
	}
}
