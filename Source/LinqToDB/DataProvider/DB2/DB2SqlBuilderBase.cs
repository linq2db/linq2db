using System;
using System.Data;
using System.Data.SqlTypes;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace LinqToDB.DataProvider.DB2
{
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	abstract partial class DB2SqlBuilderBase : BasicSqlBuilder<DB2Options>
	{
		protected DB2SqlBuilderBase(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected DB2SqlBuilderBase(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		SqlField? _identityField;

		protected abstract DB2Version Version { get; }

		protected override bool SupportsNullInColumn => false;

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity ? 1 + trun.Table!.IdentityFields.Count : 1;

			if (Version == DB2Version.LUW && statement is SqlInsertStatement insertStatement && insertStatement.Insert.WithIdentity)
			{
				_identityField = insertStatement.Insert.Into!.GetIdentityField();

				if (_identityField == null)
					return 2;
			}

			return 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				var field = trun.Table!.IdentityFields[commandNumber - 1];

				StringBuilder.Append("ALTER TABLE ");
				BuildObjectName(StringBuilder, trun.Table.TableName, ConvertType.NameToQueryTable, true, trun.Table.TableOptions);
				StringBuilder.Append(" ALTER ");
				Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				StringBuilder.AppendLine(" RESTART WITH 1");
			}
			else
			{
				StringBuilder.AppendLine("SELECT identity_val_local() FROM SYSIBM.SYSDUMMY1");
			}
		}

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			var nullability = NullabilityContext.NonQuery;

			var table = truncateTable.Table!;

			BuildTag(truncateTable);
			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(nullability, table, null);
			StringBuilder.Append(" IMMEDIATE");
			StringBuilder.AppendLine();
		}

		protected override void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, OptimizationContext optimizationContext, int indent, bool skipAlias)
		{
			var nullability = statement.SelectQuery == null
				? NullabilityContext.NonQuery
				: new (statement.SelectQuery);

			Statement           = statement;
			StringBuilder       = sb;
			OptimizationContext = optimizationContext;
			Indent              = indent;
			SkipAlias           = skipAlias;

			if (_identityField != null)
			{
				indent += 2;

				AppendIndent().AppendLine("SELECT");
				AppendIndent().Append('\t');
				BuildExpression(nullability, _identityField, false, true);
				sb.AppendLine();
				AppendIndent().AppendLine("FROM");
				AppendIndent().AppendLine("\tNEW TABLE");
				AppendIndent().Append('\t').AppendLine(OpenParens);
			}

			base.BuildSql(commandNumber, statement, sb, optimizationContext, indent, skipAlias);

			if (_identityField != null)
				sb.AppendLine("\t)");
		}

		protected override void BuildGetIdentity(NullabilityContext nullability, SqlInsertClause insertClause)
		{
			if (Version == DB2Version.zOS)
			{
				StringBuilder
					.AppendLine(";")
					.AppendLine("SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1");
			}
		}

		protected override void BuildSelectClause(NullabilityContext nullability, SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent().AppendLine("SELECT");
				BuildColumns(nullability, selectQuery);
				AppendIndent().AppendLine("FROM SYSIBM.SYSDUMMY1");
			}
			else
				base.BuildSelectClause(nullability, selectQuery);
		}

		protected override string? LimitFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue == null ? "FETCH FIRST {0} ROWS ONLY" : null;
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.Type.DataType)
			{
				case DataType.DateTime  :
				case DataType.DateTime2 :
					StringBuilder.Append("timestamp");
					if (type.Type.Precision != null && type.Type.Precision != 6)
						StringBuilder.Append($"({type.Type.Precision})");
					return;
				case DataType.Boolean   : StringBuilder.Append("smallint");              return;
				case DataType.Guid      : StringBuilder.Append("char(16) for bit data"); return;
				case DataType.NVarChar  :
					if (type.Type.Length == null || type.Type.Length > 8168 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(8168)");
						return;
					}

					break;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (field.CanBeNull && field.Type.DataType == DataType.Guid)
				return;

			base.BuildCreateTableNullAttribute(field, defaultNullable);
		}

		[Obsolete("Use DB2Options.Default.IdentifierQuoteMode instead.")]
		public static DB2IdentifierQuoteMode IdentifierQuoteMode
		{
			get => DB2Options.Default.IdentifierQuoteMode;
			set => DB2Options.Default = DB2Options.Default with { IdentifierQuoteMode = value };
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return sb.Append('@').Append(value);

				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append(':').Append(value);

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == ':'
						? sb.Append(value.Substring(1))
						: sb.Append(value);

				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable     :
				case ConvertType.NameToProcedure      :
				case ConvertType.NameToPackage        :
				case ConvertType.NameToSchema         :
				case ConvertType.NameToDatabase       :
				case ConvertType.NameToQueryTableAlias:
					if (ProviderOptions.IdentifierQuoteMode != DB2IdentifierQuoteMode.None)
					{
						if (value.Length > 0 && value[0] == '"')
							return sb.Append(value);

						if (ProviderOptions.IdentifierQuoteMode == DB2IdentifierQuoteMode.Quote ||
						    value.StartsWith("_") ||
						    value.Any(c => char.IsLower(c) || char.IsWhiteSpace(c)))
							return sb.Append('"').Append(value).Append('"');
					}

					break;
			}

			return sb.Append(value);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, "FROM SYSIBM.SYSDUMMY1 FETCH FIRST 1 ROW ONLY");
		}

		protected override void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.Append("VALUES ");

			foreach (var _ in insertClause.Into!.Fields)
				StringBuilder.Append("(DEFAULT)");

			StringBuilder.AppendLine();
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("GENERATED ALWAYS AS IDENTITY");
		}

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions)
		{
			var schemaName = name.Schema;
			if (schemaName == null && tableOptions.IsTemporaryOptionSet())
				schemaName = "SESSION";

			// "db..table" syntax not supported
			if (name.Database != null && schemaName == null)
				throw new LinqToDBException("DB2 requires schema name if database name provided.");

			if (name.Database != null)
			{
				(escape ? Convert(sb, name.Database, ConvertType.NameToDatabase) : sb.Append(name.Database))
					.Append('.');
				if (schemaName == null)
					sb.Append('.');
			}

			if (schemaName != null)
			{
				(escape ? Convert(sb, schemaName, ConvertType.NameToSchema) : sb.Append(schemaName))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (parameter.DbType == DbType.Decimal && parameter.Value is decimal decValue)
			{
				var d = new SqlDecimal(decValue);
				return string.Format("({0}{1}{2})", d.Precision.ToString(CultureInfo.InvariantCulture), InlineComma, d.Scale.ToString(CultureInfo.InvariantCulture));
			}

			if (DataProvider is DB2DataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var nullability = NullabilityContext.NonQuery;
			var table       = dropTable.Table;

			BuildTag(dropTable);
			if (dropTable.Table.TableOptions.HasDropIfExists())
			{
				AppendIndent().Append(@"BEGIN
	DECLARE CONTINUE HANDLER FOR SQLSTATE '42704' BEGIN END;
	EXECUTE IMMEDIATE 'DROP TABLE ");
				BuildPhysicalTable(nullability, table, null);
				StringBuilder.AppendLine(
				@"';
END");
			}
			else
			{
				AppendIndent().Append("DROP TABLE ");
				BuildPhysicalTable(nullability, table, null);
				StringBuilder.AppendLine();
			}
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary                                                                               :
					case TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                      :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData :
					case                                                                      TableOptions.IsLocalTemporaryData :
					case                            TableOptions.IsLocalTemporaryStructure                                      :
					case                            TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData :
						command = "DECLARE GLOBAL TEMPORARY TABLE ";
						break;
					case                            TableOptions.IsGlobalTemporaryStructure                                     :
					case                            TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData :
						command = "CREATE GLOBAL TEMPORARY TABLE ";
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
		}

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null && createTable.Table.TableOptions.HasCreateIfNotExists())
			{
				AppendIndent().AppendLine(@"BEGIN");

				Indent++;

				AppendIndent().AppendLine(@"DECLARE CONTINUE HANDLER FOR SQLSTATE '42710' BEGIN END;");
				AppendIndent().AppendLine(@"EXECUTE IMMEDIATE '");

				Indent++;
			}

			base.BuildStartCreateTableStatement(createTable);
		}

		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			base.BuildEndCreateTableStatement(createTable);

			if (createTable.StatementHeader == null && createTable.Table.TableOptions.HasCreateIfNotExists())
			{
				Indent--;

				AppendIndent()
					.AppendLine("';");

				Indent--;

				StringBuilder
					.AppendLine("END");
			}
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			// DB2 doesn't support constraints on temp tables
			if (createTable.Table.TableOptions.IsTemporaryOptionSet())
			{
				var idx = StringBuilder.Length - 1;
				while (idx >= 0 && StringBuilder[idx] != ',')
					idx--;
				StringBuilder.Length = idx == -1 ? 0 : idx;
				return;
			}

			base.BuildCreateTablePrimaryKey(createTable, pkName, fieldNames);
		}
	}
}
