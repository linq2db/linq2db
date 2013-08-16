using System;
using System.Linq;
using System.Text;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable SuggestUseVarKeywordEvident
#endregion

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using SqlQuery;
	using SqlProvider;

	class FirebirdSqlBuilder : BasicSqlBuilder
	{
		public FirebirdSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlProvider()
		{
			return new FirebirdSqlBuilder(SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildSelectClause(StringBuilder sb)
		{
			if (SelectQuery.From.Tables.Count == 0)
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

		protected override void BuildGetIdentity(StringBuilder sb)
		{
			var identityField = SelectQuery.Insert.Into.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", SelectQuery.Insert.Into.Name);

			AppendIndent(sb).AppendLine("RETURNING");
			AppendIndent(sb).Append("\t");
			BuildExpression(sb, identityField, false, true);
		}

		public override ISqlExpression GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
				return new SqlExpression("GEN_ID(" + table.SequenceAttributes[0].SequenceName + ", 1)", Precedence.Primary);

			return base.GetIdentityExpression(table);
		}

		protected override void BuildFunction(StringBuilder sb, SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(sb, func);
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.Decimal       :
					base.BuildDataType(sb, type.Precision > 18 ? new SqlDataType(type.DataType, type.Type, 18, type.Scale) : type);
					break;
				case DataType.SByte         :
				case DataType.Byte          : sb.Append("SmallInt");        break;
				case DataType.Money         : sb.Append("Decimal(18,4)");   break;
				case DataType.SmallMoney    : sb.Append("Decimal(10,4)");   break;
#if !MONO
				case DataType.DateTime2     :
#endif
				case DataType.SmallDateTime :
				case DataType.DateTime      : sb.Append("TimeStamp");       break;
				case DataType.NVarChar      :
					sb.Append("VarChar");
					if (type.Length > 0)
						sb.Append('(').Append(type.Length).Append(')');
					break;
				default                      : base.BuildDataType(sb, type); break;
			}
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause(sb);
		}

		protected override void BuildColumnExpression(StringBuilder sb, ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SelectQuery.SearchCondition)
					wrap = true;
				else
				{
					var ex = expr as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SelectQuery.SearchCondition;
				}
			}

			if (wrap) sb.Append("CASE WHEN ");
			base.BuildColumnExpression(sb, expr, alias, ref addAlias);
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

		protected override void BuildCreateTableNullAttribute(StringBuilder sb, SqlField field)
		{
			if (!field.Nullable)
				sb.Append("NOT NULL");
		}

		SqlField _identityField;

		public override int CommandCount(SelectQuery selectQuery)
		{
			if (selectQuery.IsCreateTable)
			{
				_identityField = selectQuery.CreateTable.Table.Fields.Values.FirstOrDefault(f => f.IsIdentity);

				if (_identityField != null)
					return 3;
			}

			return base.CommandCount(selectQuery);
		}

		protected override void BuildDropTableStatement(StringBuilder sb)
		{
			if (_identityField == null)
			{
				base.BuildDropTableStatement(sb);
			}
			else
			{
				sb
					.Append("DROP TRIGGER TIDENTITY_")
					.Append(SelectQuery.CreateTable.Table.PhysicalName)
					.AppendLine();
			}
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			if (SelectQuery.CreateTable.IsDrop)
			{
				if (commandNumber == 1)
				{
					sb
						.Append("DROP GENERATOR GIDENTITY_")
						.Append(SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine();
				}
				else
					base.BuildDropTableStatement(sb);
			}
			else
			{
				if (commandNumber == 1)
				{
					sb
						.Append("CREATE GENERATOR GIDENTITY_")
						.Append(SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine();
				}
				else
				{
					sb
						.AppendFormat("CREATE TRIGGER TIDENTITY_{0} FOR {0}", SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine  ()
						.AppendLine  ("BEFORE INSERT POSITION 0")
						.AppendLine  ("AS BEGIN")
						.AppendFormat("\tNEW.{0} = GEN_ID(GIDENTITY_{1}, 1);", _identityField.PhysicalName, SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine  ()
						.AppendLine  ("END");
				}
			}
		}
	}
}
