using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Access
{
	using Extensions;
	using SqlQuery;
	using SqlProvider;

	class AccessSqlBuilder : BasicSqlBuilder
	{
		public AccessSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity ? 1 + trun.Table.Fields.Values.Count(f => f.IsIdentity) : 1;
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				var field = trun.Table.Fields.Values.Skip(commandNumber - 1).First(f => f.IsIdentity);

				StringBuilder.Append("ALTER TABLE ");
				ConvertTableName(StringBuilder, trun.Table.Server, trun.Table.Database, trun.Table.Schema, trun.Table.PhysicalName);
				StringBuilder
					.Append(" ALTER COLUMN ")
					.Append(Convert(field.PhysicalName, ConvertType.NameToQueryField))
					.AppendLine(" COUNTER(1,1)")
					;
			}
			else
			{
				StringBuilder.AppendLine("SELECT @@IDENTITY");
			}
		}

		public override bool IsNestedJoinSupported => false;
		public override bool WrapJoinCondition     => true;

		#region Skip / Take Support

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "TOP {0}";
		}

		protected override void BuildSql()
		{
			var selectQuery = Statement.SelectQuery;
			if (selectQuery != null)
			{
				if (NeedSkip(selectQuery))
				{
					AlternativeBuildSql2(base.BuildSql);
					return;
				}

				if (selectQuery.From.Tables.Count == 0 && selectQuery.Select.Columns.Count == 1)
				{
					if (selectQuery.Select.Columns[0].Expression is SqlFunction func)
					{
						if (func.Name == "Iif" && func.Parameters.Length == 3 && func.Parameters[0] is SqlSearchCondition sc)
						{
							if (sc.Conditions.Count == 1 && sc.Conditions[0].Predicate is SqlPredicate.FuncLike p)
							{
								if (p.Function.Name == "EXISTS")
								{
									BuildAnyAsCount(selectQuery);
									return;
								}
							}
						}
					}
					else if (selectQuery.Select.Columns[0].Expression is SqlSearchCondition sc)
					{
						if (sc.Conditions.Count == 1 && sc.Conditions[0].Predicate is SqlPredicate.FuncLike p)
						{
							if (p.Function.Name == "EXISTS")
							{
								BuildAnyAsCount(selectQuery);
								return;
							}
						}
					}
				}
			}

			base.BuildSql();
		}

		SqlColumn _selectColumn;

		void BuildAnyAsCount(SelectQuery selectQuery)
		{
			SqlSearchCondition cond;

			if (selectQuery.Select.Columns[0].Expression is SqlFunction func)
			{
				cond  = (SqlSearchCondition)func.Parameters[0];
			}
			else
			{
				cond  = (SqlSearchCondition)selectQuery.Select.Columns[0].Expression;
			}

			var exist = ((SqlPredicate.FuncLike)cond.Conditions[0].Predicate).Function;
			var query = (SelectQuery)exist.Parameters[0];

			_selectColumn = new SqlColumn(selectQuery, new SqlExpression(cond.Conditions[0].IsNot ? "Count(*) = 0" : "Count(*) > 0"), selectQuery.Select.Columns[0].Alias);

			BuildSql(0, new SqlSelectStatement(query), StringBuilder);

			_selectColumn = null;
		}

		protected override IEnumerable<SqlColumn> GetSelectedColumns(SelectQuery selectQuery)
		{
			if (_selectColumn != null)
				return new[] { _selectColumn };

			if (NeedSkip(selectQuery) && !selectQuery.OrderBy.IsEmpty)
				return AlternativeGetSelectedColumns(selectQuery, () => base.GetSelectedColumns(selectQuery));

			return base.GetSelectedColumns(selectQuery);
		}

		protected override void BuildSkipFirst(SelectQuery selectQuery)
		{
			if (NeedSkip(selectQuery))
			{
				if (!NeedTake(selectQuery))
				{
					StringBuilder.AppendFormat(" TOP {0}", int.MaxValue);
				}
				else if (!selectQuery.OrderBy.IsEmpty)
				{
					StringBuilder.Append(" TOP ");
					BuildExpression(Add<int>(selectQuery.Select.SkipValue, selectQuery.Select.TakeValue));
				}
			}
			else
				base.BuildSkipFirst(selectQuery);
		}

		#endregion

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override bool ParenthesizeJoin(List<SqlJoinedTable> tsJoins)
		{
			return true;
		}

		protected override void BuildLikePredicate(SqlPredicate.Like predicate)
		{
			if (predicate.Expr2 is SqlValue)
			{
				var value = ((SqlValue)predicate.Expr2).Value;

				if (value != null)
				{
					var text  = ((SqlValue)predicate.Expr2).Value.ToString();
					var ntext = text.Replace("[", "[[]");

					if (text != ntext)
						predicate = new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, new SqlValue(ntext), predicate.Escape);
				}
			}
			else if (predicate.Expr2 is SqlParameter)
			{
				var p = ((SqlParameter)predicate.Expr2);
				p.ReplaceLike = true;
			}

			if (predicate.Escape != null)
			{
				if (predicate.Expr2 is SqlValue && predicate.Escape is SqlValue)
				{
					var value = ((SqlValue)predicate.Expr2).Value;

					if (value != null)
					{
						var text = ((SqlValue)predicate.Expr2).Value.ToString();
						var val  = new SqlValue(ReescapeLikeText(text, (char)((SqlValue)predicate.Escape).Value));

						predicate = new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, val, null);
					}
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
							p         = new SqlParameter(p.SystemType, p.Name, value) { DbSize = p.DbSize, DataType = p.DataType, IsQueryParameter = p.IsQueryParameter };
							predicate = new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, p, null);
						}
					}
				}
			}

			base.BuildLikePredicate(predicate);
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

		protected override void BuildBinaryExpression(SqlBinaryExpression expr)
		{
			switch (expr.Operation[0])
			{
				case '%': expr = new SqlBinaryExpression(expr.SystemType, expr.Expr1, "MOD", expr.Expr2, Precedence.Additive - 1); break;
				case '&':
				case '|':
				case '^': throw new SqlException("Operator '{0}' is not supported by the {1}.", expr.Operation, GetType().Name);
			}

			base.BuildBinaryExpression(expr);
		}

		protected override void BuildFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "Coalesce"  :

					if (func.Parameters.Length > 2)
					{
						var parms = new ISqlExpression[func.Parameters.Length - 1];

						Array.Copy(func.Parameters, 1, parms, 0, parms.Length);
						BuildFunction(new SqlFunction(func.SystemType, func.Name, func.Parameters[0],
							new SqlFunction(func.SystemType, func.Name, parms)));
						return;
					}

					var sc = new SqlSearchCondition();

					sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(func.Parameters[0], false)));

					func = new SqlFunction(func.SystemType, "Iif", sc, func.Parameters[1], func.Parameters[0]);

					break;

				case "CASE"      : func = ConvertCase(func.SystemType, func.Parameters, 0); break;
				case "CharIndex" :
					func = func.Parameters.Length == 2?
						new SqlFunction(func.SystemType, "InStr", new SqlValue(1),    func.Parameters[1], func.Parameters[0], new SqlValue(1)):
						new SqlFunction(func.SystemType, "InStr", func.Parameters[2], func.Parameters[1], func.Parameters[0], new SqlValue(1));
					break;

				case "Convert"   :
					switch (func.SystemType.ToUnderlying().GetTypeCodeEx())
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

							BuildExpression(func.Parameters[1]);

							return;
					}

					break;
			}

			base.BuildFunction(func);
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

		protected override void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			base.BuildFromClause(statement, selectQuery);
			StringBuilder.Remove(0, 4).Insert(0, "UPDATE");
			base.BuildUpdateSet(selectQuery, updateClause);
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (!statement.IsUpdate())
				base.BuildFromClause(statement, selectQuery);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.DateTime2 : StringBuilder.Append("timestamp");      break;
				default                 : base.BuildDataType(type, createDbType); break;
			}
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

				case ConvertType.NameToDatabase  :
				case ConvertType.NameToSchema    :
				case ConvertType.NameToQueryTable:
					if (value != null)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;

						if (name.IndexOf('.') > 0)
							value = string.Join("].[", name.Split('.'));

						return "[" + value + "]";
					}

					break;

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

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string server, string database, string schema, string table)
		{
			if (database != null && database.Length == 0) database = null;

			if (database != null)
				sb.Append(database).Append(".");

			return sb.Append(table);
		}

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			return ((System.Data.OleDb.OleDbParameter)parameter).OleDbType.ToString();
		}
	}
}
