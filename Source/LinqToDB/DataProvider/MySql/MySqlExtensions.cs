using LinqToDB.Expressions;
using LinqToDB.Linq;
using System;

namespace LinqToDB.DataProvider.MySql
{
	public static class MySqlExtensions
	{
		#region FTS

		/// <summary>
		/// Search modifier for MATCH AGAINST full-text search predicate.
		/// </summary>
		public enum MatchModifier
		{
			/// <summary>
			/// Applies 'IN NATURAL LANGUAGE MODE' (default value) search modifier.
			/// </summary>
			NaturalLanguage,
			/// <summary>
			/// Applies 'IN BOOLEAN MODE' (default value) search modifier.
			/// </summary>
			Boolean,
			/// <summary>
			/// Applies 'IN NATURAL LANGUAGE MODE WITH QUERY EXPANSION'/'WITH QUERY EXPANSION' search modifier.
			/// </summary>
			WithQueryExpansion
		}

		/// <summary>
		/// Applies full-text search condition using MATCH AGAINST predicate against specified full-text columns using default mode (IN NATURAL LANGUAGE MODE).
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("MATCH({columns, ', '}) AGAINST ({search})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Match<TEntity>(this TEntity entity, [ExprParameter("search")] string search, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(Match)}' is server-side method.");
		}

		/// <summary>
		/// Calculates relevance of full-text search for current record using MATCH AGAINST predicate against specified full-text columns using default mode (IN NATURAL LANGUAGE MODE).
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns full-text search relevance value for current record.</returns>
		[Sql.Extension("MATCH({columns, ', '}) AGAINST ({search})", ServerSideOnly = true)]
		public static double MatchRelevance<TEntity>(this TEntity entity, [ExprParameter("search")] string search, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(Match)}' is server-side method.");
		}

		class ModifierBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var modifier = builder.GetValue<MatchModifier>("modifier");

				switch (modifier)
				{
					case MatchModifier.NaturalLanguage:
						// default modifier, no need to add it to SQL
						break;
					case MatchModifier.Boolean:
						builder.AddExpression("modifier", " IN BOOLEAN MODE");
						break;
					case MatchModifier.WithQueryExpansion:
						// use short form without 'IN NATURAL LANGUAGE MODE' prefix
						builder.AddExpression("modifier", " WITH QUERY EXPANSION");
						break;
					default:
						throw new ArgumentOutOfRangeException("modifier");
				}
			}
		}



		/// <summary>
		/// Applies full-text search condition using MATCH AGAINST predicate against specified full-text columns using specified search modifier.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="modifier">Search modifier.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("MATCH({columns, ', '}) AGAINST ({search}{modifier?})", IsPredicate = true, ServerSideOnly = true, BuilderType = typeof(ModifierBuilder))]
		public static bool Match<TEntity>(this TEntity entity, [SqlQueryDependent] MatchModifier modifier, [ExprParameter("search")] string search, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(Match)}' is server-side method.");
		}

		/// <summary>
		/// Calculates relevance of full-text search for current record using MATCH AGAINST predicate against specified full-text columns using specified search modifier.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="modifier">Search modifier.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns full-text search relevance value for current record.</returns>
		[Sql.Extension("MATCH({columns, ', '}) AGAINST ({search}{modifier?})", ServerSideOnly = true, BuilderType = typeof(ModifierBuilder))]
		public static double MatchRelevance<TEntity>(this TEntity entity, [SqlQueryDependent] MatchModifier modifier, [ExprParameter("search")] string search, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(Match)}' is server-side method.");
		}

		#endregion
	}
}
