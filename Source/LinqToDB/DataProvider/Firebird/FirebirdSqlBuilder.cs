﻿using System;
using System.Data;
using System.Linq;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable SuggestUseVarKeywordEvident
#endregion

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using SqlQuery;
	using SqlProvider;
	using System.Text;

	public class FirebirdSqlBuilder : BasicSqlBuilder
	{
		public FirebirdSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new FirebirdSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent();
				StringBuilder.Append("SELECT").AppendLine();
				BuildColumns(selectQuery);
				AppendIndent();
				StringBuilder.Append("FROM rdb$database").AppendLine();
			}
			else if (selectQuery.Select.IsDistinct)
			{
				AppendIndent();
				StringBuilder.Append("SELECT");
				BuildSkipFirst(selectQuery);
				StringBuilder.Append(" DISTINCT");
				StringBuilder.AppendLine();
				BuildColumns(selectQuery);
			}
			else
				base.BuildSelectClause(selectQuery);
		}

		protected override bool   SkipFirst  => false;
		protected override string SkipFormat => "SKIP {0}";

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "FIRST {0}";
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", insertClause.Into.Name);

			AppendIndent().AppendLine("RETURNING");
			AppendIndent().Append("\t");
			BuildExpression(identityField, false, true);
		}

		public override ISqlExpression GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
				return new SqlExpression("GEN_ID(" + table.SequenceAttributes[0].SequenceName + ", 1)", Precedence.Primary);

			return base.GetIdentityExpression(table);
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.Decimal       :
					base.BuildDataType(type.Precision > 18 ? new SqlDataType(type.DataType, type.Type, null, 18, type.Scale) : type, createDbType);
					break;
				case DataType.SByte         :
				case DataType.Byte          : StringBuilder.Append("SmallInt");        break;
				case DataType.Money         : StringBuilder.Append("Decimal(18,4)");   break;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10,4)");   break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
				case DataType.DateTime      : StringBuilder.Append("TimeStamp");       break;
				case DataType.NVarChar      :
					StringBuilder.Append("VarChar");
					if (type.Length > 0)
						StringBuilder.Append('(').Append(type.Length).Append(')');
					StringBuilder.Append(" CHARACTER SET UNICODE_FSS");
					break;
				default                      : base.BuildDataType(type, createDbType); break;
			}
		}

//		protected override void BuildDataType(SqlDataType type, bool createDbType = false)
//		{
//			switch (type.DataType)
//			{
//				case DataType.DateTimeOffset :
//				case DataType.DateTime2      :
//				case DataType.Time           :
//				case DataType.Date           : StringBuilder.Append("DateTime"); return;
//				case DataType.Xml            : StringBuilder.Append("NText");    return;
//				case DataType.NVarChar       :
//
//					if (type.Length == int.MaxValue || type.Length < 0)
//					{
//						StringBuilder
//							.Append(type.DataType)
//							.Append("(4000)");
//						return;
//					}
//
//					break;
//
//				case DataType.VarChar        :
//				case DataType.VarBinary      :
//
//					if (type.Length == int.MaxValue || type.Length < 0)
//					{
//						StringBuilder
//							.Append(type.DataType)
//							.Append("(8000)");
//						return;
//					}
//
//					break;
//			}
//
//			base.BuildDataType(type, createDbType);
//		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (!statement.IsUpdate())
				base.BuildFromClause(statement, selectQuery);
		}

		protected override void BuildColumnExpression(SelectQuery selectQuery, ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
					wrap =
						expr is SqlExpression ex      &&
						ex.Expr              == "{0}" &&
						ex.Parameters.Length == 1     &&
						ex.Parameters[0] is SqlSearchCondition;
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");
		}

		/// <summary>
		/// Specifies how identifiers like table and field names should be quoted.
		/// </summary>
		/// <remarks>
		/// By default identifiers will not be quoted.
		/// </remarks>
		public static FirebirdIdentifierQuoteMode IdentifierQuoteMode = FirebirdIdentifierQuoteMode.None;

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryFieldAlias :
				case ConvertType.NameToQueryField      :
				case ConvertType.NameToQueryTable      :
					if (value != null && IdentifierQuoteMode != FirebirdIdentifierQuoteMode.None)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '"')
							return name;

						if (IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Quote ||
							name.StartsWith("_") ||
							name.Any(c => char.IsLower(c) || char.IsWhiteSpace(c)))
							return '"' + name + '"';
					}

					break;

				case ConvertType.NameToQueryParameter  :
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter  :
					return "@" + value;

				case ConvertType.SprocParameterToName  :
					if (value != null)
					{
						string str = value.ToString();
						return str.Length > 0 && str[0] == '@' ? str.Substring(1) : str;
					}

					break;
			}

			return value;
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, "FROM rdb$database");
		}

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (!field.CanBeNull)
				StringBuilder.Append("NOT NULL");
		}

		SqlField _identityField;

		public override int CommandCount(SqlStatement statement)
		{
			switch (statement)
			{
				case SqlTruncateTableStatement truncate:
					return truncate.ResetIdentity && truncate.Table.Fields.Values.Any(f => f.IsIdentity) ? 2 : 1;

				case SqlCreateTableStatement createTable:
					_identityField = createTable.Table.Fields.Values.FirstOrDefault(f => f.IsIdentity);
					if (_identityField != null)
						return 3;
					break;

				case SqlDropTableStatement dropTable:
					_identityField = dropTable.Table.Fields.Values.FirstOrDefault(f => f.IsIdentity);
					if (_identityField != null)
						return 3;
					break;
			}

			return base.CommandCount(statement);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			if (_identityField == null)
			{
				base.BuildDropTableStatement(dropTable);
			}
			else
			{
				StringBuilder
					.Append("DROP TRIGGER TIDENTITY_")
					.Append(dropTable.Table.PhysicalName)
					.AppendLine();
			}
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			switch (Statement)
			{
				case SqlTruncateTableStatement truncate:
					StringBuilder
						.Append("SET GENERATOR GIDENTITY_")
						.Append(truncate.Table.PhysicalName)
						.AppendLine(" TO 0")
						;
					break;

				case SqlDropTableStatement dropTable:
					{
						if (commandNumber == 1)
						{
							StringBuilder
								.Append("DROP GENERATOR GIDENTITY_")
								.Append(dropTable.Table.PhysicalName)
								.AppendLine();
						}
						else
							base.BuildDropTableStatement(dropTable);

						break;
					}

				case SqlCreateTableStatement createTable:
					{
						if (commandNumber == 1)
						{
							StringBuilder
								.Append("CREATE GENERATOR GIDENTITY_")
								.Append(createTable.Table.PhysicalName)
								.AppendLine();
						}
						else
						{
							StringBuilder
								.AppendFormat("CREATE TRIGGER TIDENTITY_{0} FOR {0}", createTable.Table.PhysicalName)
								.AppendLine  ()
								.AppendLine  ("BEFORE INSERT POSITION 0")
								.AppendLine  ("AS BEGIN")
								.AppendFormat("\tNEW.{0} = GEN_ID(GIDENTITY_{1}, 1);", _identityField.PhysicalName, createTable.Table.PhysicalName)
								.AppendLine  ()
								.AppendLine  ("END");
						}

						break;
					}
			}
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string database, string schema, string table)
		{
			return sb.Append(table);
		}

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.FbDbType.ToString();
		}
	}
}
