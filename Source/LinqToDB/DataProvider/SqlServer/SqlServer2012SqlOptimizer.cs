using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	class SqlServer2012SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags) : this(sqlProviderFlags, SqlServerVersion.v2012)
		{
		}

		protected SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion version) : base(sqlProviderFlags, version)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlServer2012SqlExpressionConvertVisitor(allowModify, SQLVersion);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			// SQL Server 2012 supports OFFSET/FETCH providing there is an ORDER BY
			// UPDATE queries do not directly support ORDER BY, TOP, OFFSET, or FETCH, but they are supported in subqueries

			if (statement.IsUpdate() || statement.IsDelete())
				statement = WrapRootTakeSkipOrderBy(statement);

			statement = AddOrderByForSkip(statement);

			return statement;
		}

		/// <summary>
		/// Adds an ORDER BY clause to queries using OFFSET/FETCH, if none exists
		/// </summary>
		protected SqlStatement AddOrderByForSkip(SqlStatement statement)
		{
			statement = statement.Convert(static (visitor, element) =>
			{
				if (element.ElementType == QueryElementType.OrderByClause)
				{
					var orderByClause = (SqlOrderByClause)element;
					// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
					if (orderByClause.SelectQuery != null && orderByClause.OrderBy.IsEmpty && orderByClause.SelectQuery.Select.SkipValue != null)
					{
						return new SqlOrderByClause(new[] { new SqlOrderByItem(new SqlValue(typeof(int), 1), false, false) });
					}
				}
				return element;
			});
			return statement;
		}

	}
}
