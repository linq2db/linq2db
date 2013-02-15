using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Extensions;
	using SqlBuilder;
	using SqlProvider;

	public class AccessSqlProvider : BasicSqlProvider
	{
		public AccessSqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
			SqlProviderFlags.IsCountSubQuerySupported  = false;
			SqlProviderFlags.IsInsertOrUpdateSupported = false;
		}

		public override int CommandCount(SqlQuery sqlQuery)
		{
			return sqlQuery.IsInsert && sqlQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			sb.AppendLine("SELECT @@IDENTITY");
		}

		public override bool IsNestedJoinSupported     { get { return false; } }

		public override bool ConvertCountSubQuery(SqlQuery subQuery)
		{
			return !subQuery.Where.IsEmpty;
		}

		#region Skip / Take Support

		protected override string FirstFormat { get { return "TOP {0}"; } }

		protected override void BuildSql(StringBuilder sb)
		{
			if (NeedSkip)
			{
				AlternativeBuildSql2(sb, base.BuildSql);
				return;
			}

			if (SqlQuery.From.Tables.Count == 0 && SqlQuery.Select.Columns.Count == 1)
			{
				if (SqlQuery.Select.Columns[0].Expression is SqlFunction)
				{
					var func = (SqlFunction)SqlQuery.Select.Columns[0].Expression;

					if (func.Name == "Iif" && func.Parameters.Length == 3 && func.Parameters[0] is SqlQuery.SearchCondition)
					{
						var sc = (SqlQuery.SearchCondition)func.Parameters[0];

						if (sc.Conditions.Count == 1 && sc.Conditions[0].Predicate is SqlQuery.Predicate.FuncLike)
						{
							var p = (SqlQuery.Predicate.FuncLike)sc.Conditions[0].Predicate;

							if (p.Function.Name == "EXISTS")
							{
								BuildAnyAsCount(sb);
								return;
							}
						}
					}
				}
				else if (SqlQuery.Select.Columns[0].Expression is SqlQuery.SearchCondition)
				{
					var sc = (SqlQuery.SearchCondition)SqlQuery.Select.Columns[0].Expression;

					if (sc.Conditions.Count == 1 && sc.Conditions[0].Predicate is SqlQuery.Predicate.FuncLike)
					{
						var p = (SqlQuery.Predicate.FuncLike)sc.Conditions[0].Predicate;

						if (p.Function.Name == "EXISTS")
						{
							BuildAnyAsCount(sb);
							return;
						}
					}
				}
			}

			base.BuildSql(sb);
		}

		SqlQuery.Column _selectColumn;

		void BuildAnyAsCount(StringBuilder sb)
		{
			SqlQuery.SearchCondition cond;

			if (SqlQuery.Select.Columns[0].Expression is SqlFunction)
			{
				var func  = (SqlFunction)SqlQuery.Select.Columns[0].Expression;
				cond  = (SqlQuery.SearchCondition)func.Parameters[0];
			}
			else
			{
				cond  = (SqlQuery.SearchCondition)SqlQuery.Select.Columns[0].Expression;
			}

			var exist = ((SqlQuery.Predicate.FuncLike)cond.Conditions[0].Predicate).Function;
			var query = (SqlQuery)exist.Parameters[0];

			_selectColumn = new SqlQuery.Column(SqlQuery, new SqlExpression(cond.Conditions[0].IsNot ? "Count(*) = 0" : "Count(*) > 0"), SqlQuery.Select.Columns[0].Alias);

			BuildSql(0, query, sb, 0, 0, false);

			_selectColumn = null;
		}

		protected override IEnumerable<SqlQuery.Column> GetSelectedColumns()
		{
			if (_selectColumn != null)
				return new[] { _selectColumn };

			if (NeedSkip && !SqlQuery.OrderBy.IsEmpty)
				return AlternativeGetSelectedColumns(base.GetSelectedColumns);

			return base.GetSelectedColumns();
		}

		protected override void BuildSkipFirst(StringBuilder sb)
		{
			if (NeedSkip)
			{
				if (!NeedTake)
				{
					sb.AppendFormat(" TOP {0}", int.MaxValue);
				}
				else if (!SqlQuery.OrderBy.IsEmpty)
				{
					sb.Append(" TOP ");
					BuildExpression(sb, Add<int>(SqlQuery.Select.SkipValue, SqlQuery.Select.TakeValue));
				}
			}
			else
				base.BuildSkipFirst(sb);
		}

		#endregion

		protected override ISqlProvider CreateSqlProvider()
		{
			return new AccessSqlProvider(SqlProviderFlags);
		}

		protected override bool ParenthesizeJoin()
		{
			return true;
		}

		protected override void BuildLikePredicate(StringBuilder sb, SqlQuery.Predicate.Like predicate)
		{
			if (predicate.Escape != null)
			{
				if (predicate.Expr2 is SqlValue && predicate.Escape is SqlValue)
				{
					var text = ((SqlValue)predicate.Expr2).Value.ToString();
					var val  = new SqlValue(ReescapeLikeText(text, (char)((SqlValue)predicate.Escape).Value));

					predicate = new SqlQuery.Predicate.Like(predicate.Expr1, predicate.IsNot, val, null);
				}
				else if (predicate.Expr2 is SqlParameter)
				{
					var p = (SqlParameter)predicate.Expr2;

					if (p.LikeStart != null)
					{
						var value = (string)p.Value;

						if (value != null)
						{
							value     = value.Replace("[", "[[]").Replace("~%", "[%]").Replace("~_", "[_]").Replace("~~", "[~]");
							p         = new SqlParameter(p.SystemType, p.Name, value) { DbSize = p.DbSize, DbType = p.DbType };
							predicate = new SqlQuery.Predicate.Like(predicate.Expr1, predicate.IsNot, p, null);
						}
					}
				}
			}

			base.BuildLikePredicate(sb, predicate);
		}

		static string ReescapeLikeText(string text, char esc)
		{
			var sb = new StringBuilder(text.Length);

			for (var i = 0; i < text.Length; i++)
			{
				var c = text[i];

				if (c == esc)
				{
					sb.Append('[');
					sb.Append(text[++i]);
					sb.Append(']');
				}
				else if (c == '[')
					sb.Append("[[]");
				else
					sb.Append(c);
			}

			return sb.ToString();
		}

		protected override void BuildBinaryExpression(StringBuilder sb, SqlBinaryExpression expr)
		{
			switch (expr.Operation[0])
			{
				case '%': expr = new SqlBinaryExpression(expr.SystemType, expr.Expr1, "MOD", expr.Expr2, Precedence.Additive - 1); break;
				case '&':
				case '|':
				case '^': throw new SqlException("Operator '{0}' is not supported by the {1}.", expr.Operation, GetType().Name);
			}

			base.BuildBinaryExpression(sb, expr);
		}

		protected override void BuildFunction(StringBuilder sb, SqlFunction func)
		{
			switch (func.Name)
			{
				case "Coalesce"  :

					if (func.Parameters.Length > 2)
					{
						var parms = new ISqlExpression[func.Parameters.Length - 1];

						Array.Copy(func.Parameters, 1, parms, 0, parms.Length);
						BuildFunction(sb, new SqlFunction(func.SystemType, func.Name, func.Parameters[0],
						                  new SqlFunction(func.SystemType, func.Name, parms)));
						return;
					}

					var sc = new SqlQuery.SearchCondition();

					sc.Conditions.Add(new SqlQuery.Condition(false, new SqlQuery.Predicate.IsNull(func.Parameters[0], false)));

					func = new SqlFunction(func.SystemType, "Iif", sc, func.Parameters[1], func.Parameters[0]);

					break;

				case "CASE"      : func = ConvertCase(func.SystemType, func.Parameters, 0); break;
				case "CharIndex" :
					func = func.Parameters.Length == 2?
						new SqlFunction(func.SystemType, "InStr", new SqlValue(1),    func.Parameters[1], func.Parameters[0], new SqlValue(1)):
						new SqlFunction(func.SystemType, "InStr", func.Parameters[2], func.Parameters[1], func.Parameters[0], new SqlValue(1));
					break;

				case "Convert"   :
					switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
					{
						case TypeCode.String   : func = new SqlFunction(func.SystemType, "CStr",  func.Parameters[1]); break;
						case TypeCode.DateTime :
							if (IsDateDataType(func.Parameters[0], "Date"))
								func = new SqlFunction(func.SystemType, "DateValue", func.Parameters[1]);
							else if (IsTimeDataType(func.Parameters[0]))
								func = new SqlFunction(func.SystemType, "TimeValue", func.Parameters[1]);
							else
								func = new SqlFunction(func.SystemType, "CDate", func.Parameters[1]);
							break;

						default:
							if (func.SystemType == typeof(DateTime))
								goto case TypeCode.DateTime;

							BuildExpression(sb, func.Parameters[1]);

							return;
					}

					break;
			}

			base.BuildFunction(sb, func);
		}

		SqlFunction ConvertCase(Type systemType, ISqlExpression[] parameters, int start)
		{
			var len = parameters.Length - start;

			if (len < 3)
				throw new SqlException("CASE statement is not supported by the {0}.", GetType().Name);

			if (len == 3)
				return new SqlFunction(systemType, "Iif", parameters[start], parameters[start + 1], parameters[start + 2]);

			return new SqlFunction(systemType, "Iif", parameters[start], parameters[start + 1], ConvertCase(systemType, parameters, start + 2));
		}

		public override void BuildValue(StringBuilder sb, object value)
		{
			if (value is bool)
				sb.Append(value);
			else if (value is Guid)
				sb.Append("'").Append(((Guid)value).ToString("B")).Append("'");
			else
				base.BuildValue(sb, value);
		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			sqlQuery = base.Finalize(sqlQuery);

			switch (sqlQuery.QueryType)
			{
				case QueryType.Delete : return GetAlternativeDelete(sqlQuery);
				default               : return sqlQuery;
			}
		}

		protected override void BuildUpdateClause(StringBuilder sb)
		{
			base.BuildFromClause(sb);
			sb.Remove(0, 4).Insert(0, "UPDATE");
			base.BuildUpdateSet(sb);
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SqlQuery.IsUpdate)
				base.BuildFromClause(sb);
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

		protected override void BuildDateTime(StringBuilder sb, object value)
		{
			sb.Append(string.Format("#{0:yyyy-MM-dd HH:mm:ss}#", value));
		}
	}
}
