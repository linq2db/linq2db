using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SapHana
{
	using SqlQuery;
	using SqlProvider;
	using LinqToDB.Mapping;

	partial class SapHanaSqlBuilder : BasicSqlBuilder
	{
		public SapHanaSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		public override int CommandCount(SqlStatement statement)
		{
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			var insertClause = Statement.GetInsertClause();
			if (insertClause != null)
			{
				var identityField = insertClause.Into!.GetIdentityField();
				var table = insertClause.Into;

				if (identityField == null || table == null)
					throw new SqlException("Identity field must be defined for '{0}'.", insertClause.Into.Name);

				StringBuilder.Append("SELECT MAX(");
				BuildExpression(identityField, false, true);
				StringBuilder.Append(") FROM ");
				BuildPhysicalTable(table, null);
			}
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaSqlBuilder(MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.TakeValue == null ? "LIMIT 4200000000 OFFSET {0}" : "OFFSET {0}";
		}

		public override bool IsNestedJoinParenthesisRequired { get { return true; } }

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null)
			{
				AppendIndent().Append("CREATE COLUMN TABLE ");
				BuildPhysicalTable(createTable.Table!, null);
			}
			else
			{
				var name = WithStringBuilder(
					new StringBuilder(),
					() =>
					{
						BuildPhysicalTable(createTable.Table!, null);
						return StringBuilder.ToString();
					});

				AppendIndent().AppendFormat(createTable.StatementHeader, name);
			}
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsUpdateInsert(insertOrUpdate);
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Int32         :
				case DataType.UInt16        :
					StringBuilder.Append("Integer");
					return;
				case DataType.Double:
					StringBuilder.Append("Double");
					return;
				case DataType.DateTime2     :
				case DataType.DateTime      :
				case DataType.Time:
					StringBuilder.Append("Timestamp");
					return;
				case DataType.SmallDateTime :
					StringBuilder.Append("SecondDate");
					return;
				case DataType.Boolean       :
					StringBuilder.Append("TinyInt");
					return;
				case DataType.Image:
					StringBuilder.Append("Blob");
					return;
				case DataType.Xml:
					StringBuilder.Append("Clob");
					return;
				case DataType.Guid:
					StringBuilder.Append("Char (36)");
					return;
				case DataType.NVarChar:
				case DataType.VarChar:
				case DataType.VarBinary:
					if (type.Type.Length == null || type.Type.Length > 5000 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(5000)");
						return;
					}
					break;
			}
			base.BuildDataTypeFromDataType(type, forCreateTable);
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
				StringBuilder.Append("FROM DUMMY").AppendLine();
			else
				base.BuildFromClause(statement, selectQuery);
		}

		public static bool TryConvertParameterSymbol { get; set; }

		private static List<char>? _convertParameterSymbols;
		public  static List<char>  ConvertParameterSymbols
		{
			get => _convertParameterSymbols == null ? (_convertParameterSymbols = new List<char>()) : _convertParameterSymbols;
			set => _convertParameterSymbols = value ?? new List<char>();
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return sb.Append(':').Append(value);

				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
				case ConvertType.SprocParameterToName:
					return sb.Append(value);

				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					{
						if (value.Length > 0 && value[0] == '"')
							return sb.Append(value);
						return sb.Append('"').Append(value).Append('"');
					}

				case ConvertType.NameToServer     :
				case ConvertType.NameToDatabase   :
				case ConvertType.NameToSchema     :
				case ConvertType.NameToQueryTable :
					if (value.Length > 0 && value[0] == '\"')
						return sb.Append(value);

					return sb.Append('"').Append(value).Append('"');
			}

			return sb.Append(value);
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("GENERATED BY DEFAULT AS IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("PRIMARY KEY (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
					wrap = expr is SqlExpression ex && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlSearchCondition;
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");
		}

		//this is for Tests.Linq.Common.CoalesceLike test
		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			switch (func.Name)
			{
				case "CASE": func = ConvertCase(func.SystemType, func.Parameters, 0);
					break;
			}
			base.BuildFunction(func);
		}

		//this is for Tests.Linq.Common.CoalesceLike test
		static SqlFunction ConvertCase(Type systemType, ISqlExpression[] parameters, int start)
		{
			var len  = parameters.Length - start;
			var cond = parameters[start];

			if (start == 0 && SqlExpression.NeedsEqual(cond))
			{
				cond = new SqlSearchCondition(
					new SqlCondition(
						false,
						new SqlPredicate.ExprExpr(cond, SqlPredicate.Operator.Equal, new SqlValue(1))));
			}

			const string name = "CASE";

			if (len == 3)
				return new SqlFunction(systemType, name, cond, parameters[start + 1], parameters[start + 2]);

			return new SqlFunction(systemType, name,
				cond,
				parameters[start + 1],
				ConvertCase(systemType, parameters, start + 2));
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table)
		{
			if (server   != null && server.Length == 0) server = null;
			if (schema   != null && schema.Length == 0) schema = null;

			// <table_name> ::= [[<linked_server_name>.]<schema_name>.]<identifier>
			if (server != null && schema == null)
				throw new LinqToDBException("You must specify schema name for linked server queries.");

			if (server != null)
				sb.Append(server).Append(".");

			if (schema != null)
				sb.Append(schema).Append(".");

			return sb.Append(table);
		}
	}
}
