namespace LinqToDB.Infrastructure
{
	public interface IUniqueIdGenerator
	{
		public int GetNext();
	}

	public interface IUniqueIdGenerator<T> : IUniqueIdGenerator
	{
	}

}
