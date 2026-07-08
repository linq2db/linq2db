using System;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public class FirebirdSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public FirebirdSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool ConcatRequiresExplicitStringCast => false;

		#region LIKE

		protected static string[] LikeFirebirdEscapeSymbols = { "_", "%" };

		public override string[] LikeCharactersToEscape    => LikeFirebirdEscapeSymbols;
		public override bool     LikeValueParameterSupport => false;

		#endregion

		public override ISqlExpression ConvertSqlUnaryExpression(SqlUnaryExpression element)
		{
			if (element.Operation is SqlUnaryOperation.BitwiseNegation)
				return new SqlFunction(element.Type, "BIN_NOT", element.Expr);

			return base.ConvertSqlUnaryExpression(element);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element.Operation switch
			{
				"%"                                           => new SqlFunction(element.Type, "Mod", element.Expr1, element.Expr2),
				"&"                                           => new SqlFunction(element.Type, "Bin_And", element.Expr1, element.Expr2),
				"|"                                           => new SqlFunction(element.Type, "Bin_Or", element.Expr1, element.Expr2),
				"^"                                           => new SqlFunction(element.Type, "Bin_Xor", element.Expr1, element.Expr2),
				_                                             => base.ConvertSqlBinaryExpression(element),
			};
		}

		protected virtual bool? GetCaseSensitiveParameter(SqlPredicate.SearchString predicate)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateExpression(EvaluationContext);

			return caseSensitive switch
			{
				'0' => false,
				'1' => true,
				bool boolValue => boolValue,
				_ => null,
			};
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var caseSensitive = GetCaseSensitiveParameter(predicate);

			// for explicit case-sensitive search we apply "CAST({0} AS BLOB)" to searched string as COLLATE's collation is character set-dependent
			switch (predicate.Kind)
			{
				case SqlPredicate.SearchString.SearchKind.EndsWith:
				{
					if (caseSensitive == false)
					{
						predicate = new SqlPredicate.SearchString(
							PseudoFunctions.MakeToLower(predicate.Expr1, MappingSchema),
							predicate.IsNot,
							PseudoFunctions.MakeToLower(predicate.Expr2, MappingSchema), predicate.Kind,
							predicate.CaseSensitive);
					}
					else if (caseSensitive == true)
					{
						predicate = new SqlPredicate.SearchString(
							new SqlExpression(MappingSchema.GetDbDataType(typeof(string)), "CAST({0} AS BLOB)", Precedence.Primary, predicate.Expr1),
							predicate.IsNot,
							predicate.Expr2,
							predicate.Kind,
							predicate.CaseSensitive);
					}

					return ConvertSearchStringPredicateViaLike(predicate);
				}

				case SqlPredicate.SearchString.SearchKind.StartsWith:
				{
					var expr = new SqlExpression(MappingSchema.GetDbDataType(typeof(bool)),
						predicate.IsNot ? "{0} NOT STARTING WITH {1}" : "{0} STARTING WITH {1}",
						Precedence.Comparison,
						SqlFlags.IsPredicate,
						ParametersNullabilityType.IfAnyParameterNullable,
						TryConvertToValue(
							caseSensitive switch
							{
								false => PseudoFunctions.MakeToLower(predicate.Expr1, MappingSchema),
								true  => new SqlExpression(MappingSchema.GetDbDataType(typeof(string)), "CAST({0} AS BLOB)", Precedence.Primary, predicate.Expr1),
								_     => predicate.Expr1,
							},
							EvaluationContext
						),
						TryConvertToValue(
							caseSensitive == false
								? PseudoFunctions.MakeToLower(predicate.Expr2, MappingSchema)
								: predicate.Expr2, EvaluationContext
						)
					) { CanBeNull = false };

					return new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.Expr(expr));
				}

				case SqlPredicate.SearchString.SearchKind.Contains:
				{
					if (caseSensitive == false)
					{
						var expr = new SqlExpression(MappingSchema.GetDbDataType(typeof(bool)),
							predicate.IsNot ? "{0} NOT CONTAINING {1}" : "{0} CONTAINING {1}",
							precedence : Precedence.Comparison,
							flags : SqlFlags.IsPredicate,
							nullabilityType : ParametersNullabilityType.IfAnyParameterNullable,
							TryConvertToValue(predicate.Expr1, EvaluationContext),
							TryConvertToValue(predicate.Expr2, EvaluationContext)) { CanBeNull = false };

						return new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.Expr(expr));
					}
					else
					{
						if (caseSensitive == true)
						{
							predicate = new SqlPredicate.SearchString(
								new SqlExpression(MappingSchema.GetDbDataType(typeof(string)), "CAST({0} AS BLOB)", Precedence.Primary, predicate.Expr1),
								predicate.IsNot,
								predicate.Expr2,
								predicate.Kind,
								new SqlValue(false));
						}

						return ConvertSearchStringPredicateViaLike(predicate);
					}
				}

				default:
					throw new InvalidOperationException($"Unexpected predicate: {predicate.Kind}");
			}
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (cast.SystemType.ToUnderlying() == typeof(bool))
			{
				if (cast.Type.DataType == DataType.Boolean && cast.Expression is not ISqlPredicate)
				{
					if (cast.Expression is SqlValue)
						return cast.Expression;

					var sc = new SqlSearchCondition()
						.AddNotEqual(cast.Expression, new SqlValue(QueryHelper.GetDbDataType(cast.Expression, MappingSchema), 0), DataOptions.LinqOptions.CompareNulls);
					return sc;
				}
			}
			else if (cast.SystemType.ToUnderlying() == typeof(string) && cast.Expression.SystemType?.ToUnderlying() == typeof(Guid))
			{
				return Translation.FirebirdMemberTranslator.TranslateGuidToString(cast.Expression, Factory);
			}
			else if (cast.SystemType.ToUnderlying() == typeof(Guid) && cast.Expression.SystemType?.ToUnderlying() == typeof(string))
			{
				return new SqlFunction(cast.Type, "CHAR_TO_UUID", cast.Expression);
			}
			else if (cast.ToType.DataType == DataType.Decimal)
			{
				if (cast.ToType.Precision == null && cast.ToType.Scale == null)
				{
					//TODO: check default precision and scale
					cast = cast.WithToType(cast.ToType.WithPrecisionScale(18, 10));
				}
			}

			cast = FloorBeforeConvert(cast);

			return base.ConvertConversion(cast);
		}

		protected internal override IQueryElement VisitExprPredicate(SqlPredicate.Expr predicate)
		{
			return predicate switch
			{
				{ ElementType: QueryElementType.ExprPredicate, Expr1: SqlParameter { Type.DataType: not DataType.Boolean } p } =>
					Visit(
						new SqlPredicate.ExprExpr(
							p,
							SqlPredicate.Operator.Equal,
							MappingSchema.GetSqlValue(p.Type, true),
							DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null
						)
					),

				_ => base.VisitExprPredicate(predicate),
			};
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func switch
			{
				{ Name: PseudoFunctions.LENGTH } => func.WithName("CHAR_LENGTH"),
				_                                => base.ConvertSqlFunction(func),
			};
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			// Read a Guid as its canonical text form via UUID_TO_CHAR. A Guid is stored / produced as
			// CHAR(16) CHARACTER SET OCTETS (GEN_UUID, and how ConvertGuidToSql writes it); reading those raw
			// bytes back fails under a non-NONE connection charset ("Malformed string", SQLSTATE 22000) because
			// the client transliterates the column to the connection charset during fetch. The ASCII text form
			// is charset-independent. This only rewrites projected (read) columns — predicates and keys keep the
			// indexable binary form, and writes stay binary — and round-trips because UUID_TO_CHAR of the stored
			// network-order bytes yields the value's canonical string.
			if (expr.SystemType?.ToUnderlying() == typeof(Guid)
				&& QueryHelper.GetDbDataType(expr, MappingSchema).DataType is not (DataType.Char or DataType.NChar or DataType.VarChar or DataType.NVarChar))
			{
				var textType = MappingSchema.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(36);
				return new SqlFunction(textType, "UUID_TO_CHAR", expr);
			}

			if (expr is SqlValue
				{
					Value: uint or long or ulong or float or double or decimal,
				} value)
			{
				expr = new SqlCastExpression(expr, value.ValueType, null, isMandatory: true);
			}

			if (expr is SqlParameter { IsQueryParameter: false } param)
			{
				var paramType = param.Type.SystemType.UnwrapNullableType();
				if (paramType == typeof(uint)
					|| paramType == typeof(long)
					|| paramType == typeof(ulong)
					|| paramType == typeof(float)
					|| paramType == typeof(double)
					|| paramType == typeof(decimal))
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory: true);
			}

			return base.WrapColumnExpression(expr);
		}
	}
}
