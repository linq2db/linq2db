using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.DataProvider.SQLite
{
	using SqlQuery;
	using SqlProvider;
	using Mapping;

	public class SQLiteSqlBuilder : BasicSqlBuilder
	{
		public SQLiteSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity && trun.Table!.IdentityFields.Count > 0 ? 2 : 1;
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				StringBuilder
					.Append("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='")
					.Append(trun.Table!.PhysicalName)
					.AppendLine("'")
					;
			}
			else
			{
				StringBuilder.AppendLine("SELECT last_insert_rowid()");
			}
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SQLiteSqlBuilder(MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0}";
		}

		public override bool IsNestedJoinSupported => false;

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

				case ConvertType.NameToDatabase:
				case ConvertType.NameToSchema:
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

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Int32 : StringBuilder.Append("INTEGER");                      break;
				default             : base.BuildDataTypeFromDataType(type, forCreateTable); break;
			}
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("PRIMARY KEY AUTOINCREMENT");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			if (createTable.Table!.IdentityFields.Count > 0)
			{
				while (StringBuilder[StringBuilder.Length - 1] != ',')
					StringBuilder.Length--;
				StringBuilder.Length--;
			}
			else
			{
				AppendIndent();
				StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
			StringBuilder.Append(string.Join(InlineComma, fieldNames));
				StringBuilder.Append(')');
			}
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions)
		{
			if (database != null && database.Length == 0) database = null;

			if (database != null)
				sb.Append(database).Append('.');

			return sb.Append(table);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary                                                                              :
					case TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
					case                                                                     TableOptions.IsLocalTemporaryData :
					case                            TableOptions.IsLocalTemporaryStructure                                     :
					case                            TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
						command = "CREATE TEMPORARY TABLE ";
						break;
					case var value :
						throw new InvalidOperationException($"Incompatible table options '{value}'");
				}
			}
			else
			{
				command = "CREATE TABLE ";
			}

			StringBuilder.Append(command);

			if (table.TableOptions.HasCreateIfNotExists())
				StringBuilder.Append("IF NOT EXISTS ");
		}
	}
}
