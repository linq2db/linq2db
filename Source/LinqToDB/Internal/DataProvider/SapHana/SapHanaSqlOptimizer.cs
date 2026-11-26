using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public class SapHanaSqlOptimizer : BasicSqlOptimizer
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
				case QueryType.Delete: statement = GetAlternativeDelete((SqlDeleteStatement) statement); break;
				case QueryType.Update: statement = GetAlternativeUpdate((SqlUpdateStatement) statement, dataOptions, mappingSchema); break;
			}

			RemoveParametersFromLateralJoin(statement);

			return statement;
		}

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.FinalizeStatement(statement, context, dataOptions, mappingSchema);

			// SAP HANA types a BETWEEN by its value, and a parameter value by a date bound beside it, dropping the time of the
			// value and of a parameter bound. Casting the value keeps its own type.
			statement.VisitAll(mappingSchema, static (mappingSchema, e) =>
			{
				if (e is SqlPredicate.Between between && IsTimestampParameterBesideDate(between, mappingSchema))
				{
					between.Expr1 = new SqlCastExpression(between.Expr1, QueryHelper.GetDbDataType(between.Expr1, mappingSchema), null, isMandatory: true);
				}
			});

			return statement;
		}

		static bool IsTimestampParameterBesideDate(SqlPredicate.Between between, MappingSchema mappingSchema)
		{
			return QueryHelper.UnwrapNullablity(between.Expr1) is SqlParameter { IsQueryParameter: true }
				&& QueryHelper.GetDbDataType(between.Expr1, mappingSchema).DataType is DataType.DateTime or DataType.DateTime2
				&& (IsDate(between.Expr2, mappingSchema) || IsDate(between.Expr3, mappingSchema));

			static bool IsDate(ISqlExpression expression, MappingSchema mappingSchema)
			{
				return QueryHelper.UnwrapNullablity(expression) is not SqlParameter
					&& QueryHelper.GetDbDataType(expression, mappingSchema).DataType == DataType.Date;
			}
		}

		static void RemoveParametersFromLateralJoin(SqlStatement statement)
		{
			new LateralJoinParametersCorrector().Visit(statement);
		}

		sealed class LateralJoinParametersCorrector : QueryElementVisitor
		{
			bool _isLateralJoin;

			public LateralJoinParametersCorrector() : base(VisitMode.Modify)
			{
			}

			protected internal override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
			{
				var saveIsLateralJoin = _isLateralJoin;
				_isLateralJoin = _isLateralJoin || element.JoinType == JoinType.CrossApply || element.JoinType == JoinType.OuterApply;

				base.VisitSqlJoinedTable(element);

				_isLateralJoin = saveIsLateralJoin;

				return element;
			}

			protected internal override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
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
