using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Shared
{
	public sealed class Entity : IHasWriteableId<Entity, long>
	{
		public Id<Entity, long> Id { get; set; }
		public string Name { get; set; } = null!;

		public IEnumerable<Detail> Details { get; set; } = null!;
		public IEnumerable<Child> Children { get; set; } = null!;
		public IEnumerable<Entity2Item> Items { get; set; } = null!;
	}
}
