using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using LinqToDB.Internal.Async;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Mapping;
using LinqToDB.Model;

namespace LinqToDB.Internal.Linq
{
	internal static class LinqInternalExtensions
	{
		#region Stub helpers

		internal static TOutput AsQueryable<TOutput, TInput>(TInput source)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region Table helpers
		/// <summary>
		/// Used internally to pass entity descriptor into Expression Tree.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="tableDescriptor"></param>
		/// <returns></returns>
		// TODO: Consider to rewrite such functionality in V7
		[LinqTunnel]
		[Pure]
		internal static ITable<T> UseTableDescriptor<T>(this ITable<T> table, [SqlQueryDependent] EntityDescriptor tableDescriptor)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (tableDescriptor == null) throw new ArgumentNullException(nameof(tableDescriptor));

			var result = ((ITableMutable<T>)table).ChangeTableDescriptor(tableDescriptor);
			return result;
		}
		#endregion

		#region Association helpers

		internal static TSource AssociationRecord<TSource>(this IQueryable<TSource> source)
		{
			throw new LinqToDBException("This method is not intended to be called directly.");
		}

		internal static TSource? AssociationOptionalRecord<TSource>(this IQueryable<TSource> source)
		{
			throw new LinqToDBException("This method is not intended to be called directly.");
		}

		#endregion Internal

		#region CTE

		internal static IQueryable<T> AsCte<T>(IQueryable<T> cteTable, IQueryable<T> cteBody, string? tableName)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Query Filters

		/// <summary>
		/// Disables filter for specific expression. For internal translator logic
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Query with disabled filters.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> DisableFilterInternal<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DisableFilterInternal, source), source.Expression);

			return source.Provider.CreateQuery<TSource>(expr);
		}

		public static IQueryable<TSource> ApplyModifierInternal<TSource>(this IQueryable<TSource> source, TranslationModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ApplyModifierInternal, source, modifier), source.Expression, Expression.Constant(modifier));
			return source.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Eager Loading helpers

		[LinqTunnel]
		[Pure]
		public static TSource LoadWithInternal<TSource>(
			this TSource             source,
			LoadWithEntity           loadWith)
			where TSource : class
		{
			return source;
		}

		/// <summary>
		/// Marks SelectQuery as Distinct.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Distinct query.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> SelectDistinct<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(SelectDistinct, source), currentSource.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion;

		#region IQueryable Helpers
		public static IQueryable<T> ProcessIQueryable<T>(this IQueryable<T> source)
		{
			return (IQueryable<T>)(LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source);
		}

		public static IQueryProviderAsync GetLinqToDBSource<T>(this IQueryable<T> source, [CallerMemberName] string? method = null)
		{
			if (source.ProcessIQueryable() is not IQueryProviderAsync query)
				return ThrowInvalidSource(method);

			return query;
		}

		[DoesNotReturn]
		private static IQueryProviderAsync ThrowInvalidSource(string? method)
		{
			throw new LinqToDBException($"LinqToDB method '{method}' called on non-LinqToDB IQueryable.");
		}
		#endregion
	}
}
