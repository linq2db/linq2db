using System.ComponentModel.DataAnnotations;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.NpgSqlEntities
{
	public class EntityWithXmin
	{
		[Key]
		public int    Id    { get; set; }
		public uint   xmin  { get; set; }
		public string Value { get; set; } = null!;
	}
}
