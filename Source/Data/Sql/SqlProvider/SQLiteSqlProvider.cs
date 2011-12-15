using System;
using System.Text;
using LinqToDB.Extensions;
using LinqToDB.Reflection;

namespace LinqToDB.Data.Sql.SqlProvider
{
	using DataProvider;

	public class SQLiteSqlProvider : BasicSqlProvider
	{
		public override int CommandCount(SqlQuery sqlQuery)
		{
			return sqlQuery.IsInsert && sqlQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			sb.AppendLine("SELECT last_insert_rowid()");
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SQLiteSqlProvider();
		}

		protected override string LimitFormat  { get { return "LIMIT {0}";  } }
		protected override string OffsetFormat { get { return "OFFSET {0}"; } }

		public override bool IsSkipSupported           { get { return SqlQuery.Select.TakeValue != null; } }
		public override bool IsNestedJoinSupported     { get { return false; } }
		public override bool IsInsertOrUpdateSupported { get { return false; } }

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
					case "^": // (a + b) - (a & b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlBinaryExpression(be.SystemType, be.Expr1, "&", be.Expr2), 2), be.SystemType);
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Space"   : return new SqlFunction(func.SystemType, "PadR", new SqlValue(" "), func.Parameters[0]);
					case "Convert" :
						{
							var ftype = ReflectionExtensions.GetUnderlyingType(func.SystemType);

							if (ftype == typeof(bool))
							{
								var ex = AlternativeConvertToBoolean(func, 1);
								if (ex != null)
									return ex;
							}

							if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset))
							{
								if (IsDateDataType(func.Parameters[0], "Date"))
									return new SqlFunction(func.SystemType, "Date", func.Parameters[1]);
								return new SqlFunction(func.SystemType, "DateTime", func.Parameters[1]);
							}

							return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, func.Parameters[1], func.Parameters[0]);
						}
				}
			}
			else if (expr is SqlExpression)
			{
				var e = (SqlExpression)expr;

				if (e.Expr.StartsWith("Cast(StrFTime(Quarter"))
					return Inc(Div(Dec(new SqlExpression(e.SystemType, e.Expr.Replace("Cast(StrFTime(Quarter", "Cast(StrFTime('%m'"), e.Parameters)), 3));

				if (e.Expr.StartsWith("Cast(StrFTime('%w'"))
					return Inc(new SqlExpression(e.SystemType, e.Expr.Replace("Cast(StrFTime('%w'", "Cast(strFTime('%w'"), e.Parameters));

				if (e.Expr.StartsWith("Cast(StrFTime('%f'"))
					return new SqlExpression(e.SystemType, "Cast(strFTime('%f', {0}) * 1000 as int) % 1000", Precedence.Multiplicative, e.Parameters);

				if (e.Expr.StartsWith("DateTime"))
				{
					if (e.Expr.EndsWith("Quarter')"))
						return new SqlExpression(e.SystemType, "DateTime({1}, '{0} Month')", Precedence.Primary, Mul(e.Parameters[0], 3), e.Parameters[1]);

					if (e.Expr.EndsWith("Week')"))
						return new SqlExpression(e.SystemType, "DateTime({1}, '{0} Day')",   Precedence.Primary, Mul(e.Parameters[0], 7), e.Parameters[1]);
				}
			}

			return expr;
		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			sqlQuery = base.Finalize(sqlQuery);

			switch (sqlQuery.QueryType)
			{
				case QueryType.Delete :
					sqlQuery = GetAlternativeDelete(base.Finalize(sqlQuery));
					sqlQuery.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					sqlQuery = GetAlternativeUpdate(sqlQuery);
					break;
			}

			return sqlQuery;
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SqlQuery.IsUpdate)
				base.BuildFromClause(sb);
		}

		public override void BuildValue(StringBuilder sb, object value)
		{
			if (value is Guid)
			{
				var s = ((Guid)value).ToString("N");

				sb
					.Append("Cast(x'")
					.Append(s.Substring( 6,  2))
					.Append(s.Substring( 4,  2))
					.Append(s.Substring( 2,  2))
					.Append(s.Substring( 0,  2))
					.Append(s.Substring(10,  2))
					.Append(s.Substring( 8,  2))
					.Append(s.Substring(14,  2))
					.Append(s.Substring(12,  2))
					.Append(s.Substring(16, 16))
					.Append("' as blob)");
			}
			else
				base.BuildValue(sb, value);
		}
		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return "@" + value;

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;
					}

					return "[" + value + "]";

				case ConvertType.NameToDatabase:
				case ConvertType.NameToOwner:
				case ConvertType.NameToQueryTable:
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;

						if (name.IndexOf('.') > 0)
							value = string.Join("].[", name.Split('.'));
					}

					return "[" + value + "]";

				case ConvertType.SprocParameterToName:
					{
						var name = (string)value;
						return name.Length > 0 && name[0] == '@'? name.Substring(1): name;
					}
			}

			return value;
		}
	}
}
