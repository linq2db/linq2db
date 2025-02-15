namespace LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping
{
	public class SkipModesTable
	{
		public int Id { get; set; }
		public int? InsertOnly { get; set; }
		public int? UpdateOnly { get; set; }
		public int? ReadOnly { get; set; }
	}
}
