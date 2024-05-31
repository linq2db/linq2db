using System;

namespace LinqToDB.DataProvider.Sybase
{
	using LinqToDB.Extensions;
	using LinqToDB.SqlProvider;
	using LinqToDB.SqlQuery;

	public class SybaseSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SybaseSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

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
#if NET6_0_OR_GREATER
							|| stype == typeof(DateOnly)
#endif
				   )
				{
					return new SqlFunction(cast.SystemType, "Convert", false, true, Precedence.Primary, ParametersNullabilityType.IfAllParametersNullable, null, new SqlDataType(cast.ToType), cast.Expression, new SqlValue(23));
				}
			}*/

			return base.ConvertConversion(cast);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func)
			{
				case { Name: PseudoFunctions.REPLACE }:
					return func.WithName("Str_Replace");

				case { Name: "EXISTS", Parameters: [var sql] }
					when sql is SelectQuery selectQuery:
				{
					if (selectQuery.HasSetOperators)
					{
						var query = new SelectQuery() {DoNotRemove = true};
						query.From.Table(selectQuery);
						return func.WithParameters([query]);
					}
					return func;
				}

				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					SystemType: var type,
				}:
					return Add<int>(
						new SqlFunction(func.SystemType, "CharIndex",
							p0,
							new SqlFunction(typeof(string), "Substring",
								p1,
								p2,
								new SqlFunction(typeof(int), "Len", p1))),
						Sub(p2, 1));

				case {
					Name: "Stuff",
					Parameters:
					[
						var p0, var p1, _,
						SqlValue { Value: string @string, ValueType: var valueType }
					],
					SystemType: var type,
					Precedence: var precedence,
				} when string.IsNullOrEmpty(@string):
					return new SqlFunction(
						type,
						"Stuff",
						false,
						precedence,
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
