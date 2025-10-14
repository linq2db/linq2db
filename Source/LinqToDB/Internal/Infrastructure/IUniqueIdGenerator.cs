namespace LinqToDB.Internal.Infrastructure
{
	interface IUniqueIdGenerator
	{
		public int GetNext();
	}

	interface IUniqueIdGenerator<T> : IUniqueIdGenerator
	{
	}
}
