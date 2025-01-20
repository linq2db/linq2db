using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Common.Internal;
	using Extensions;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	public partial class PostgreSQLSqlBuilder : BasicSqlBuilder<PostgreSQLOptions>
	{
		public PostgreSQLSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected PostgreSQLSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSqlBuilder(this);
		}

		protected override bool IsRecursiveCteKeywordRequired => true;

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField == null)
				throw new LinqToDBException($"Identity field must be defined for '{insertClause.Into.NameForLogging}'.");

			AppendIndent().AppendLine("RETURNING ");
			AppendIndent().Append('\t');
			BuildExpression(identityField, false, true);
			StringBuilder.AppendLine();
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ";
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Decimal       :
					StringBuilder.Append("decimal");
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
				case DataType.SByte         :
				case DataType.Byte          : StringBuilder.Append("SmallInt");       break;
				case DataType.Money         : StringBuilder.Append("money");          break;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10, 4)"); break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
				case DataType.DateTime      : StringBuilder.Append("TimeStamp");      break;
				case DataType.DateTimeOffset: StringBuilder.Append("TimeStampTZ");    break;
				case DataType.Boolean       : StringBuilder.Append("Boolean");        break;
				case DataType.Text          : StringBuilder.Append("text");           break;
				case DataType.NVarChar      :
					StringBuilder.Append("VarChar");
					if (type.Length > 0)
						StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Length})");
					break;
				case DataType.Json           : StringBuilder.Append("json");           break;
				case DataType.BinaryJson     : StringBuilder.Append("jsonb");          break;
				case DataType.Guid           : StringBuilder.Append("uuid");           break;
				case DataType.Binary         :
				case DataType.VarBinary      : StringBuilder.Append("bytea");          break;
				case DataType.BitArray       :
					if (type.Length == 1)
						StringBuilder.Append("bit");
					if (type.Length > 1)
						StringBuilder.Append(CultureInfo.InvariantCulture, $"bit({type.Length})");
					else
						StringBuilder.Append("bit varying");
					break;
				case DataType.NChar          :
					StringBuilder.Append("character");
					if (type.Length > 1) // this is correct condition
						StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Length})");
					break;
					case DataType.Interval   : StringBuilder.Append("interval");       break;
				case DataType.Udt            :
					var udtType = type.SystemType.ToNullableUnderlying();

					var provider = DataProvider as PostgreSQLDataProvider;

					     if (udtType == provider?.Adapter.NpgsqlPointType   ) StringBuilder.Append("point");
					else if (udtType == provider?.Adapter.NpgsqlLineType    ) StringBuilder.Append("line");
					else if (udtType == provider?.Adapter.NpgsqlBoxType     ) StringBuilder.Append("box");
					else if (udtType == provider?.Adapter.NpgsqlLSegType    ) StringBuilder.Append("lseg");
					else if (udtType == provider?.Adapter.NpgsqlCircleType  ) StringBuilder.Append("circle");
					else if (udtType == provider?.Adapter.NpgsqlPolygonType ) StringBuilder.Append("polygon");
					else if (udtType == provider?.Adapter.NpgsqlPathType    ) StringBuilder.Append("path");
					else if (udtType == provider?.Adapter.NpgsqlDateType    ) StringBuilder.Append("date");
					else if (udtType == provider?.Adapter.NpgsqlDateTimeType) StringBuilder.Append("timestamp");
					else if (udtType == provider?.Adapter.NpgsqlIntervalType) StringBuilder.Append("interval");
					else if (udtType == provider?.Adapter.NpgsqlCidrType    ) StringBuilder.Append("cidr");
					else if (udtType == typeof(PhysicalAddress) && provider != null && !provider.HasMacAddr8) StringBuilder.Append("macaddr");
					else if (udtType == typeof(IPAddress)) StringBuilder.Append("inet");
					else base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);

					break;

				default                      : base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull); break;
			}
		}

		protected sealed override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.PostgreSQL);
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			// TODO: implement better quotation logic
			// E.g. we currently don't handle quotes inside identifier
			// https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS
			switch (convertType)
			{
				case ConvertType.NameToQueryField     :
					if (value == PseudoFunctions.MERGE_ACTION)
						return sb.Append("merge_action()");
					goto case ConvertType.NameToQueryFieldAlias;
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable     :
				case ConvertType.NameToProcedure      :
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToDatabase       :
				case ConvertType.NameToSchema         :
				case ConvertType.SequenceName         :
					if (ProviderOptions.IdentifierQuoteMode != PostgreSQLIdentifierQuoteMode.None)
					{
						// current logic limitations (hardly an issue as they represent quite exotic cases):
						// - surrogate pairs/runes not handled
						// - non-lowercase non-uppercase letters not handled
						var quote =
							// force quote enabled
							ProviderOptions.IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Quote
							// only for Auto mode - contains upper-case letter
							|| ProviderOptions.IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Auto && value.Any(char.IsUpper)
							// is a keyword
							|| IsReserved(value)
							// starts from non-letter/underscore
							|| (value.Length > 0 && value[0] != '_' && !char.IsLetter(value[0]))
							// contains non-letter/underscore/digit(0-9 only)/$
#if NET8_0_OR_GREATER
							|| value.Skip(1).Any(c => !char.IsLetter(c) && !char.IsAsciiDigit(c) && c is not '_' and not '$')
#else
							|| value.Skip(1).Any(c => !char.IsLetter(c) && c is (< '0' or > '9') and not '_' and not '$')
#endif
							;

						if (quote)
							// don't forget to duplicate quotes
							return sb.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
					}

					break;

				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append(':').Append(value);

				case ConvertType.SprocParameterToName:
					return (value.Length > 0 && value[0] == ':')
						? sb.Append(value.Substring(1))
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
					return new SqlExpression(sb.Value.ToString(), Precedence.Primary);
				}
			}

			return base.GetIdentityExpression(table);
		}

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				if (field.Type.DataType == DataType.Int16)
				{
					StringBuilder.Append("SMALLSERIAL");
					return;
				}

				if (field.Type.DataType == DataType.Int32)
				{
					StringBuilder.Append("SERIAL");
					return;
				}

				if (field.Type.DataType == DataType.Int64)
				{
					StringBuilder.Append("BIGSERIAL");
					return;
				}
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

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix = false)
		{
			var schemaName = tableOptions.HasIsTemporary() ? null : name.Schema;

			// "db..table" syntax not supported and postgresql doesn't support database name, if it is not current database
			// so we can ignore database name to avoid error from server
			if (name.Database != null && schemaName != null)
			{
				(escape ? Convert(sb, name.Database, ConvertType.NameToDatabase) : sb.Append(name.Database)).Append('.');
			}

			if (schemaName != null)
			{
				(escape ? Convert(sb, schemaName, ConvertType.NameToSchema) : sb.Append(schemaName)).Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is PostgreSQLDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
				{
					try
					{
						// fast Enum detection path
						if (param.DbType == DbType.Object && param.Value?.GetType().IsEnum == true)
							return "Enum";

						return provider.Adapter.GetDbType(param).ToString();
					}
					catch (NotSupportedException)
					{
						// Hadling Npgsql mapping exception
						// Exception is thrown when using PostgreSQL Enums
					}
				}
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			var nullability = NullabilityContext.NonQuery;
			var table       = truncateTable.Table;

			BuildTag(truncateTable);
			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(table!, null);

			if (truncateTable.Table!.IdentityFields.Count > 0)
			{
				if (truncateTable.ResetIdentity)
					StringBuilder.Append(" RESTART IDENTITY");
				else
					StringBuilder.Append(" CONTINUE IDENTITY");
			}

			StringBuilder.AppendLine();
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary                                                                                    :
					case TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData       :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                           :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData       :
					case                                                                     TableOptions.IsLocalTemporaryData       :
					case                                                                     TableOptions.IsTransactionTemporaryData :
					case                            TableOptions.IsLocalTemporaryStructure                                           :
					case                            TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData       :
					case                            TableOptions.IsLocalTemporaryStructure | TableOptions.IsTransactionTemporaryData :
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

		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			var table = createTable.Table;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				StringBuilder.AppendLine(table.TableOptions.HasIsTransactionTemporaryData()
					? "ON COMMIT DELETE ROWS"
					: "ON COMMIT PRESERVE ROWS");
			}

			base.BuildEndCreateTableStatement(createTable);
		}

		public override string GetReserveSequenceValuesSql(int count, string sequenceName)
		{
			return FormattableString.Invariant($"SELECT nextval('{ConvertInline(sequenceName, ConvertType.SequenceName)}') FROM generate_series(1, {count})");
		}

		protected override void BuildSubQueryExtensions(SqlStatement statement)
		{
			if (statement.SelectQuery?.SqlQueryExtensions is not null)
			{
				var len = StringBuilder.Length;

				AppendIndent();

				var prefix = Environment.NewLine;

				if (StringBuilder.Length > len)
				{
					var buffer = new char[StringBuilder.Length - len];

					StringBuilder.CopyTo(len, buffer, 0, StringBuilder.Length - len);

					prefix += new string(buffer);
				}

				BuildQueryExtensions(StringBuilder, statement.SelectQuery.SqlQueryExtensions, null, prefix, Environment.NewLine, Sql.QueryExtensionScope.SubQueryHint);
			}
		}

		protected override void BuildQueryExtensions(SqlStatement statement)
		{
			if (statement.SqlQueryExtensions is not null)
			{
				var len = StringBuilder.Length;

				AppendIndent();

				var prefix = Environment.NewLine;

				if (StringBuilder.Length > len)
				{
					var buffer = new char[StringBuilder.Length - len];

					StringBuilder.CopyTo(len, buffer, 0, StringBuilder.Length - len);

					prefix += new string(buffer);
				}

				BuildQueryExtensions(StringBuilder, statement.SqlQueryExtensions, null, prefix, Environment.NewLine, Sql.QueryExtensionScope.QueryHint);
			}
		}

		protected override void BuildTypedExpression(DbDataType dataType, ISqlExpression value)
		{
			var saveStep = BuildStep;
			BuildStep = Step.TypedExpression;

			BuildExpression(Precedence.Primary, value);
			StringBuilder.Append("::");
			BuildDataType(dataType, false, value.CanBeNullable(NullabilityContext));

			BuildStep = saveStep;
		}

		protected override void BuildSql()
		{
			BuildSqlForUnion();
		}
	}
}
