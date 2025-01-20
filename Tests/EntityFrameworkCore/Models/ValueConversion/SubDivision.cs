using System;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ValueConversion
{
	public class SubDivision : IEntity<long>
	{
		long IEntity<long>.Id => Id;
		public Id<SubDivision, long> Id { get; set; }
		public Guid PermanentId { get; set; }
		public string Code { get; set; } = null!;
		public string Name { get; set; } = null!;
		public bool? IsDeleted { get; set; }
	}
}
