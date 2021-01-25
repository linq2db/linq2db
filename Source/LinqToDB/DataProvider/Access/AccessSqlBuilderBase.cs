using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.DataProvider.Access
{
	using Extensions;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	abstract class AccessSqlBuilderBase : BasicSqlBuilder
	{
		protected AccessSqlBuilderBase(
			MappingSchema       mappingSchema,
			ISqlOptimizer       sqlOptimizer,
			SqlProviderFlags    sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity ? 1 + trun.Table!.IdentityFields.Count : 1;
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				var field = trun.Table!.IdentityFields[commandNumber - 1];

				StringBuilder.Append("ALTER TABLE ");
				ConvertTableName(StringBuilder, trun.Table.Server, trun.Table.Database, trun.Table.Schema, trun.Table.PhysicalName!, trun.Table.TableOptions);
				StringBuilder.Append(" ALTER COLUMN ");
				Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				StringBuilder.AppendLine(" COUNTER(1, 1)");
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

		SqlColumn? _selectColumn;

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

			BuildSql(0, new SqlSelectStatement(query), StringBuilder, OptimizationContext);

			_selectColumn = null;
		}

		protected override IEnumerable<SqlColumn> GetSelectedColumns(SelectQuery selectQuery)
		{
			if (_selectColumn != null)
				return new[] { _selectColumn };

			if (NeedSkip(selectQuery.Select.TakeValue, selectQuery.Select.SkipValue) && !selectQuery.OrderBy.IsEmpty)
				return AlternativeGetSelectedColumns(selectQuery, () => base.GetSelectedColumns(selectQuery));

			return base.GetSelectedColumns(selectQuery);
		}

		#endregion

		protected override bool ParenthesizeJoin(List<SqlJoinedTable> joins)
		{
			return true;
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

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.DateTime2 : StringBuilder.Append("timestamp");                    break;
				default                 : base.BuildDataTypeFromDataType(type, forCreateTable); break;
			}
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append('@').Append(value);

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					if (value.Length > 0 && value[0] == '[')
							return sb.Append(value);

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.NameToDatabase  :
				case ConvertType.NameToSchema    :
				case ConvertType.NameToQueryTable:
					if (value.Length > 0 && value[0] == '[')
							return sb.Append(value);

					if (value.IndexOf('.') > 0)
						value = string.Join("].[", value.Split('.'));

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == '@'
						? sb.Append(value.Substring(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(string.Join(InlineComma, fieldNames));
			StringBuilder.Append(")");
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions)
		{
			if (database != null && database.Length == 0)
				database = null;

			if (database != null)
				sb.Append(database).Append(".");

			return sb.Append(table);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}
	}
}
