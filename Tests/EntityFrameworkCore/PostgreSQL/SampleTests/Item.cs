namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.SampleTests
{
	public sealed class Item : IHasWriteableId<Item, long>
	{
		public Id<Item, long> Id { get; set; }
		public string Name { get; set; } = null!;
	}
}
