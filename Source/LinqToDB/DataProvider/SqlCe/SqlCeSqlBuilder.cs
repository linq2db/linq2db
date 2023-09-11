using System;
using System.Data.Common;
using System.Text;

namespace LinqToDB.DataProvider.SqlCe
{
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	sealed class SqlCeSqlBuilder : BasicSqlBuilder
	{
		public SqlCeSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		SqlCeSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlCeSqlBuilder(this);
		}

		protected override string? FirstFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue == null ? "TOP ({0})" : null;
		}

		protected override string? LimitFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null;
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ROWS";
		}

		protected override bool OffsetFirst                   => true;
		protected override bool IsValuesSyntaxSupported       => false;
		protected override bool SupportsColumnAliasesInSource => true;
		protected override bool RequiresConstantColumnAliases => true;

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
				StringBuilder.Append(" ALTER COLUMN ");
				Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				StringBuilder.AppendLine(" IDENTITY(1, 1)");
			}
			else
			{
				StringBuilder.AppendLine("SELECT @@IDENTITY");
			}
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable, bool canBeNull)
		{
			// https://learn.microsoft.com/en-us/previous-versions/sql/sql-server-2005/ms172424(v=sql.90)
			switch (type.Type.DataType)
			{
				case DataType.Guid          : StringBuilder.Append("UNIQUEIDENTIFIER");                                                                        return;
				case DataType.Char          : base.BuildDataTypeFromDataType(new SqlDataType(DataType.NChar,    type.Type.Length), forCreateTable, canBeNull); return;
				case DataType.VarChar       : base.BuildDataTypeFromDataType(new SqlDataType(DataType.NVarChar, type.Type.Length), forCreateTable, canBeNull); return;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10, 4)");                                                                          return;
				case DataType.DateTime2     :
				case DataType.Time          :
				case DataType.Date          :
				case DataType.SmallDateTime : StringBuilder.Append("DateTime");                                                                                return;
				case DataType.NVarChar:
					if (type.Type.Length == null || type.Type.Length > 4000 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(4000)");
						return;
					}

					break;

				case DataType.Binary:
					StringBuilder.Append("BINARY");
					if (type.Type.Length > 1 && type.Type.Length <= 8000)
						StringBuilder.AppendFormat("({0})", type.Type.Length);
					return;
				case DataType.VarBinary:
					StringBuilder.Append("VARBINARY");
					if (type.Type.Length > 1 && type.Type.Length <= 8000)
						StringBuilder.AppendFormat("({0})", type.Type.Length);
					return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}

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

				case ConvertType.NameToDatabase  :
				case ConvertType.NameToSchema    :
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToProcedure :
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

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix = false)
		{
			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is SqlCeDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}

		protected override void BuildIsDistinctPredicate(NullabilityContext nullability, SqlPredicate.IsDistinct expr) => BuildIsDistinctPredicateFallback(nullability, expr);

		protected override void BuildTableExtensions(NullabilityContext nullability, SqlTable table, string alias)
		{
			if (table.SqlQueryExtensions is not null)
				BuildTableExtensions(nullability, StringBuilder, table, alias, " WITH (", ", ", ")");
		}
	}
}
