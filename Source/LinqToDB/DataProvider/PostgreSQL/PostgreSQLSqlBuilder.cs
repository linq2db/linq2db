using System.Data;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using SqlQuery;
	using SqlProvider;
	using System.Globalization;
	using LinqToDB.Extensions;
	using LinqToDB.Mapping;

	public class PostgreSQLSqlBuilder : BasicSqlBuilder
	{
		private readonly PostgreSQLDataProvider? _provider;

		public PostgreSQLSqlBuilder(
			PostgreSQLDataProvider? provider,
			MappingSchema           mappingSchema,
			ISqlOptimizer           sqlOptimizer,
			SqlProviderFlags        sqlProviderFlags)
			: this(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		// remote context
		public PostgreSQLSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override bool IsRecursiveCteKeywordRequired => true;

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", insertClause.Into.Name);

			AppendIndent().AppendLine("RETURNING ");
			AppendIndent().Append("\t");
			BuildExpression(identityField, false, true);
			StringBuilder.AppendLine();
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ";
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.SByte         :
				case DataType.Byte          : StringBuilder.Append("SmallInt");       break;
				case DataType.Money         : StringBuilder.Append("money");          break;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10,4)");  break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
				case DataType.DateTime      : StringBuilder.Append("TimeStamp");      break;
				case DataType.DateTimeOffset: StringBuilder.Append("TimeStampTZ");    break;
				case DataType.Boolean       : StringBuilder.Append("Boolean");        break;
				case DataType.NVarChar      :
					StringBuilder.Append("VarChar");
					if (type.Type.Length > 0)
						StringBuilder.Append('(').Append(type.Type.Length.Value.ToString(NumberFormatInfo.InvariantInfo)).Append(')');
					break;
				case DataType.Json           : StringBuilder.Append("json");           break;
				case DataType.BinaryJson     : StringBuilder.Append("jsonb");          break;
				case DataType.Guid           : StringBuilder.Append("uuid");           break;
				case DataType.VarBinary      : StringBuilder.Append("bytea");          break;
				case DataType.BitArray       :
					if (type.Type.Length == 1)
						StringBuilder.Append("bit");
					if (type.Type.Length > 1)
						StringBuilder.Append("bit(").Append(type.Type.Length.Value.ToString(NumberFormatInfo.InvariantInfo)).Append(')');
					else
						StringBuilder.Append("bit varying");
					break;
				case DataType.NChar          :
					StringBuilder.Append("character");
					if (type.Type.Length > 1) // this is correct condition
						StringBuilder.Append('(').Append(type.Type.Length.Value.ToString(NumberFormatInfo.InvariantInfo)).Append(')');
					break;
					case DataType.Interval   : StringBuilder.Append("interval");       break;
				case DataType.Udt            :
					var udtType = type.Type.SystemType.ToNullableUnderlying();

					     if (_provider != null && udtType == _provider.Adapter.NpgsqlPointType   ) StringBuilder.Append("point");
					else if (_provider != null && udtType == _provider.Adapter.NpgsqlLineType    ) StringBuilder.Append("line");
					else if (_provider != null && udtType == _provider.Adapter.NpgsqlBoxType     ) StringBuilder.Append("box");
					else if (_provider != null && udtType == _provider.Adapter.NpgsqlLSegType    ) StringBuilder.Append("lseg");
					else if (_provider != null && udtType == _provider.Adapter.NpgsqlCircleType  ) StringBuilder.Append("circle");
					else if (_provider != null && udtType == _provider.Adapter.NpgsqlPolygonType ) StringBuilder.Append("polygon");
					else if (_provider != null && udtType == _provider.Adapter.NpgsqlPathType    ) StringBuilder.Append("path");
					else if (_provider != null && udtType == _provider.Adapter.NpgsqlDateType    ) StringBuilder.Append("date");
					else if (_provider != null && udtType == _provider.Adapter.NpgsqlDateTimeType) StringBuilder.Append("timestamp");
					else if (udtType == typeof(PhysicalAddress) && _provider != null && !_provider.HasMacAddr8) StringBuilder.Append("macaddr");
					else if (udtType == typeof(IPAddress)) StringBuilder.Append("inet");
					else base.BuildDataTypeFromDataType(type, forCreateTable);

					break;

				default                      : base.BuildDataTypeFromDataType(type, forCreateTable); break;
			}
		}

		protected sealed override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.PostgreSQL);
		}

		public static PostgreSQLIdentifierQuoteMode IdentifierQuoteMode = PostgreSQLIdentifierQuoteMode.Auto;

		public override string Convert(string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToDatabase:
				case ConvertType.NameToSchema:
					if (IdentifierQuoteMode != PostgreSQLIdentifierQuoteMode.None)
					{
						if (value.Length > 0 && value[0] == '"')
							return value;

						if (IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Quote)
							return '"' + value + '"';

						if (IsReserved(value))
							return '"' + value + '"';

						if (value.Any(c => char.IsWhiteSpace(c) || IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Auto && char.IsUpper(c)))
							return '"' + value + '"';
					}

					break;

				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return ":" + value;

				case ConvertType.SprocParameterToName:
					return (value.Length > 0 && value[0] == ':')? value.Substring(1): value;
			}

			return value;
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertQuery(insertOrUpdate, insertOrUpdate.Insert, true);

			AppendIndent();
			StringBuilder.Append("ON CONFLICT (");

			var firstKey = true;
			foreach (var expr in insertOrUpdate.Update.Keys)
			{
				if (!firstKey)
					StringBuilder.Append(',');
				firstKey = false;

				BuildExpression(expr.Column, false, true);
			}

			if (insertOrUpdate.Update.Items.Count > 0)
			{
				StringBuilder.AppendLine(") DO UPDATE SET");

				Indent++;

				var first = true;

				foreach (var expr in insertOrUpdate.Update.Items)
				{
					if (!first)
						StringBuilder.Append(',').AppendLine();
					first = false;

					AppendIndent();
					BuildExpression(expr.Column, false, true);
					StringBuilder.Append(" = ");
					BuildExpression(expr.Expression!, true, true);
				}

				Indent--;

				StringBuilder.AppendLine();
			}
			else
			{
				StringBuilder.AppendLine(") DO NOTHING");
			}
		}

		public override ISqlExpression? GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
			{
				var attr = GetSequenceNameAttribute(table, false);

				if (attr != null)
				{
					var name     = Convert(attr.SequenceName, ConvertType.NameToQueryTable);
					var server   = GetTableServerName(table);
					var database = GetTableDatabaseName(table);
					var schema   = attr.Schema != null
						? Convert(attr.Schema, ConvertType.NameToSchema)
						: GetTableSchemaName(table);

					var sb = new StringBuilder();
					sb.Append("nextval(");
					ValueToSqlConverter.Convert(sb, BuildTableName(new StringBuilder(), server, database, schema, name).ToString());
					sb.Append(")");
					return new SqlExpression(sb.ToString(), Precedence.Primary);
				}
			}

			return base.GetIdentityExpression(table);
		}

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				if (field.Type!.Value.DataType == DataType.Int16)
				{
					StringBuilder.Append("SMALLSERIAL");
					return;
				}

				if (field.Type!.Value.DataType == DataType.Int32)
				{
					StringBuilder.Append("SERIAL");
					return;
				}

				if (field.Type!.Value.DataType == DataType.Int64)
				{
					StringBuilder.Append("BIGSERIAL");
					return;
				}
			}

			base.BuildCreateTableFieldType(field);
		}

		protected override bool BuildJoinType(SqlJoinedTable join)
		{
			switch (join.JoinType)
			{
				case JoinType.CrossApply : StringBuilder.Append("INNER JOIN LATERAL "); return true;
				case JoinType.OuterApply : StringBuilder.Append("LEFT JOIN LATERAL ");  return true;
			}

			return base.BuildJoinType(join);
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table)
		{
			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			// "db..table" syntax not supported and postgresql doesn't support database name, if it is not current database
			// so we can clear database name to avoid error from server
			if (database != null && schema == null)
				database = null;

			return base.BuildTableName(sb, null, database, schema, table);
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
		{
			if (_provider != null)
			{
				var param = _provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return _provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
		}

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			var table = truncateTable.Table;

			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(table!, null);

			if (truncateTable.Table!.Fields.Values.Any(f => f.IsIdentity))
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

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}
	}
}
