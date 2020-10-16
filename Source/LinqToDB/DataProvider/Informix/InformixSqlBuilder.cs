using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using SqlQuery;
	using SqlProvider;
	using System.Globalization;
	using Mapping;

	partial class InformixSqlBuilder : BasicSqlBuilder
	{
		private readonly InformixDataProvider? _provider;

		public InformixSqlBuilder(
			InformixDataProvider? provider,
			MappingSchema         mappingSchema,
			ISqlOptimizer         sqlOptimizer,
			SqlProviderFlags      sqlProviderFlags)
			: this(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		// remote context
		public InformixSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
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
				ConvertTableName(StringBuilder, trun.Table.Server, trun.Table.Database, trun.Table.Schema, trun.Table.PhysicalName!);
				StringBuilder.Append(" MODIFY ");
				Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				StringBuilder.AppendLine(" SERIAL(1)");
			}
			else
			{
				StringBuilder.AppendLine("SELECT DBINFO('sqlca.sqlerrd1') FROM systables where tabid = 1");
			}
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			StringBuilder.Append("TRUNCATE TABLE ");
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new InformixSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, EvaluationContext context, int indent, bool skipAlias)
		{
			base.BuildSql(commandNumber, statement, sb, context, indent, skipAlias);

			sb
				.Replace("NULL IS NOT NULL", "1=0")
				.Replace("NULL IS NULL",     "1=1");
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent().Append("SELECT FIRST 1").AppendLine();
				BuildColumns(selectQuery);
				AppendIndent().Append("FROM SYSTABLES").AppendLine();
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

		protected override string FirstFormat(SelectQuery selectQuery) => "FIRST {0}";
		protected override string SkipFormat  => "SKIP {0}";

		protected override void BuildLikePredicate(SqlPredicate.Like predicate)
		{
			if (predicate.IsNot)
				StringBuilder.Append("NOT ");

			var precedence = GetPrecedence(predicate);

			BuildExpression(precedence, predicate.Expr1);
			StringBuilder.Append(" LIKE ");
			BuildExpression(precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				StringBuilder.Append(" ESCAPE ");
				BuildExpression(precedence, predicate.Escape);
			}
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.VarBinary  : StringBuilder.Append("BYTE");                      return;
				case DataType.Boolean    : StringBuilder.Append("BOOLEAN");                   return;
				case DataType.DateTime   : StringBuilder.Append("datetime year to second");   return;
				case DataType.DateTime2  : StringBuilder.Append("datetime year to fraction"); return;
				case DataType.Time       :
					StringBuilder.Append("INTERVAL HOUR TO FRACTION");
					StringBuilder.AppendFormat("({0})", (type.Type.Length ?? 5).ToString(CultureInfo.InvariantCulture));
					return;
				case DataType.Date       : StringBuilder.Append("DATETIME YEAR TO DAY");      return;
				case DataType.SByte      :
				case DataType.Byte       : StringBuilder.Append("SmallInt");                  return;
				case DataType.SmallMoney : StringBuilder.Append("Decimal(10,4)");             return;
				case DataType.Decimal    :
					StringBuilder.Append("Decimal");
					if (type.Type.Precision != null && type.Type.Scale != null)
						StringBuilder.AppendFormat(
							"({0}, {1})",
							type.Type.Precision.Value.ToString(CultureInfo.InvariantCulture),
							type.Type.Scale.Value.ToString(CultureInfo.InvariantCulture));
					return;
				case DataType.NVarChar:
					if (type.Type.Length == null || type.Type.Length > 255 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(255)");
						return;
					}

					break;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable);
		}

		/// <summary>
		/// Check if identifier is valid without quotation. Expects non-zero length string as input.
		/// </summary>
		private bool IsValidIdentifier(string name)
		{
			// https://www.ibm.com/support/knowledgecenter/en/SSGU8G_12.1.0/com.ibm.sqls.doc/ids_sqs_1660.htm
			// TODO: add informix-specific reserved words list
			// TODO: Letter definitions is: In the default locale, must be an ASCII character in the range A to Z or a to z
			// add support for other locales later
			return !IsReserved(name) &&
				((name[0] >= 'a' && name[0] <= 'z') || (name[0] >= 'A' && name[0] <= 'Z') || name[0] == '_') &&
				name.All(c =>
					(c >= 'a' && c <= 'z') ||
					(c >= 'A' && c <= 'Z') ||
					(c >= '0' && c <= '9') ||
					c == '$' ||
					c == '_');
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryTable:
					if (value.Length > 0 && !IsValidIdentifier(value))
						// I wonder what to do if identifier has " in name?
						return sb.Append('"').Append(value).Append('"');

					break;
				case ConvertType.NameToQueryParameter   :
					return SqlProviderFlags.IsParameterOrderDependent
						? sb.Append('?')
						: sb.Append('@').Append(value);
				case ConvertType.NameToCommandParameter :
				case ConvertType.NameToSprocParameter   : return sb.Append(':').Append(value);
				case ConvertType.SprocParameterToName   :
					return (value.Length > 0 && value[0] == ':')
						? sb.Append(value.Substring(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				if (field.Type!.Value.DataType == DataType.Int32)
				{
					StringBuilder.Append("SERIAL");
					return;
				}

				if (field.Type!.Value.DataType == DataType.Int64)
				{
					StringBuilder.Append("SERIAL8");
					return;
				}
			}

			base.BuildCreateTableFieldType(field);
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("PRIMARY KEY (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		// https://www.ibm.com/support/knowledgecenter/en/SSGU8G_12.1.0/com.ibm.sqls.doc/ids_sqs_1652.htm
		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table)
		{
			if (server   != null && server  .Length == 0) server   = null;
			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			if (server != null && database == null)
				throw new LinqToDBException("You must specify database for linked server query");

			if (database != null)
				sb.Append(database);

			if (server != null)
				sb.Append("@").Append(server);

			if (database != null)
				sb.Append(":");

			if (schema != null)
				sb.Append(schema).Append(".");

			return sb.Append(table);
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
		{
			if (_provider != null)
			{
				var param = _provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					if (_provider.Adapter.GetIfxType != null)
						return _provider.Adapter.GetIfxType(param).ToString();
					else
						return _provider.Adapter.GetDB2Type!(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
		}

		protected override void BuildTypedExpression(SqlDataType dataType, ISqlExpression value)
		{
			BuildExpression(value);
			StringBuilder.Append("::");
			BuildDataType(dataType, false);
		}
	}
}
