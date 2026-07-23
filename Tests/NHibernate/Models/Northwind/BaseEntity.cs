namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public class BaseEntity : ISoftDelete
	{
		public virtual bool IsDeleted { get; set; }
	}

	public interface ISoftDelete
	{
		public bool IsDeleted { get; }
	}
}
