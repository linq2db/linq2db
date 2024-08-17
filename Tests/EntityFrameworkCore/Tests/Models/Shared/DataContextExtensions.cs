namespace LinqToDB.EntityFrameworkCore.Tests.Models.Shared
{
	public static class DataContextExtensions
	{
		public static Id<T, long> InsertAsId<T>(this IDataContext context, T item)
			where T : IHasWriteableId<T, long>
		{
			item.Id = context.InsertWithInt64Identity(item).AsId<T>();
			return item.Id;
		}
	}
}
