using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore
{
	public static partial class LinqToDBForEFTools
	{
		/// <summary>
		/// Converts EF.Core <see cref="DbSet{TEntity}"/> instance to LINQ To DB <see cref="ITable{T}"/> instance.
		/// </summary>
		/// <typeparam name="T">Mapping entity type.</typeparam>
		/// <param name="dbSet">EF.Core <see cref="DbSet{TEntity}"/> instance.</param>
		/// <returns>LINQ To DB <see cref="ITable{T}"/> instance.</returns>
		public static ITable<T> ToLinqToDBTable<T>(this DbSet<T> dbSet)
			where T : class
		{
			var context = Implementation.GetCurrentContext(dbSet)
				?? throw new LinqToDBForEFToolsException($"Can not load current context from {nameof(dbSet)}");
#pragma warning disable CA2000 // Dispose objects before losing scope
			var dc = CreateLinqToDBContext(context);
#pragma warning restore CA2000 // Dispose objects before losing scope
			return dc.GetTable<T>();
		}

		/// <summary>
		/// Converts EF.Core <see cref="DbSet{TEntity}"/> instance to LINQ To DB <see cref="ITable{T}"/> instance
		/// using existing LINQ To DB <see cref="IDataContext"/> instance.
		/// </summary>
		/// <typeparam name="T">Mapping entity type.</typeparam>
		/// <param name="dbSet">EF.Core <see cref="DbSet{TEntity}"/> instance.</param>
		/// <param name="dataContext">LINQ To DB data context instance.</param>
		/// <returns>LINQ To DB <see cref="ITable{T}"/> instance.</returns>
		public static ITable<T> ToLinqToDBTable<T>(this DbSet<T> dbSet, IDataContext dataContext)
			where T : class
		{
			return dataContext.GetTable<T>();
		}
	}
}
