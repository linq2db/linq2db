using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	public class ClickHouseSqlOptimizer : BasicSqlOptimizer
	{
		public ClickHouseSqlOptimizer(SqlProviderFlags sqlProviderFlags, DataOptions dataOptions) : base(sqlProviderFlags)
		{
			_dataOptions = dataOptions;
		}

		readonly DataOptions _dataOptions;

		public ClickHouseOptions ProviderOptions => field ??= _dataOptions.FindOrDefault(ClickHouseOptions.Default);

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.FinalizeStatement(statement, context, dataOptions, mappingSchema);

			statement = DisableParameters(statement);

			statement = FixCteAliases(statement);

			return statement;
		}

		protected override SqlStatement FinalizeUpdate(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			var result = base.FinalizeUpdate(statement, dataOptions, mappingSchema);

			if (result is SqlUpdateStatement updateStatement)
			{
				CorrectUpdateSetters(updateStatement);
			}

			return result;
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new ClickHouseSqlExpressionConvertVisitor(allowModify, ProviderOptions);
		}

		private static SqlStatement DisableParameters(SqlStatement statement)
		{
			// We disable parameters completely as parameters support is very poor across providers:
			// - big difference in behavior of parameters between providers
			// - not all places could accept parameters (e.g. due to provider limitation)
			//
			// E.g. see https://github.com/Octonica/ClickHouseClient/issues/49
			statement = statement.Convert(static (visitor, e) =>
			{
				if (e is SqlParameter p)
					p.IsQueryParameter = false;

				return e;
			});

			return statement;
		}

		private static SqlStatement FixCteAliases(SqlStatement statement)
		{
			// CTE clause in ClickHouse currently doesn't support field list, so we should ensure
			// that CTE query use same field names as we generate for CTE table
			//
			// Issue (has PR): https://github.com/ClickHouse/ClickHouse/issues/22932
			// After it fixed we probably need to introduce dialects to provider for backward compat
			statement = statement.Convert(static (visitor, e) =>
			{
				if (e is CteClause cte)
				{
					for (var i = 0; i < cte.Fields.Count; i++)
						cte.Body!.Select.Columns[i].RawAlias = cte.Fields[i].Alias ?? cte.Fields[i].PhysicalName;

					// block rewrite of alias
					cte.Body!.DoNotSetAliases = true;
				}

				return e;
			});

			return statement;
		}

	}
}
