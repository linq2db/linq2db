using System;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public class YdbSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public YdbSqlExpressionConvertVisitor(bool allowModify) : base(allowModify) { }

		/// <inheritdoc/>
		protected override bool SupportsNullInColumn => false;

		#region (I)LIKE https://ydb.tech/docs/en/yql/reference/syntax/expressions#check-match

		protected static string[] YdbLikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => YdbLikeCharactersToEscape;

		// escape value literal should have String type
		public override ISqlExpression CreateLikeEscapeCharacter() => new SqlValue(new DbDataType(typeof(string), DataType.VarBinary), LikeEscapeCharacter);

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate);

			// use ILIKE for case-insensitive search
			if (false == predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) && searchPredicate is SqlPredicate.Like likePredicate)
			{
				searchPredicate = new SqlPredicate.Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		#endregion

		public override ISqlExpression ConvertSqlUnaryExpression(SqlUnaryExpression element)
		{
			if (element.Operation is SqlUnaryOperation.BitwiseNegation)
			{
				var argType = QueryHelper.GetDbDataType(element.Expr, MappingSchema);
				if (!argType.IsUnsignedType())
				{
					var expr = new SqlCastExpression(element.Expr, argType.ToUnsigned(), null, false);
					return new SqlUnaryExpression(element.Type, expr, element.Operation, element.Precedence);
				}
			}

			return base.ConvertSqlUnaryExpression(element);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "+" when element.Type.DataType == DataType.Undefined && element.SystemType == typeof(string):
				case "+" when element.Type.DataType
					is DataType.NVarChar
					or DataType.NChar
					or DataType.Char
					or DataType.VarChar:
				{
					var castType  = MappingSchema.GetDbDataType(typeof(string));
					var expr1Type = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);
					var expr2Type = QueryHelper.GetDbDataType(element.Expr2, MappingSchema);
					var expr1     = expr1Type.IsTextType() ? element.Expr1 : new SqlCastExpression(element.Expr1, castType, null, false);
					var expr2     = expr2Type.IsTextType() ? element.Expr2 : new SqlCastExpression(element.Expr2, castType, null, false);
					return new SqlBinaryExpression(element.SystemType, expr1, "||", expr2, element.Precedence);
				}

				case "+" when element.Type.DataType
					is DataType.Binary
					or DataType.VarBinary
					or DataType.Blob:
				{
					return new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence);
				}

				case "&" or "|" or "^":
				{
					var expr1Type = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);
					var expr2Type = QueryHelper.GetDbDataType(element.Expr2, MappingSchema);
					var expr1     = expr1Type.IsUnsignedType() ? element.Expr1 : new SqlCastExpression(element.Expr1, expr1Type.ToUnsigned(), null, false);
					var expr2     = expr2Type.IsUnsignedType() ? element.Expr2 : new SqlCastExpression(element.Expr2, expr2Type.ToUnsigned(), null, false);

					if (expr1 != element.Expr1 || expr2 != element.Expr2)
					{
						ISqlExpression expr = new SqlBinaryExpression(element.SystemType, expr1, element.Operation, expr2, element.Precedence);
						var oldType         = QueryHelper.GetDbDataType(element, MappingSchema);
						var newType         = QueryHelper.GetDbDataType(element, MappingSchema);

						if (!oldType.EqualsDbOnly(newType))
						{
							expr = new SqlCastExpression(expr, oldType, null, false);
						}

						return expr;
					}

					break;
				}

				//case "%":
				//{
				//	var dbType = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);

				//	if (dbType.SystemType.ToNullableUnderlying() != typeof(decimal))
				//	{
				//		var toType   = MappingSchema.GetDbDataType(typeof(decimal));
				//		var newLeft  = PseudoFunctions.MakeCast(element.Expr1, toType);

				//		var sysType  = dbType.SystemType?.IsNullable() == true
				//			? typeof(decimal?)
				//			: typeof(decimal);

				//		var newExpr  = PseudoFunctions.MakeMandatoryCast(
				//			new SqlBinaryExpression(sysType, newLeft, "%", element.Expr2),
				//			toType);

				//		return Visit(Optimize(newExpr));
				//	}

				//	break;
				//}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func.Name switch
			{
				PseudoFunctions.TO_LOWER => func.WithName("Unicode::ToLower"),
				PseudoFunctions.TO_UPPER => func.WithName("Unicode::ToUpper"),
				PseudoFunctions.REPLACE  => func.WithName("Unicode::ReplaceAll"),

				_                        => base.ConvertSqlFunction(func),
			};

			////----------------------------------------------------------------
			//// save cast
			//case PseudoFunctions.TRY_CONVERT:
			//	// CAST(x AS <type>?) → null
			//	return new SqlExpression(
			//		func.Type,
			//		"CAST({0} AS {1}?)",
			//		Precedence.Primary,
			//		func.Parameters[2],      // value
			//		func.Parameters[0]);     // type

			//case PseudoFunctions.TRY_CONVERT_OR_DEFAULT:
			//	// COALESCE(CAST(x AS <type>?), default)
			//	return new SqlExpression(
			//			func.Type,
			//			"COALESCE(CAST({0} AS {1}?), {2})",
			//			Precedence.Primary,
			//			func.Parameters[2],    // value
			//			func.Parameters[0],    // type
			//			func.Parameters[3])    // default
			//	{
			//		CanBeNull =
			//				func.Parameters[2].CanBeNullable(NullabilityContext) ||
			//				func.Parameters[3].CanBeNullable(NullabilityContext)
			//	};

			////----------------------------------------------------------------
			//// CharIndex (there is no POSITION analog in YDB; using FIND)
			//case "CharIndex":
			//	switch (func.Parameters.Length)
			//	{
			//		// CharIndex(substr, str)
			//		case 2:
			//			return new SqlExpression(
			//				func.Type,
			//				"COALESCE(FIND({1}, {0}) + 1, 0)",
			//				Precedence.Primary,
			//				func.Parameters[0],    // substring
			//				func.Parameters[1]);   // source

			//		// CharIndex(substr, str, start)
			//		case 3:
			//			return new SqlExpression(
			//				func.Type,
			//				"COALESCE(FIND(SUBSTRING({1}, {2} - 1), {0}) + {2}, 0)",
			//				Precedence.Primary,
			//				func.Parameters[0],    // substring
			//				func.Parameters[1],    // source
			//				func.Parameters[2]);   // start
			//	}

			//	break;
		}

		//// ------------------------------------------------------------------
		//// CAST bool → CASE
		//// ------------------------------------------------------------------
		//protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		//{
		//	if (cast.SystemType.ToUnderlying() == typeof(bool) &&
		//	    !(cast.IsMandatory && cast.Expression.SystemType?.ToNullableUnderlying() == typeof(bool)) &&
		//	    cast.Expression is not SqlSearchCondition and not SqlCaseExpression)
		//	{
		//		// YDB not supporting CAST(condition AS bool),
		//		// cast to CASE WHEN ... THEN TRUE ELSE FALSE END
		//		return ConvertBooleanToCase(cast.Expression, cast.ToType);
		//	}

		//	cast = FloorBeforeConvert(cast);
		//	return base.ConvertConversion(cast);
		//}

		protected override ISqlExpression ConvertSqlCaseExpression(SqlCaseExpression element)
		{
			// ELSE required
			if (element.ElseExpression == null)
				return new SqlCaseExpression(element.Type, element.Cases, new SqlValue(element.Type, null));

			return base.ConvertSqlCaseExpression(element);
		}
	}
}
