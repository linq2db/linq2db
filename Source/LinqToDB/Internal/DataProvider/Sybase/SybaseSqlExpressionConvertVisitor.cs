using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	public class SybaseSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SybaseSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		// could be enabled if we add SP03 version support (also IsDistinctFromSupported should be enabled)
		//protected override bool SupportsDistinctAsExistsIntersect => true;

		#region LIKE

		private static readonly string[] SybaseCharactersToEscape = {"_", "%", "[", "]", "^"};

		public override string[] LikeCharactersToEscape => SybaseCharactersToEscape;

		#endregion

		protected internal override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
		{
			var result = base.VisitExistsPredicate(predicate);

			if (result is SqlPredicate.Exists { SubQuery: { HasSetOperators: true } selectQuery } exists)
			{
				var query = new SelectQuery() {DoNotRemove = true};
				query.From.Table(selectQuery);

				result = new SqlPredicate.Exists(exists.IsNot, query);
			}

			return result;
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func switch
			{
				{ Name: PseudoFunctions.REPLACE } => func.WithName("Str_Replace"),
				{ Name: PseudoFunctions.LENGTH } => func.WithName("CHAR_LENGTH"),

				{
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					Type: var type,
				} => Add<int>(
						new SqlFunction(
							type,
							"CharIndex",
							p0,
							new SqlFunction(
								MappingSchema.GetDbDataType(typeof(string)),
								"Substring",
								p1,
								p2,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)),
								"Len",
								p1)
							)
						),
						Sub(p2, 1)
					),

				{
					Name: "Stuff",
					Parameters:
					[
						var p0, var p1, _,
						SqlValue { Value: string @string, ValueType: var valueType }
					],
					Type: var type,
				} when string.IsNullOrEmpty(@string) =>
					new SqlFunction(
						type,
						"Stuff",
						ParametersNullabilityType.SameAsFirstParameter,
						p0,
						p1,
						p1,
						new SqlValue(valueType, null)
					),

				_ => base.ConvertSqlFunction(func),
			};
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			// TODO: Revisit this workaround with string concatenation changes in PR #5504.
			if (DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr && element is { Operation: "+", SystemType: var type } && type.IsStringType)
			{
				var expr1 = UnwrapEmptyStringCoalesce(element.Expr1);
				var expr2 = UnwrapEmptyStringCoalesce(element.Expr2);

				if (!ReferenceEquals(expr1, element.Expr1) || !ReferenceEquals(expr2, element.Expr2))
					return new SqlBinaryExpression(element.Type, expr1, element.Operation, expr2, element.Precedence);
			}

			return base.ConvertSqlBinaryExpression(element);

			static ISqlExpression UnwrapEmptyStringCoalesce(ISqlExpression expression)
			{
				return expression switch
				{
					SqlFunction
					{
						Name      : "Coalesce",
						Parameters: [var value, SqlValue { Value: "" }]
					} => value,
					SqlCoalesceExpression
					{
						Expressions: [var value, SqlValue { Value: "" }]
					} => value,
					_ => expression
				};
			}
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			if (expr is SqlValue
				{
					Value: uint or long or ulong or float or double or decimal,
				} value)
			{
				expr = new SqlCastExpression(expr, value.ValueType, null, isMandatory: true);
			}
			else if (expr is SqlParameter param)
			{
				var paramType = param.Type.SystemType.UnwrapNullableType();

				var wrap = paramType == typeof(uint)
						|| paramType == typeof(long)
						|| paramType == typeof(ulong)
						|| paramType == typeof(float)
						|| paramType == typeof(double)
						|| paramType == typeof(decimal);

				if (wrap && param.IsQueryParameter)
				{
					if (!EvaluationContext.IsParametersInitialized)
					{
						wrap = false;
					}
					else
					{
						var paramValue = param.GetParameterValue(EvaluationContext.ParameterValues);
						wrap = paramValue.ProviderValue == null;
					}
				}

				if (wrap)
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory: true);
			}

			return base.WrapColumnExpression(expr);
		}
	}
}
