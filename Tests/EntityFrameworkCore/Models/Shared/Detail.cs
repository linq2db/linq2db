using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Shared
{
	public sealed class Detail : IHasWriteableId<Detail, long>
	{
		public Id<Detail, long> Id { get; set; }
		public Id<Entity, long> MasterId { get; set; }
		public string Name { get; set; } = null!;
		public Entity Master { get; set; } = null!;
		public IEnumerable<SubDetail> Details { get; set; } = null!;
	}
}
