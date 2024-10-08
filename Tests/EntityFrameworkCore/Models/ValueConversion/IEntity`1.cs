namespace LinqToDB.EntityFrameworkCore.Tests.Models.ValueConversion
{
	public interface IEntity<TKey>
	{
		public TKey Id { get; }
	}
}
