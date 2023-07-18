using System;

using LinqToDB.Extensions;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Sybase
{
	public class SybaseSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SybaseSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		#region LIKE

		private static string[] SybaseCharactersToEscape = {"_", "%", "[", "]", "^"};

		public override string[] LikeCharactersToEscape => SybaseCharactersToEscape;

		#endregion

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case PseudoFunctions.REPLACE: return func.WithName("Str_Replace");

				case "CharIndex":
				{
					if (func.Parameters.Length == 3)
						return Add<int>(
							new SqlFunction(func.SystemType, "CharIndex",
								func.Parameters[0],
								new SqlFunction(typeof(string), "Substring",
									func.Parameters[1],
									func.Parameters[2],
									new SqlFunction(typeof(int), "Len", func.Parameters[1]))),
							Sub(func.Parameters[2], 1));
					break;
				}

				case "Stuff":
				{
					if (func.Parameters[3] is SqlValue value)
					{
						if (value.Value is string @string && string.IsNullOrEmpty(@string))
							return new SqlFunction(
								func.SystemType,
								func.Name,
								false,
								func.Precedence,
								func.Parameters[0],
								func.Parameters[1],
								func.Parameters[1],
								new SqlValue(value.ValueType, null));
					}

					break;
				}

				case PseudoFunctions.CONVERT:
				{
					var ftype = func.SystemType.ToUnderlying();
					if (ftype == typeof(string))
					{
						var stype = func.Parameters[2].SystemType!.ToUnderlying();

						if (stype == typeof(DateTime)
#if NET6_0_OR_GREATER
							|| stype == typeof(DateOnly)
#endif
						   )
						{
							return new SqlFunction(func.SystemType, "convert", false, true, func.Parameters[0], func.Parameters[2], new SqlValue(23))
							{
								CanBeNull = func.CanBeNull
							};
						}
					}

					break;
				}
			}

			return base.ConvertSqlFunction(func);
		}

	}
}
