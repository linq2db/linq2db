using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using JBNotNull = JetBrains.Annotations.NotNullAttribute;

namespace LinqToDB.Tools
{
	using Comparers;
	using Mapping;
	using Reflection;

	[PublicAPI]
	public static class MappingSchemaExtensions
	{
		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="mappingSchema">Instance of <see cref="T:MappingSchema" />.</param>
		/// <param name="columnPredicate">A function to filter columns to compare.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			[JBNotNull] this MappingSchema mappingSchema,
			[JBNotNull, InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
		{
			if (mappingSchema   == null) throw new ArgumentNullException(nameof(mappingSchema));
			if (columnPredicate == null) throw new ArgumentNullException(nameof(columnPredicate));

			var cols = new HashSet<MemberAccessor>(
				mappingSchema.GetEntityDescriptor(typeof(T)).Columns
					.Where(columnPredicate).Select(c => c.MemberAccessor));

			return ComparerBuilder.GetEqualityComparer<T>(cols.Contains);
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="mappingSchema">Instance of <see cref="T:MappingSchema" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>([JBNotNull] this MappingSchema mappingSchema)
		{
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			var cols = new HashSet<MemberAccessor>(
				mappingSchema.GetEntityDescriptor(typeof(T)).Columns
					.Select(c => c.MemberAccessor));

			return ComparerBuilder.GetEqualityComparer<T>(cols.Contains);
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity primary key columns equality.
		/// </summary>
		/// <param name="mappingSchema">Instance of <see cref="T:MappingSchema" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>([JBNotNull] this MappingSchema mappingSchema)
		{
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			var cols = new HashSet<MemberAccessor>(
				mappingSchema.GetEntityDescriptor(typeof(T)).Columns
					.Where(c => c.IsPrimaryKey).Select(c => c.MemberAccessor));

			if (cols.Count > 0)
				return mappingSchema.GetEqualityComparer<T>(c => c.IsPrimaryKey);

			return mappingSchema.GetEntityEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="dataContext">Instance of <see cref="T:IDataContext" />.</param>
		/// <param name="columnPredicate">A function to filter columns to compare.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			[JBNotNull] this IDataContext dataContext,
			[JBNotNull, InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
		{
			return dataContext.MappingSchema.GetEqualityComparer<T>(columnPredicate);
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="dataContext">Instance of <see cref="T:IDataContext" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>([JBNotNull] this IDataContext dataContext)
		{
			return dataContext.MappingSchema.GetEntityEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity primary key columns equality.
		/// </summary>
		/// <param name="dataContext">Instance of <see cref="T:IDataContext" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>([JBNotNull] this IDataContext dataContext)
		{
			return dataContext.MappingSchema.GetKeyEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="table">Instance of <see cref="T:ITable`1" />.</param>
		/// <param name="columnPredicate">A function to filter columns to compare.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			[JBNotNull] this ITable<T> table,
			[JBNotNull, InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
		{
			return table.DataContext.MappingSchema.GetEqualityComparer<T>(columnPredicate);
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="table">Instance of <see cref="T:ITable`1" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>([JBNotNull] this ITable<T> table)
		{
			return table.DataContext.MappingSchema.GetEntityEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity primary key columns equality.
		/// </summary>
		/// <param name="table">Instance of <see cref="T:ITable`1" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[JBNotNull, Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>([JBNotNull] this ITable<T> table)
		{
			return table.DataContext.MappingSchema.GetKeyEqualityComparer<T>();
		}
	}
}
