using System;
using System.Data;
using System.Text;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Sql.SqlProvider
{
	using DataProvider;
	using Reflection;

	public class SqlCeSqlProvider : BasicSqlProvider
	{
		const int Version = 4;

		public override bool IsSkipSupported           { get { return Version == 4; } }
		public override bool IsTakeSupported           { get { return Version == 4; } }
		public override bool IsSubQueryTakeSupported   { get { return Version == 4; } }
		public override bool IsSubQueryColumnSupported { get { return false; } }
		public override bool IsCountSubQuerySupported  { get { return false; } }
		public override bool IsApplyJoinSupported      { get { return true;  } }
		public override bool IsInsertOrUpdateSupported { get { return false; } }

		protected override string FirstFormat  { get { return SqlQuery.Select.SkipValue == null ? "TOP ({0})" :                null; } }
		protected override string LimitFormat  { get { return SqlQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null; } }
		protected override string OffsetFormat { get { return "OFFSET {0} ROWS"; } }
		protected override bool   OffsetFirst  { get { return true;              } }

		public override int CommandCount(SqlQuery sqlQuery)
		{
			return sqlQuery.IsInsert && sqlQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			sb.AppendLine("SELECT @@IDENTITY");
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlCeSqlProvider();
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%":
						return be.Expr1.SystemType.IsIntegerType()?
							be :
							new SqlBinaryExpression(
								typeof(int),
								new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, be.Expr1),
								be.Operation,
								be.Expr2,
								be.Precedence);
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction)expr;

				switch (func.Name)
				{
					case "Convert" :
						switch (Type.GetTypeCode(func.SystemType.GetUnderlyingType()))
						{
							case TypeCode.UInt64 :
								if (func.Parameters[1].SystemType.IsFloatType())
									return new SqlFunction(
										func.SystemType,
										func.Name,
										func.Precedence,
										func.Parameters[0],
										new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

								break;

							case TypeCode.DateTime :
								var type1 = func.Parameters[1].SystemType.GetUnderlyingType();

								if (IsTimeDataType(func.Parameters[0]))
								{
									if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
										return new SqlExpression(
											func.SystemType, "Cast(Convert(NChar, {0}, 114) as DateTime)", Precedence.Primary, func.Parameters[1]);

									if (func.Parameters[1].SystemType == typeof(string))
										return func.Parameters[1];

									return new SqlExpression(
										func.SystemType, "Convert(NChar, {0}, 114)", Precedence.Primary, func.Parameters[1]);
								}

								if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
								{
									if (IsDateDataType(func.Parameters[0], "Datetime"))
										return new SqlExpression(
											func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, func.Parameters[1]);
								}

								break;
						}

						break;
				}
			}

			return expr;
		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			sqlQuery = base.Finalize(sqlQuery);

			new QueryVisitor().Visit(sqlQuery.Select, element =>
			{
				if (element.ElementType == QueryElementType.SqlParameter)
				{
					((SqlParameter)element).IsQueryParameter = false;
					sqlQuery.ParameterDependent = true;
				}
			});

			switch (sqlQuery.QueryType)
			{
				case QueryType.Delete :
					sqlQuery = GetAlternativeDelete(sqlQuery);
					sqlQuery.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					sqlQuery = GetAlternativeUpdate(sqlQuery);
					break;
			}

			return sqlQuery;
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type)
		{
			switch (type.SqlDbType)
			{
				case SqlDbType.Char          : base.BuildDataType(sb, new SqlDataType(SqlDbType.NChar,    type.Length)); break;
				case SqlDbType.VarChar       : base.BuildDataType(sb, new SqlDataType(SqlDbType.NVarChar, type.Length)); break;
				case SqlDbType.SmallMoney    : sb.Append("Decimal(10,4)");   break;
#if !MONO
				case SqlDbType.DateTime2     :
#endif
				case SqlDbType.Time          :
				case SqlDbType.Date          :
				case SqlDbType.SmallDateTime : sb.Append("DateTime");        break;
				default                      : base.BuildDataType(sb, type); break;
			}
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SqlQuery.IsUpdate)
				base.BuildFromClause(sb);
		}

		protected override void BuildOrderByClause(StringBuilder sb)
		{
			if (SqlQuery.OrderBy.Items.Count == 0 && SqlQuery.Select.SkipValue != null)
			{
				AppendIndent(sb);

				sb.Append("ORDER BY").AppendLine();

				Indent++;

				AppendIndent(sb);

				BuildExpression(sb, SqlQuery.Select.Columns[0].Expression);
				sb.AppendLine();

				Indent--;
			}
			else
				base.BuildOrderByClause(sb);
		}

		protected override void BuildColumn(StringBuilder sb, SqlQuery.Column col, ref bool addAlias)
		{
			var wrap = false;

			if (col.SystemType == typeof(bool))
			{
				if (col.Expression is SqlQuery.SearchCondition)
					wrap = true;
				else
				{
					var ex = col.Expression as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlQuery.SearchCondition;
				}
			}

			if (wrap) sb.Append("CASE WHEN ");
			base.BuildColumn(sb, col, ref addAlias);
			if (wrap) sb.Append(" THEN 1 ELSE 0 END");
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
					if (value != null)
					{
						var str = value.ToString();
						return str.Length > 0 && str[0] == '@'? str.Substring(1): str;
					}
					break;
			}

			return value;
		}
	}
}
