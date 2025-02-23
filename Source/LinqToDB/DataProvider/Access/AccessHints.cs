using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Access
{
	public static class AccessHints
	{
		public static class Query
		{
			public const string WithOwnerAccessOption = "WITH OWNERACCESS OPTION";
		}

		#region AccessSpecific Hints

		[ExpressionMethod(nameof(WithOwnerAccessOptionImpl))]
		public static IAccessSpecificQueryable<TSource> WithOwnerAccessOption<TSource>(this IAccessSpecificQueryable<TSource> query)
			where TSource : notnull
		{
			return SubQueryHint(query, Query.WithOwnerAccessOption);
		}

		static Expression<Func<IAccessSpecificQueryable<TSource>,IAccessSpecificQueryable<TSource>>> WithOwnerAccessOptionImpl<TSource>()
			where TSource : notnull
		{
			return query => SubQueryHint(query, Query.WithOwnerAccessOption);
		}

		#endregion

		#region SubQueryHint

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Access, Sql.QueryExtensionScope.SubQueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IAccessSpecificQueryable<TSource> SubQueryHint<TSource>(this IAccessSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new AccessSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		#endregion
	}
}
