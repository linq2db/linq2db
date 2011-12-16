using System;
using System.Data;
using System.Text;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable SuggestUseVarKeywordEvident
#endregion

namespace LinqToDB.SqlProvider
{
	using Extensions;
	using Mapping;
	using Data.DataProvider;
	using Sql;

	public class FirebirdSqlProvider : BasicSqlProvider, IMappingSchemaProvider
	{
		protected override ISqlProvider CreateSqlProvider()
		{
			return new FirebirdSqlProvider();
		}

		protected override void BuildSelectClause(StringBuilder sb)
		{
			if (SqlQuery.From.Tables.Count == 0)
			{
				AppendIndent(sb);
				sb.Append("SELECT").AppendLine();
				BuildColumns(sb);
				AppendIndent(sb);
				sb.Append("FROM rdb$database").AppendLine();
			}
			else
				base.BuildSelectClause(sb);
		}

		protected override bool   SkipFirst   { get { return false;       } }
		protected override string SkipFormat  { get { return "SKIP {0}";  } }
		protected override string FirstFormat { get { return "FIRST {0}"; } }

		public override bool IsIdentityParameterRequired { get { return true; } }

		protected override void BuildGetIdentity(StringBuilder sb)
		{
			var identityField = SqlQuery.Insert.Into.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", SqlQuery.Insert.Into.Name);

			AppendIndent(sb).AppendLine("RETURNING");
			AppendIndent(sb).Append("\t");
			BuildExpression(sb, identityField, false, true);
		}

		public override ISqlExpression GetIdentityExpression(SqlTable table, SqlField identityField, bool forReturning)
		{
			if (table.SequenceAttributes != null)
				return new SqlExpression("GEN_ID(" + table.SequenceAttributes[0].SequenceName + ", 1)", Precedence.Primary);

			return base.GetIdentityExpression(table, identityField, forReturning);
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				SqlBinaryExpression be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%": return new SqlFunction(be.SystemType, "Mod",     be.Expr1, be.Expr2);
					case "&": return new SqlFunction(be.SystemType, "Bin_And", be.Expr1, be.Expr2);
					case "|": return new SqlFunction(be.SystemType, "Bin_Or",  be.Expr1, be.Expr2);
					case "^": return new SqlFunction(be.SystemType, "Bin_Xor", be.Expr1, be.Expr2);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				SqlFunction func = (SqlFunction)expr;

				switch (func.Name)
				{
					case "Convert" :
						if (func.SystemType.GetUnderlyingType() == typeof(bool))
						{
							ISqlExpression ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);

					case "DateAdd" :
						switch ((Data.Linq.Sql.DateParts)((SqlValue)func.Parameters[0]).Value)
						{
							case Data.Linq.Sql.DateParts.Quarter  :
								return new SqlFunction(func.SystemType, func.Name, new SqlValue(Data.Linq.Sql.DateParts.Month), Mul(func.Parameters[1], 3), func.Parameters[2]);
							case Data.Linq.Sql.DateParts.DayOfYear:
							case Data.Linq.Sql.DateParts.WeekDay:
								return new SqlFunction(func.SystemType, func.Name, new SqlValue(Data.Linq.Sql.DateParts.Day),   func.Parameters[1],         func.Parameters[2]);
							case Data.Linq.Sql.DateParts.Week     :
								return new SqlFunction(func.SystemType, func.Name, new SqlValue(Data.Linq.Sql.DateParts.Day),   Mul(func.Parameters[1], 7), func.Parameters[2]);
						}

						break;
				}
			}
			else if (expr is SqlExpression)
			{
				SqlExpression e = (SqlExpression)expr;

				if (e.Expr.StartsWith("Extract(Quarter"))
					return Inc(Div(Dec(new SqlExpression(e.SystemType, "Extract(Month from {0})", e.Parameters)), 3));

				if (e.Expr.StartsWith("Extract(YearDay"))
					return Inc(new SqlExpression(e.SystemType, e.Expr.Replace("Extract(YearDay", "Extract(yearDay"), e.Parameters));

				if (e.Expr.StartsWith("Extract(WeekDay"))
					return Inc(new SqlExpression(e.SystemType, e.Expr.Replace("Extract(WeekDay", "Extract(weekDay"), e.Parameters));
			}

			return expr;
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type)
		{
			switch (type.SqlDbType)
			{
				case SqlDbType.Decimal       :
					base.BuildDataType(sb, type.Precision > 18 ? new SqlDataType(type.SqlDbType, type.Type, 18, type.Scale) : type);
					break;
				case SqlDbType.TinyInt       : sb.Append("SmallInt");        break;
				case SqlDbType.Money         : sb.Append("Decimal(18,4)");   break;
				case SqlDbType.SmallMoney    : sb.Append("Decimal(10,4)");   break;
#if !MONO
				case SqlDbType.DateTime2     :
#endif
				case SqlDbType.SmallDateTime :
				case SqlDbType.DateTime      : sb.Append("TimeStamp");       break;
				case SqlDbType.NVarChar      :
					sb.Append("VarChar");
					if (type.Length > 0)
						sb.Append('(').Append(type.Length).Append(')');
					break;
				default                      : base.BuildDataType(sb, type); break;
			}
		}

		static void SetNonQueryParameter(IQueryElement element)
		{
			if (element.ElementType == QueryElementType.SqlParameter)
				((SqlParameter)element).IsQueryParameter = false;
		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			CheckAliases(sqlQuery, int.MaxValue);

			new QueryVisitor().Visit(sqlQuery.Select, SetNonQueryParameter);

			if (sqlQuery.QueryType == QueryType.InsertOrUpdate)
			{
				foreach (var key in sqlQuery.Insert.Items)
					new QueryVisitor().Visit(key.Expression, SetNonQueryParameter);

				foreach (var key in sqlQuery.Update.Items)
					new QueryVisitor().Visit(key.Expression, SetNonQueryParameter);

				foreach (var key in sqlQuery.Update.Keys)
					new QueryVisitor().Visit(key.Expression, SetNonQueryParameter);
			}

			new QueryVisitor().Visit(sqlQuery, element =>
			{
				if (element.ElementType == QueryElementType.InSubQueryPredicate)
					new QueryVisitor().Visit(((SqlQuery.Predicate.InSubQuery)element).Expr1, SetNonQueryParameter);
			});

			sqlQuery = base.Finalize(sqlQuery);

			switch (sqlQuery.QueryType)
			{
				case QueryType.Delete : return GetAlternativeDelete(sqlQuery);
				case QueryType.Update : return GetAlternativeUpdate(sqlQuery);
				default               : return sqlQuery;
			}
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SqlQuery.IsUpdate)
				base.BuildFromClause(sb);
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

		public static bool QuoteIdentifiers = false;

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryTable:
					if (QuoteIdentifiers)
					{
						string name = value.ToString();

						if (name.Length > 0 && name[0] == '"')
							return value;

						return '"' + name + '"';
					}

					break;

				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return "@" + value;

				case ConvertType.SprocParameterToName:
					if (value != null)
					{
						string str = value.ToString();
						return str.Length > 0 && str[0] == '@' ? str.Substring(1) : str;
					}

					break;
			}

			return value;
		}

		protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			BuildInsertOrUpdateQueryAsMerge(sb, "FROM rdb$database");
		}

		#region IMappingSchemaProvider Members

		readonly FirebirdMappingSchema _mappingSchema = new FirebirdMappingSchema();

		MappingSchema IMappingSchemaProvider.MappingSchema
		{
			get { return _mappingSchema; }
		}

		#endregion
	}
}
