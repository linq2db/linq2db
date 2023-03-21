using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace LinqToDB.DataProvider.Informix
{
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	sealed partial class InformixSqlBuilder : BasicSqlBuilder
	{
		public InformixSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		InformixSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new InformixSqlBuilder(this);
		}

		protected override bool SupportsNullInColumn => false;

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
				BuildObjectName(StringBuilder, trun.Table.TableName, ConvertType.NameToQueryTable, true, trun.Table.TableOptions);
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

		protected override void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, OptimizationContext optimizationContext, int indent, bool skipAlias)
		{
			base.BuildSql(commandNumber, statement, sb, optimizationContext, indent, skipAlias);

			sb
				.Replace("NULL IS NOT NULL", "1=0")
				.Replace("NULL IS NULL",     "1=1");
		}

		protected override void BuildSelectClause(NullabilityContext nullability, SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent().Append("SELECT").AppendLine();
				BuildColumns(nullability, selectQuery);
				AppendIndent().Append("FROM ").Append(FakeTable).AppendLine();
			}
			else if (selectQuery.Select.IsDistinct)
			{
				AppendIndent();
				StringBuilder.Append("SELECT");
				BuildSkipFirst(nullability, selectQuery);
				StringBuilder.Append(" DISTINCT");
				StringBuilder.AppendLine();
				BuildColumns(nullability, selectQuery);
			}
			else
				base.BuildSelectClause(nullability, selectQuery);
		}

		protected override string FirstFormat(SelectQuery selectQuery) => "FIRST {0}";
		protected override string SkipFormat  => "SKIP {0}";

		protected override void BuildIsDistinctPredicate(NullabilityContext nullability, SqlPredicate.IsDistinct expr) => BuildIsDistinctPredicateFallback(nullability, expr);

		protected override void BuildLikePredicate(NullabilityContext nullability, SqlPredicate.Like predicate)
		{
			if (predicate.IsNot)
				StringBuilder.Append("NOT ");

			var precedence = GetPrecedence(predicate);

			BuildExpression(nullability, precedence, predicate.Expr1);
			StringBuilder.Append(" LIKE ");
			BuildExpression(nullability, precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				StringBuilder.Append(" ESCAPE ");
				BuildExpression(nullability, precedence, predicate.Escape);
			}
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.Type.DataType)
			{
				case DataType.Guid       : StringBuilder.Append("VARCHAR(36)");               return;
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
				case DataType.SmallMoney : StringBuilder.Append("Decimal(10, 4)");            return;
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

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
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
				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryTable     :
				case ConvertType.NameToProcedure      :
				case ConvertType.NameToServer         :
				case ConvertType.NameToDatabase       :
				case ConvertType.NameToSchema         :
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
				if (field.Type.DataType == DataType.Int32)
				{
					StringBuilder.Append("SERIAL");
					return;
				}

				if (field.Type.DataType == DataType.Int64)
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
			StringBuilder.Append(string.Join(InlineComma, fieldNames));
			StringBuilder.Append(')');
		}

		// https://www.ibm.com/support/knowledgecenter/en/SSGU8G_12.1.0/com.ibm.sqls.doc/ids_sqs_1652.htm
		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions)
		{
			if (name.Server != null && name.Database == null)
				throw new LinqToDBException("You must specify database for linked server query");

			if (name.Database != null)
			{
				if (escape)
					Convert(sb, name.Database, ConvertType.NameToDatabase);
				else
					sb.Append(name.Database);
			}

			if (name.Server != null)
			{
				sb.Append('@');
				if (escape)
					Convert(sb, name.Server, ConvertType.NameToServer);
				else
					sb.Append(name.Server);
			}

			if (name.Database != null)
				sb.Append(':');

			if (name.Schema != null)
			{
				(escape ? Convert(sb, name.Schema, ConvertType.NameToSchema) : sb.Append(name.Schema))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is InformixDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					if (provider.Adapter.GetIfxType != null)
						return provider.Adapter.GetIfxType(param).ToString();
					else
						return provider.Adapter.GetDB2Type!(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildTypedExpression(NullabilityContext nullability, SqlDataType dataType, ISqlExpression value)
		{
			BuildExpression(nullability, value);
			StringBuilder.Append("::");
			BuildDataType(dataType, false, value.CanBeNullable(nullability));
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
						command = "CREATE TEMP TABLE ";
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

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		protected override void BuildSqlRow(NullabilityContext nullability, SqlRow expr, bool buildTableName, bool checkParentheses, bool throwExceptionIfTableNotFound)
		{
			// Informix needs ROW(1,2) syntax instead of BasicSqlBuilder default (1,2)
			StringBuilder.Append("ROW (");
			foreach (var value in expr.Values)
			{
				BuildExpression(nullability, value, buildTableName, checkParentheses, throwExceptionIfTableNotFound);
				StringBuilder.Append(InlineComma);
			}
			StringBuilder.Length -= InlineComma.Length; // Note that SqlRow are never empty
			StringBuilder.Append(')');
		}

		protected override ISqlExpression WrapBooleanExpression(ISqlExpression expr)
		{
			var newExpr = base.WrapBooleanExpression(expr);
			if (!ReferenceEquals(newExpr, expr))
			{
				return new SqlFunction(typeof(bool), "Convert", false, new SqlDataType(DataType.Boolean),
					newExpr);
			}

			return newExpr;
		}

		protected override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.Informix);
		}
	}
}
