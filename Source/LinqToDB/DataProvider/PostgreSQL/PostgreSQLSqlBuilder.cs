using System;
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

	public class PostgreSQLSqlBuilder : BasicSqlBuilder
	{
		private readonly PostgreSQLDataProvider _provider;
		public PostgreSQLSqlBuilder(PostgreSQLDataProvider provider, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: this(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
			_provider = provider;
		}

		// used by linq service
		public PostgreSQLSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override bool IsRecursiveCteKeywordRequired => true;

		public override int CommandCount(SqlStatement statement)
		{
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			var insertClause = Statement.GetInsertClause();
			if (insertClause != null)
			{
				var into = insertClause.Into;
				var attr = GetSequenceNameAttribute(into, false);
				var name =
					attr != null
						? attr.SequenceName
						: Convert(
							$"{into.PhysicalName}_{into.GetIdentityField().PhysicalName}_seq",
							ConvertType.NameToQueryField);

				name = Convert(name, ConvertType.NameToQueryTable);

				var database = GetTableDatabaseName(into);
				var schema   = GetTableSchemaName(into);

				AppendIndent()
					.Append("SELECT currval('");

				BuildTableName(StringBuilder, database, schema, name.ToString());

				StringBuilder.AppendLine("')");
			}
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSqlBuilder(_provider, SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ";
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
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
					if (type.Length > 0)
						StringBuilder.Append('(').Append(type.Length.Value.ToString(NumberFormatInfo.InvariantInfo)).Append(')');
					break;
				case DataType.Undefined      :
					if (type.Type == typeof(string))
						goto case DataType.NVarChar;
					break;
				case DataType.Json           : StringBuilder.Append("json");           break;
				case DataType.BinaryJson     : StringBuilder.Append("jsonb");          break;
				case DataType.Guid           : StringBuilder.Append("uuid");           break;
				case DataType.VarBinary      : StringBuilder.Append("bytea");          break;
				case DataType.BitArray       :
					if (type.Length == 1)
						StringBuilder.Append("bit");
					if (type.Length > 1)
						StringBuilder.Append("bit(").Append(type.Length.Value.ToString(NumberFormatInfo.InvariantInfo)).Append(')');
					else
						StringBuilder.Append("bit varying");
					break;
				case DataType.NChar          :
					StringBuilder.Append("character");
					if (type.Length > 1) // this is correct condition
						StringBuilder.Append('(').Append(type.Length.Value.ToString(NumberFormatInfo.InvariantInfo)).Append(')');
					break;
				case DataType.Udt            :
					if (type.Type != null)
					{
						var udtType = type.Type.ToNullableUnderlying();

						if      (udtType == _provider.NpgsqlPointType)    StringBuilder.Append("point");
						else if (udtType == _provider.NpgsqlLineType)     StringBuilder.Append("line");
						else if (udtType == _provider.NpgsqlBoxType)      StringBuilder.Append("box");
						else if (udtType == _provider.NpgsqlLSegType)     StringBuilder.Append("lseg");
						else if (udtType == _provider.NpgsqlCircleType)   StringBuilder.Append("circle");
						else if (udtType == _provider.NpgsqlPolygonType)  StringBuilder.Append("polygon");
						else if (udtType == _provider.NpgsqlPathType)     StringBuilder.Append("path");
						else if (udtType == _provider.NpgsqlIntervalType) StringBuilder.Append("interval");
						else if (udtType == _provider.NpgsqlDateType)     StringBuilder.Append("date");
						else if (udtType == _provider.NpgsqlDateTimeType) StringBuilder.Append("timestamp");
						else if (udtType == typeof(IPAddress))            StringBuilder.Append("inet");
						else if (udtType == typeof(PhysicalAddress)
							&& !_provider.HasMacAddr8)                    StringBuilder.Append("macaddr");
						else                                              base.BuildDataType(type, createDbType);
					}
					else
						base.BuildDataType(type, createDbType);

					break;

				default                      : base.BuildDataType(type, createDbType); break;
			}
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (!statement.IsUpdate())
				base.BuildFromClause(statement, selectQuery);
		}

		protected sealed override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.PostgreSQL);
		}

		public static PostgreSQLIdentifierQuoteMode IdentifierQuoteMode = PostgreSQLIdentifierQuoteMode.Auto;

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToDatabase:
				case ConvertType.NameToSchema:
					if (value != null && IdentifierQuoteMode != PostgreSQLIdentifierQuoteMode.None)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '"')
							return name;

						if (IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Quote)
							return '"' + name + '"';

						if (IsReserved(name))
							return '"' + name + '"';

						if (name.Any(c => char.IsWhiteSpace(c) || IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Auto && char.IsUpper(c)))
							return '"' + name + '"';
					}

					break;

				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return ":" + value;

				case ConvertType.SprocParameterToName:
					if (value != null)
					{
						var str = value.ToString();
						return (str.Length > 0 && str[0] == ':')? str.Substring(1): str;
					}

					break;
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
					BuildExpression(expr.Expression, true, true);
				}

				Indent--;

				StringBuilder.AppendLine();
			}
			else
			{
				StringBuilder.AppendLine(") DO NOTHING");
			}
		}

		public override ISqlExpression GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
			{
				var attr = GetSequenceNameAttribute(table, false);

				if (attr != null)
				{
					var name     = Convert(attr.SequenceName, ConvertType.NameToQueryTable).ToString();
					var database = GetTableDatabaseName(table);
					var schema   = GetTableSchemaName  (table);

					var sb = BuildTableName(new StringBuilder(), database, schema, name);

					return new SqlExpression($"nextval('{sb}')", Precedence.Primary);
				}
			}

			return base.GetIdentityExpression(table);
		}

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				if (field.DataType == DataType.Int16)
				{
					StringBuilder.Append("SMALLSERIAL");
					return;
				}

				if (field.DataType == DataType.Int32)
				{
					StringBuilder.Append("SERIAL");
					return;
				}

				if (field.DataType == DataType.Int64)
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

		public override StringBuilder BuildTableName(StringBuilder sb, string database, string schema, string table)
		{
			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			// "db..table" syntax not supported and postgresql doesn't support database name, if it is not current database
			// so we can clear database name to avoid error from server
			if (database != null && schema == null)
				database = null;

			return base.BuildTableName(sb, database, schema, table);
		}

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.NpgsqlDbType.ToString();
		}

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			var table = truncateTable.Table;

			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(table, null);

			if (truncateTable.Table.Fields.Values.Any(f => f.IsIdentity))
			{
				if (truncateTable.ResetIdentity)
					StringBuilder.Append(" RESTART IDENTITY");
				else
					StringBuilder.Append(" CONTINUE IDENTITY");
			}

			StringBuilder.AppendLine();
		}

		protected override void BuildReturningSubclause(SqlStatement statement)
		{
			var output = statement.GetOutputClause();
			if (output != null)
			{
				StringBuilder
					.AppendLine("RETURNING");

				++Indent;

				bool first = true;
				foreach (var oi in output.OutputItems)
				{
					if (!first)
						StringBuilder.Append(',').AppendLine();
					first = false;

					AppendIndent();

					BuildExpression(oi.Expression);
				}

				StringBuilder
					.AppendLine();

				--Indent;
			}
		}
	}
}
