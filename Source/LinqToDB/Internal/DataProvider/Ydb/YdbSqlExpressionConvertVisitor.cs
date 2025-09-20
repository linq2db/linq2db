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

		///// <inheritdoc/>
		//protected override bool SupportsNullInColumn => false;

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

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "+" when element.Type.DataType == DataType.Undefined && element.SystemType == typeof(string):
				case "+" when element.Type.DataType
					is DataType.NVarChar
					or DataType.NChar
					or DataType.Char
					or DataType.VarChar
					or DataType.Binary
					or DataType.VarBinary
					or DataType.Blob:
					return new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence);

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
			switch (func.Name)
			{
				case PseudoFunctions.TO_LOWER:
					return func.WithName("Unicode::ToLower");

				case PseudoFunctions.TO_UPPER:
					return func.WithName("Unicode::ToUpper");

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

			return base.ConvertSqlFunction(func);
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
	}
}
