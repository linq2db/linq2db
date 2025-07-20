using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	sealed class SybaseSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SybaseSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		// could be enabled if we add SP03 version support (also IsDistinctFromSupported should be enabled)
		//protected override bool SupportsDistinctAsExistsIntersect => true;

		#region LIKE

		private static string[] SybaseCharactersToEscape = {"_", "%", "[", "]", "^"};

		public override string[] LikeCharactersToEscape => SybaseCharactersToEscape;

		#endregion

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			/*var ftype = cast.SystemType.ToUnderlying();
			if (ftype == typeof(string))
			{
				var stype = cast.Expression.SystemType!.ToUnderlying();

				if (stype == typeof(DateTime)
#if NET8_0_OR_GREATER
							|| stype == typeof(DateOnly)
#endif
				   )
				{
					return new SqlFunction(cast.SystemType, "Convert", false, true, Precedence.Primary, ParametersNullabilityType.IfAllParametersNullable, null, new SqlDataType(cast.ToType), cast.Expression, new SqlValue(23));
				}
			}*/

			return base.ConvertConversion(cast);
		}

		protected override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
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
			switch (func)
			{
				case { Name: PseudoFunctions.REPLACE }:
					return func.WithName("Str_Replace");

				case { Name: PseudoFunctions.LENGTH }:
					return func.WithName("CHAR_LENGTH");

				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					Type: var type,
				}:
					return Add<int>(
						new SqlFunction(type, "CharIndex",
							p0,
							new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "Substring",
								p1,
								p2,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "Len", p1))),
						Sub(p2, 1));

				case {
					Name: "Stuff",
					Parameters:
					[
						var p0, var p1, _,
						SqlValue { Value: string @string, ValueType: var valueType }
					],
					Type: var type
				} when string.IsNullOrEmpty(@string):
					return new SqlFunction(
						type,
						"Stuff",
						ParametersNullabilityType.SameAsFirstParameter,
						p0,
						p1,
						p1,
						new SqlValue(valueType, null));

				default:
					return base.ConvertSqlFunction(func);
			};
		}
	}
}
