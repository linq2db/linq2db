namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class BaseEntity : ISoftDelete
	{
		public bool IsDeleted { get; set; }
	}

	public interface ISoftDelete
	{
		public bool IsDeleted { get; }
	}
}
