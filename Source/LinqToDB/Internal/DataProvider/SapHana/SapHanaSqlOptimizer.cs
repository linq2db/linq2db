using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	sealed class SapHanaSqlOptimizer : BasicSqlOptimizer
	{
		public SapHanaSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{

		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SapHanaSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete: statement = GetAlternativeDelete((SqlDeleteStatement) statement, dataOptions); break;
				case QueryType.Update: statement = GetAlternativeUpdate((SqlUpdateStatement) statement, dataOptions, mappingSchema); break;
			}

			RemoveParametersFromLateralJoin(statement);

			return statement;
		}

		static void RemoveParametersFromLateralJoin(SqlStatement statement)
		{
			new LateralJoinParametersCorrector().Visit(statement);
		}

		class LateralJoinParametersCorrector : QueryElementVisitor
		{
			bool _isLateralJoin;

			public LateralJoinParametersCorrector() : base(VisitMode.Modify)
			{
			}

			protected override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
			{
				var saveIsLateralJoin = _isLateralJoin;
				_isLateralJoin = _isLateralJoin || element.JoinType == JoinType.CrossApply || element.JoinType == JoinType.OuterApply;

				base.VisitSqlJoinedTable(element);

				_isLateralJoin = saveIsLateralJoin;

				return element;
			}

			protected override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
			{
				if (_isLateralJoin)
				{
					sqlParameter.IsQueryParameter = false;
				}

				return sqlParameter;
			}
		}
	}
}
