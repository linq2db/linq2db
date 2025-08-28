using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	public class SqlCeSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SqlCeSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsNullIf => false;

		#region LIKE

		private static string[] LikeSqlCeCharactersToEscape = { "_", "%" };

		public override string[] LikeCharactersToEscape => LikeSqlCeCharactersToEscape;

		#endregion

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%":
				{
					var exprType = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);

					if (!exprType.SystemType.IsIntegerType())
					{
						return new SqlBinaryExpression(
							typeof(int),
							PseudoFunctions.MakeCast(element.Expr1, new DbDataType(typeof(int), DataType.Int32)),
							element.Operation,
							element.Expr2,
							element.Precedence);
					}

					break;
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case PseudoFunctions.LENGTH:
				{
					/*
					 * LEN(value + ".") - 1
					 */

					var value     = func.Parameters[0];
					var valueType = Factory.GetDbDataType(value);
					var funcType  = Factory.GetDbDataType(value);

					var valueString = Factory.Add(valueType, value, Factory.Value(valueType, "."));
					var valueLength = Factory.Function(funcType, "LEN", valueString);

					return Factory.Sub(func.Type, valueLength, Factory.Value(func.Type, 1));
		}
			}

			return base.ConvertSqlFunction(func);
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
									new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "SUBSTRING",
										predicate.Expr1,
										new SqlValue(1),
										Factory.Length(predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);
						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						var indexExpression = new SqlBinaryExpression(typeof(int),
							new SqlBinaryExpression(typeof(int),
								Factory.Length(predicate.Expr1),
								"-",
								Factory.Length(predicate.Expr2)),
							"+",
							new SqlValue(1));

						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
									new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "SUBSTRING",
										predicate.Expr1,
										indexExpression,
										Factory.Length(predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "CHARINDEX",
									new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr2),
									new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr1)),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SqlSearchCondition(predicate.IsNot, canBeUnknown: null, subStrPredicate.MakeNot(predicate.IsNot));

					return result;
				}
			}

			return like;
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var toType = cast.ToType;
			var argument = cast.Expression;

			switch (toType.DataType)
			{
				case DataType.UInt64:
				{
					var argumentType = QueryHelper.GetDbDataType(argument, MappingSchema);

					if (argumentType.SystemType.IsFloatType())
					{
						return PseudoFunctions.MakeCast(new SqlFunction(cast.Type, "Floor", argument), toType);
					}

					break;
				}

				case DataType.Time:
				case DataType.DateTime:
				{
					var type1 = argument.SystemType!.ToUnderlying();

					if (IsTimeDataType(toType))
					{
						if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
							return new SqlExpression(
								cast.Type, "Cast(Convert(NChar, {0}, 114) as DateTime)",
								Precedence.Primary, argument);

						if (argument.SystemType == typeof(string))
							return argument;

						return new SqlExpression(
							cast.Type, "Convert(NChar, {0}, 114)", Precedence.Primary,
							argument);
					}

					if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
					{
						if (IsDateDataType(toType, "Datetime"))
							return new SqlExpression(
								cast.Type, "Cast(Floor(Cast({0} as Float)) as DateTime)",
								Precedence.Primary, argument);
					}

					break;
				}

				case  DataType.Decimal:
				{
					if (cast.ToType.Precision == null && cast.ToType.Scale == null)
					{
						cast = cast.WithToType(cast.ToType.WithPrecisionScale(38, 17));
						return cast;
					}

					break;
				}
			}

			return base.ConvertConversion(cast);
		}
	}

}
