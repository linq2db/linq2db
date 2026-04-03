using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DuckDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBSqlBuilder : BasicSqlBuilder<DuckDBOptions>
	{
		public DuckDBSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		DuckDBSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder() => new DuckDBSqlBuilder(this);

		protected override bool IsRecursiveCteKeywordRequired => true;

		protected override string LimitFormat (SelectQuery selectQuery) => "LIMIT {0}";
		protected override string OffsetFormat(SelectQuery selectQuery) => "OFFSET {0} ";

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField()
				?? throw new LinqToDBException($"Identity field must be defined for '{insertClause.Into.NameForLogging}'.");

			AppendIndent().AppendLine("RETURNING ");
			AppendIndent().Append('\t');
			BuildExpression(identityField, false, true);
			StringBuilder.AppendLine();
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Decimal       :
					StringBuilder.Append("DECIMAL");
					if (type.Precision > 0)
					{
						StringBuilder
							.Append('(')
							.Append(type.Precision.Value.ToString(NumberFormatInfo.InvariantInfo));
						if (type.Scale > 0)
							StringBuilder
								.Append(", ")
								.Append(type.Scale.Value.ToString(NumberFormatInfo.InvariantInfo));
						StringBuilder
							.Append(')');
					}

					break;
				case DataType.SByte         : StringBuilder.Append("TINYINT");        break;
				case DataType.Byte          : StringBuilder.Append("UTINYINT");       break;
				case DataType.Int16         : StringBuilder.Append("SMALLINT");       break;
				case DataType.UInt16        : StringBuilder.Append("USMALLINT");      break;
				case DataType.Int32         : StringBuilder.Append("INTEGER");        break;
				case DataType.UInt32        : StringBuilder.Append("UINTEGER");       break;
				case DataType.Int64         : StringBuilder.Append("BIGINT");         break;
				case DataType.UInt64        : StringBuilder.Append("UBIGINT");        break;
				case DataType.Single        : StringBuilder.Append("FLOAT");          break;
				case DataType.Double        : StringBuilder.Append("DOUBLE");         break;
				case DataType.Money
					or DataType.SmallMoney  : StringBuilder.Append("DECIMAL(19, 4)"); break;
				case DataType.DateTime2
					or DataType.SmallDateTime
					or DataType.DateTime    : StringBuilder.Append("TIMESTAMP");      break;
				case DataType.DateTimeOffset: StringBuilder.Append("TIMESTAMPTZ");    break;
				case DataType.Date          : StringBuilder.Append("DATE");           break;
				case DataType.Time          : StringBuilder.Append("TIME");           break;
				case DataType.Boolean       : StringBuilder.Append("BOOLEAN");        break;
				case DataType.Text
					or DataType.NText       : StringBuilder.Append("VARCHAR");        break;
				case DataType.NVarChar
					or DataType.VarChar     :
					StringBuilder.Append("VARCHAR");
					if (type.Length > 0)
						StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Length})");
					break;
				case DataType.NChar
					or DataType.Char        :
					StringBuilder.Append("VARCHAR");
					if (type.Length > 0)
						StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Length})");
					break;
				case DataType.Json          : StringBuilder.Append("JSON");           break;
				case DataType.BinaryJson    : StringBuilder.Append("JSON");           break;
				case DataType.Guid          : StringBuilder.Append("UUID");           break;
				case DataType.Binary
					or DataType.VarBinary
					or DataType.Blob        : StringBuilder.Append("BLOB");           break;
				case DataType.Interval      : StringBuilder.Append("INTERVAL");       break;
				case DataType.VarNumeric    : StringBuilder.Append("HUGEINT");        break;
				default                     : base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull); break;
			}
		}

		protected sealed override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.DuckDB);
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable     :
				case ConvertType.NameToCteName        :
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToDatabase       :
				case ConvertType.NameToSchema         :
				case ConvertType.SequenceName         :
				{
					var quote =
						   IsReserved(value)
						|| (value.Length > 0 && value[0] != '_' && !char.IsLetter(value[0]))
						|| value.Skip(1).Any(c => !char.IsLetter(c) && !c.IsAsciiDigit() && c is not '_');

					if (quote)
						return sb.Append('"').Append(value.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');

					break;
				}

				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append('$').Append(value);

				case ConvertType.NameToCommandParameter:
					return sb.Append(value);

				case ConvertType.SprocParameterToName:
					return (value.Length > 0 && value[0] == '$')
						? sb.Append(value.AsSpan(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsOnConflictUpdateOrNothing(insertOrUpdate);
		}

		public override ISqlExpression? GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
			{
				var attr = GetSequenceNameAttribute(table, false);

				if (attr != null)
				{
					var sequenceName = new SqlObjectName(attr.SequenceName, Server: table.TableName.Server, Database: table.TableName.Database, Schema: attr.Schema ?? table.TableName.Schema);

					using var sb = Pools.StringBuilder.Allocate();
					sb.Value.Append("nextval(");
					MappingSchema.ConvertToSqlValue(sb.Value, null, DataOptions, BuildObjectName(new (), sequenceName, ConvertType.SequenceName, true, TableOptions.NotSet).ToString());
					sb.Value.Append(')');

					return new SqlFragment(sb.Value.ToString());
				}
			}

			return base.GetIdentityExpression(table);
		}

		protected override void BuildCreateTableStatement(SqlCreateTableStatement createTable)
		{
			// DuckDB doesn't support GENERATED AS IDENTITY or SERIAL.
			// For identity fields, create a sequence first, then use DEFAULT nextval().
			var table = createTable.Table;
			foreach (var field in table.Fields)
			{
				if (field.IsIdentity)
				{
					var seqName = $"{table.TableName.Name}_{field.PhysicalName}_seq";
					StringBuilder.Append("CREATE SEQUENCE IF NOT EXISTS ");
					Convert(StringBuilder, seqName, ConvertType.SequenceName);
					StringBuilder.AppendLine(" START 1;");
				}
			}

			base.BuildCreateTableStatement(createTable);
		}

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				var tableName = ((SqlCreateTableStatement)Statement).Table.TableName.Name;
				var typeName = field.Type.DataType switch
				{
					DataType.Int16 => "SMALLINT",
					DataType.Int32 => "INTEGER",
					DataType.Int64 => "BIGINT",
					_              => throw new InvalidOperationException($"Unsupported identity field type {field.Type.DataType}"),
				};

				var seqName = $"{tableName}_{field.PhysicalName}_seq";
				StringBuilder.Append(typeName);
				StringBuilder.Append(" DEFAULT NEXTVAL('");
				StringBuilder.Append(seqName);
				StringBuilder.Append("')");

				return;
			}

			base.BuildCreateTableFieldType(field);
		}

		protected override bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
		{
			switch (join.JoinType)
			{
				case JoinType.CrossApply : StringBuilder.Append("INNER JOIN LATERAL "); return true;
				case JoinType.OuterApply : StringBuilder.Append("LEFT JOIN LATERAL ");  return true;
			}

			return base.BuildJoinType(join, condition);
		}

		public override StringBuilder BuildObjectName(
			StringBuilder sb,
			SqlObjectName name,
			ConvertType objectType = ConvertType.NameToQueryTable,
			bool escape = true,
			TableOptions tableOptions = TableOptions.NotSet,
			bool withoutSuffix = false
		)
		{
			var schemaName = tableOptions.HasIsTemporary() ? null : name.Schema;

			if (schemaName != null)
			{
				(escape ? Convert(sb, schemaName, ConvertType.NameToSchema) : sb.Append(schemaName)).Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
			=> BuildDropTableStatementIfExists(dropTable);

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (!field.CanBeNull && !field.IsPrimaryKey)
				StringBuilder.Append("NOT NULL");
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			var command = table.TableOptions.TemporaryOptionValue switch
			{
				TableOptions.IsTemporary                                                                              or
				TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData or
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     or
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData or
				                                                                    TableOptions.IsLocalTemporaryData or
				                           TableOptions.IsLocalTemporaryStructure                                     or
				                           TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData =>
					"CREATE TEMPORARY TABLE ",

				0 =>
					"CREATE TABLE ",

				var value =>
					throw new LinqToDBException($"Incompatible table options '{value}'"),
			};

			StringBuilder.Append(command);
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent()
				.Append("PRIMARY KEY (")
				.AppendJoinStrings(InlineComma, fieldNames)
				.Append(')');
		}

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			var table = truncateTable.Table;

			BuildTag(truncateTable);
			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(table!, null);
			StringBuilder.AppendLine();
		}
		protected override void BuildParameter(SqlParameter parameter)
		{
			// DuckDB.NET sends TimeSpan as string; wrap INTERVAL parameters in CAST
			if (parameter.Type.DataType == DataType.Interval || parameter.Type.SystemType == typeof(TimeSpan))
			{
				StringBuilder.Append("CAST(");
				base.BuildParameter(parameter);
				StringBuilder.Append(" AS INTERVAL)");
				return;
			}

			base.BuildParameter(parameter);
		}
	}
}
