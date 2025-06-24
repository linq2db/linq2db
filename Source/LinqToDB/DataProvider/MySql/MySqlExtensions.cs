using System;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.MySql
{
	public static class MySqlExtensions
	{
		public static IMySqlExtensions? MySql(this Sql.ISqlExtension? ext) => null;

		#region FTS
		sealed class ModifierBuilder : Sql.IExtensionCallBuilder
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
						throw new InvalidOperationException($"Unexpected modifier: {modifier}");
				}
			}
		}

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
		/// Example: MATCH(col1, col2) AGAINST('search query').
		/// </summary>
		/// <param name="ext">Extension point.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("MATCH({columns, ', '}) AGAINST ({search})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Match(this IMySqlExtensions? ext, [ExprParameter] string search, [ExprParameter] params object?[] columns)
			=> throw new ServerSideOnlyException(nameof(Match));

		/// <summary>
		/// Calculates relevance of full-text search for current record using MATCH AGAINST predicate against specified full-text columns using default mode (IN NATURAL LANGUAGE MODE).
		/// Example: MATCH(col1, col2) AGAINST('search query').
		/// </summary>
		/// <param name="ext">Extension point.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns full-text search relevance value for current record.</returns>
		[Sql.Extension("MATCH({columns, ', '}) AGAINST ({search})", ServerSideOnly = true)]
		public static double MatchRelevance(this IMySqlExtensions? ext, [ExprParameter] string search, [ExprParameter] params object?[] columns)
			=> throw new ServerSideOnlyException(nameof(MatchRelevance));

		/// <summary>
		/// Applies full-text search condition using MATCH AGAINST predicate against specified full-text columns using specified search modifier.
		/// Example: MATCH(col1, col2) AGAINST('search query' MODIFIER).
		/// </summary>
		/// <param name="ext">Extension point.</param>
		/// <param name="modifier">Search modifier.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("MATCH({columns, ', '}) AGAINST ({search}{modifier?})", IsPredicate = true, ServerSideOnly = true, BuilderType = typeof(ModifierBuilder))]
		public static bool Match(this IMySqlExtensions? ext, [SqlQueryDependent] MatchModifier modifier, [ExprParameter] string search, [ExprParameter] params object?[] columns)
			=> throw new ServerSideOnlyException(nameof(Match));

		/// <summary>
		/// Calculates relevance of full-text search for current record using MATCH AGAINST predicate against specified full-text columns using specified search modifier.
		/// Example: MATCH(col1, col2) AGAINST('search query' MODIFIER).
		/// </summary>
		/// <param name="ext">Extension point.</param>
		/// <param name="modifier">Search modifier.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns full-text search relevance value for current record.</returns>
		[Sql.Extension("MATCH({columns, ', '}) AGAINST ({search}{modifier?})", ServerSideOnly = true, BuilderType = typeof(ModifierBuilder))]
		public static double MatchRelevance(this IMySqlExtensions? ext, [SqlQueryDependent] MatchModifier modifier, [ExprParameter] string search, [ExprParameter] params object?[] columns)
			=> throw new ServerSideOnlyException(nameof(MatchRelevance));

		#endregion
	}
}
