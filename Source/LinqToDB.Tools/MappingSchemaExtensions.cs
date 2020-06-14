﻿using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

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
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			this MappingSchema mappingSchema,
			[InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
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
		[Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>(this MappingSchema mappingSchema)
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
		[Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>(this MappingSchema mappingSchema)
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
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			this IDataContext dataContext,
			[InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
		{
			if (dataContext     == null) throw new ArgumentNullException(nameof(dataContext));
			if (columnPredicate == null) throw new ArgumentNullException(nameof(columnPredicate));

			return dataContext.MappingSchema.GetEqualityComparer<T>(columnPredicate);
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="dataContext">Instance of <see cref="T:IDataContext" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>(this IDataContext dataContext)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			return dataContext.MappingSchema.GetEntityEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity primary key columns equality.
		/// </summary>
		/// <param name="dataContext">Instance of <see cref="T:IDataContext" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>(this IDataContext dataContext)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

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
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			this ITable<T> table,
			[InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
		{
			if (table           == null) throw new ArgumentNullException(nameof(table));
			if (columnPredicate == null) throw new ArgumentNullException(nameof(columnPredicate));

			return table.DataContext.MappingSchema.GetEqualityComparer<T>(columnPredicate);
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="table">Instance of <see cref="T:ITable`1" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>(this ITable<T> table)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			return table.DataContext.MappingSchema.GetEntityEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface
		/// based on provided entity primary key columns equality.
		/// </summary>
		/// <param name="table">Instance of <see cref="T:ITable`1" />.</param>
		/// <returns>Instance of <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>(this ITable<T> table)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			return table.DataContext.MappingSchema.GetKeyEqualityComparer<T>();
		}
	}
}
