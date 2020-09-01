using System;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SqlCe
{
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	class SqlCeSqlBuilder : BasicSqlBuilder
	{
		private readonly SqlCeDataProvider? _provider;
		public SqlCeSqlBuilder(
			SqlCeDataProvider? provider,
			MappingSchema      mappingSchema,
			ISqlOptimizer      sqlOptimizer,
			SqlProviderFlags   sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		// remote context
		public SqlCeSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
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

		protected override bool   OffsetFirst => true;

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity ? 1 + trun.Table!.Fields.Values.Count(f => f.IsIdentity) : 1;
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				var field = trun.Table!.Fields.Values.Skip(commandNumber - 1).First(f => f.IsIdentity);

				StringBuilder.Append("ALTER TABLE ");
				ConvertTableName(StringBuilder, trun.Table.Server, trun.Table.Database, trun.Table.Schema, trun.Table.PhysicalName!, trun.Table.TableOptions);
				StringBuilder.Append(" ALTER COLUMN ");
				Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				StringBuilder.AppendLine(" IDENTITY(1,1)");
			}
			else
			{
				StringBuilder.AppendLine("SELECT @@IDENTITY");
			}
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlCeSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Char          : base.BuildDataTypeFromDataType(new SqlDataType(DataType.NChar,    type.Type.Length), forCreateTable); return;
				case DataType.VarChar       : base.BuildDataTypeFromDataType(new SqlDataType(DataType.NVarChar, type.Type.Length), forCreateTable); return;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10,4)");                                                           return;
				case DataType.DateTime2     :
				case DataType.Time          :
				case DataType.Date          :
				case DataType.SmallDateTime : StringBuilder.Append("DateTime");                                                                return;
				case DataType.NVarChar:
					if (type.Type.Length == null || type.Type.Length > 4000 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(4000)");
						return;
					}

					break;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable);
		}

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
					wrap = expr is SqlExpression ex && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlSearchCondition;
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");
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

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions)
		{
			return sb.Append(table);
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

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}
	}
}
