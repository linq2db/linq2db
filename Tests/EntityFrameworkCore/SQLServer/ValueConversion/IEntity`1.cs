namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer.ValueConversion
{
	public interface IEntity<TKey>
	{
		public TKey Id { get; }
	}
}
