using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.MySql
{
	public abstract class MySqlSqlBuilder : BasicSqlBuilder<MySqlOptions>
	{
		protected MySqlSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected MySqlSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override bool IsRecursiveCteKeywordRequired   => true;
		public    override bool IsNestedJoinParenthesisRequired => true;
		protected override bool IsValuesSyntaxSupported         => false;
		protected override bool SupportsColumnAliasesInSource   => false;

		protected override bool CanSkipRootAliases(SqlStatement statement)
		{
			if (statement.SelectQuery != null)
			{
				return statement.SelectQuery.From.Tables.Count > 0;
			}

			return true;
		}

		public override int CommandCount(SqlStatement statement)
		{
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			StringBuilder.AppendLine("SELECT LAST_INSERT_ID()");
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override void BuildOffsetLimit(SelectQuery selectQuery)
		{
			SqlOptimizer.ConvertSkipTake(NullabilityContext, MappingSchema, DataOptions, selectQuery, OptimizationContext, out var takeExpr, out var skipExpr);

			if (skipExpr == null)
				base.BuildOffsetLimit(selectQuery);
			else
			{
				AppendIndent()
					.AppendFormat(
						CultureInfo.InvariantCulture,
						"LIMIT {0}, {1}",
						WithStringBuilderBuildExpression(skipExpr),
						takeExpr == null ?
							(object)long.MaxValue :
							WithStringBuilderBuildExpression(takeExpr))
					.AppendLine();
			}
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			// mysql has limited support for types in type-CAST expressions
			if (!forCreateTable)
			{
				switch ((type.DataType, type.Precision, type.Scale, type.Length) switch
				{
					(DataType.Boolean  or
					 DataType.SByte    or
					 DataType.Int16    or
					 DataType.Int32    or
					 DataType.Int64,          _,                   _,                  _                   ) => "SIGNED",
					(DataType.BitArray or // wild guess
					 DataType.Byte     or
					 DataType.UInt16   or
					 DataType.UInt32   or
					 DataType.UInt64,         _,                   _,                  _                   ) => "UNSIGNED",
					(DataType.Money,          _,                   _,                  _                   ) => "DECIMAL(19, 4)",
					(DataType.SmallMoney,     _,                   _,                  _                   ) => "DECIMAL(10, 4)",
					(DataType.DateTime      or
					 DataType.DateTime2     or
					 DataType.SmallDateTime or
					 DataType.DateTimeOffset, _,                   _,                  _                   ) => "DATETIME",
					(DataType.Time,           _,                   _,                  _                   ) => "TIME",
					(DataType.Date,           _,                   _,                  _                   ) => "DATE",
					(DataType.Json,           _,                   _,                  _                   ) => "JSON",
					(DataType.Guid,           _,                   _,                  _                   ) => "CHAR(36)",
					(DataType.Double,         _,                   _,                  _                   ) => "DOUBLE",
					// https://bugs.mysql.com/bug.php?id=87794
					// FLOAT type is garbage and we shouldn't use it for type CASTs
					(DataType.Single,         _,                   _,                  _                   ) => "DOUBLE",
					(DataType.Decimal,        _,                   not null and not 0, _                   ) => string.Create(CultureInfo.InvariantCulture, $"DECIMAL({type.Precision ?? 10}, {type.Scale})"),
					(DataType.Decimal,        not null and not 10, _,                  _                   ) => string.Create(CultureInfo.InvariantCulture, $"DECIMAL({type.Precision})"),
					(DataType.Decimal,        _,                   _,                  _                   ) => "DECIMAL",
					(DataType.Char      or
					 DataType.NChar     or
					 DataType.VarChar   or
					 DataType.NVarChar  or
					 DataType.NText     or
					 DataType.Text,           _,                   _,                  null or > 255 or < 0) => "CHAR(255)",
					(DataType.Char      or
					 DataType.NChar     or
					 DataType.VarChar   or
					 DataType.NVarChar  or
					 DataType.NText     or
					 DataType.Text,           _,                   _,                  1                   ) => "CHAR",
					(DataType.Char      or
					 DataType.NChar     or
					 DataType.VarChar   or
					 DataType.NVarChar  or
					 DataType.NText     or
					 DataType.Text,           _,                   _,                  _                   ) => $"CHAR({type.Length})",
					(DataType.VarBinary or
					 DataType.Binary    or
					 DataType.Blob,           _,                   _,                  null or < 0         ) => "BINARY(255)",
					(DataType.VarBinary or
					 DataType.Binary    or
					 DataType.Blob,           _,                   _,                  1                   ) => "BINARY",
					(DataType.VarBinary or
					 DataType.Binary    or
					 DataType.Blob,           _,                   _,                  _                   ) => $"BINARY({type.Length})",
					_ => null,
				})
				{
					case null        : base.BuildDataTypeFromDataType(type,                forCreateTable, canBeNull); break;
					case var t       : StringBuilder.Append(t);                                                        break;
				}

				return;
			}

			// types for CREATE TABLE statement
			switch ((type.DataType, type.Precision, type.Scale, type.Length) switch
			{
				(DataType.SByte,          _,                   _,                  _                   ) => "TINYINT",
				(DataType.Int16,          _,                   _,                  _                   ) => "SMALLINT",
				(DataType.Int32,          _,                   _,                  _                   ) => "INT",
				(DataType.Int64,          _,                   _,                  _                   ) => "BIGINT",
				(DataType.Byte,           _,                   _,                  _                   ) => "TINYINT UNSIGNED",
				(DataType.UInt16,         _,                   _,                  _                   ) => "SMALLINT UNSIGNED",
				(DataType.UInt32,         _,                   _,                  _                   ) => "INT UNSIGNED",
				(DataType.UInt64,         _,                   _,                  _                   ) => "BIGINT UNSIGNED",
				(DataType.Money,          _,                   _,                  _                   ) => "DECIMAL(19, 4)",
				(DataType.SmallMoney,     _,                   _,                  _                   ) => "DECIMAL(10, 4)",
				(DataType.Decimal,        null,                null,               _                   ) => "DECIMAL",
				(DataType.Decimal,        _,                   _,                  _                   ) => string.Create(CultureInfo.InvariantCulture, $"DECIMAL({type.Precision ?? 29}, {type.Scale ?? 10})"),
				(DataType.DateTime  or
				 DataType.DateTime2 or
				 DataType.SmallDateTime,  > 0 and <= 6,        _,                  _                   ) => string.Create(CultureInfo.InvariantCulture, $"DATETIME({type.Precision})"),
				(DataType.DateTime  or
				 DataType.DateTime2 or
				 DataType.SmallDateTime,  _,                   _,                  _                   ) => "DATETIME",
				(DataType.DateTimeOffset, > 0 and <= 6,        _,                  _                   ) => string.Create(CultureInfo.InvariantCulture, $"TIMESTAMP({type.Precision})"),
				(DataType.DateTimeOffset, _,                   _,                  _                   ) => "TIMESTAMP",
				(DataType.Time,           > 0 and <= 6,        _,                  _                   ) => string.Create(CultureInfo.InvariantCulture, $"TIME({type.Precision})"),
				(DataType.Time,           _,                   _,                  _                   ) => "TIME",
				(DataType.Boolean,        _,                   _,                  _                   ) => "BOOLEAN",
				(DataType.Double,         _,                   _,                  _                   ) => "DOUBLE",
				(DataType.Single,         _,                   _,                  _                   ) => "FLOAT",
				(DataType.BitArray,       _,                   _,                  null                ) =>
					type.SystemType.UnwrapNullableType()
					switch
					{
						var t when t == typeof(byte)  || t == typeof(sbyte)  =>  8,
						var t when t == typeof(short) || t == typeof(ushort) => 16,
						var t when t == typeof(int)   || t == typeof(uint)   => 32,
						var t when t == typeof(long)  || t == typeof(ulong)  => 64,
						_ => 0,
					}
					switch
					{
						0     => "BIT",
						var l => string.Create(CultureInfo.InvariantCulture, $"BIT({l})"),
					},
				(DataType.BitArray,       _,                  _,                   not 1 and >= 0      ) => $"BIT({type.Length})",
				(DataType.BitArray,       _,                  _,                   _                   ) => "BIT",
				(DataType.Date,           _,                  _,                   _                   ) => "DATE",
				(DataType.Json,           _,                  _,                   _                   ) => "JSON",
				(DataType.Guid,           _,                  _,                   _                   ) => "CHAR(36)",
				(DataType.Char    or
				 DataType.NChar,          _,                  _,                   null or > 255 or < 0) => "CHAR(255)",
				(DataType.Char    or
				 DataType.NChar,          _,                  _,                   1                   ) => "CHAR",
				(DataType.Char    or
				 DataType.NChar,          _,                  _,                   _                   ) => $"CHAR({type.Length})",
				(DataType.VarChar or
				 DataType.NVarChar,       _,                  _,                   null or > 65535 or < 0) => "VARCHAR(255)",
				(DataType.VarChar or
				 DataType.NVarChar,       _,                  _,                   _                   ) => $"VARCHAR({type.Length})",
				(DataType.Binary,         _,                  _,                   null or < 0         ) => "BINARY(255)",
				(DataType.Binary,         _,                  _,                   1                   ) => "BINARY",
				(DataType.Binary,         _,                  _,                   _                   ) => $"BINARY({type.Length})",
				(DataType.VarBinary,      _,                  _,                   null or < 0         ) => "VARBINARY(255)",
				(DataType.VarBinary,      _,                  _,                   _                   ) => $"VARBINARY({type.Length})",
				(DataType.Blob,           _,                  _,                   null or < 0         ) => "BLOB",
				(DataType.Blob,           _,                  _,                   <= 255              ) => "TINYBLOB",
				(DataType.Blob,           _,                  _,                   <= 65535            ) => "BLOB",
				(DataType.Blob,           _,                  _,                   <= 16777215         ) => "MEDIUMBLOB",
				(DataType.Blob,           _,                  _,                   _                   ) => "LONGBLOB",
				(DataType.NText or
				 DataType.Text,           _,                  _,                   null or < 0         ) => "TEXT",
				(DataType.NText or
				 DataType.Text,           _,                  _,                   <= 255              ) => "TINYTEXT",
				(DataType.NText or
				 DataType.Text,           _,                  _,                   <= 65535            ) => "TEXT",
				(DataType.NText or
				 DataType.Text,           _,                  _,                   <= 16777215         ) => "MEDIUMTEXT",
				(DataType.NText or
				 DataType.Text,           _,                  _,                   _                   ) => "LONGTEXT",
				_ => null,
			})
			{
				case null  : base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull); break;
				case var t : StringBuilder.Append(t);                                         break;
			}
		}

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			var table = deleteStatement.Table != null ?
				(deleteStatement.SelectQuery.From.FindTableSource(deleteStatement.Table) ?? deleteStatement.Table) :
				deleteStatement.SelectQuery.From.Tables[0];

			var alias = GetTableAlias(table);

			AppendIndent().Append("DELETE ");
			StartStatementQueryExtensions(deleteStatement.SelectQuery);
			StringBuilder.Append(' ');

			if (alias != null)
			{
				StringBuilder.Append(' ');
				Convert(StringBuilder, alias, ConvertType.NameToQueryTableAlias);
			}

			StringBuilder.AppendLine();
		}

		protected override void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery,
			SqlUpdateClause                                    updateClause)
		{
			var pos = StringBuilder.Length;

			base.BuildFromClause(statement, selectQuery);

			StringBuilder.Remove(pos, 4).Insert(pos, "UPDATE");

			BuildUpdateSet(selectQuery, updateClause);
		}

		protected override void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			var nullability = NullabilityContext.GetContext(statement.SelectQuery);

			BuildStep = Step.Tag;          BuildTag(statement);
			BuildStep = Step.InsertClause; BuildInsertClause(statement, insertClause, addAlias);

			if (statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count != 0)
			{
				BuildStep = Step.WithClause;      BuildWithClause        (statement.GetWithClause());
				BuildStep = Step.SelectClause;    BuildSelectClause      (statement.SelectQuery);
				BuildStep = Step.FromClause;      BuildFromClause        (statement, statement.SelectQuery);
				BuildStep = Step.WhereClause;     BuildWhereClause       (statement.SelectQuery);
				BuildStep = Step.GroupByClause;   BuildGroupByClause     (statement.SelectQuery);
				BuildStep = Step.HavingClause;    BuildHavingClause      (statement.SelectQuery);
				BuildStep = Step.OrderByClause;   BuildOrderByClause     (statement.SelectQuery);
				BuildStep = Step.OffsetLimit;     BuildOffsetLimit       (statement.SelectQuery);
				BuildStep = Step.QueryExtensions; BuildSubQueryExtensions(statement);
			}

			if (insertClause.WithIdentity)
				BuildGetIdentity(insertClause);
			else
			{
				BuildOutputSubclause(statement.GetOutputClause());
			}
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (!statement.IsUpdate())
				base.BuildFromClause(statement, selectQuery);
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter  :
				case ConvertType.NameToCommandParameter:
					return sb.Append('@').Append(value);

				case ConvertType.NameToSprocParameter:
					if(string.IsNullOrEmpty(value))
						throw new ArgumentException($"Argument '{nameof(value)}' must represent parameter name.", nameof(value));

					if (value[0] == '@')
						value = value.Substring(1);

					return sb.Append('@').Append(value);

				case ConvertType.SprocParameterToName:
					value = value.Length > 0 && value[0] == '@' ? value.Substring(1) : value;

					return sb.Append(value);

				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToDatabase       :
				case ConvertType.NameToSchema         :
				case ConvertType.NameToPackage        :
				case ConvertType.NameToQueryTable     :
				case ConvertType.NameToCteName        :
				case ConvertType.NameToProcedure      :
					// https://dev.mysql.com/doc/refman/8.0/en/identifiers.html
					value = value.Replace("`", "``", StringComparison.Ordinal);

					return sb.Append('`').Append(value).Append('`');
			}

			return sb.Append(value);
		}

		protected override StringBuilder BuildExpression(ISqlExpression expr,
			bool buildTableName,
			bool checkParentheses,
			string? alias,
			ref bool addAlias,
			bool throwExceptionIfTableNotFound = true)
		{
			return base.BuildExpression(expr,
				buildTableName && Statement.QueryType != QueryType.InsertOrUpdate,
				checkParentheses,
				alias,
				ref addAlias,
				throwExceptionIfTableNotFound);
		}

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr)
		{
			if (!expr.IsNot)
				StringBuilder.Append("NOT ");
			BuildExpression(GetPrecedence(expr), expr.Expr1);
			StringBuilder.Append(" <=> ");
			BuildExpression(GetPrecedence(expr), expr.Expr2);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			var nullability = new NullabilityContext(insertOrUpdate.SelectQuery);

			var position = StringBuilder.Length;

			BuildInsertQuery(insertOrUpdate, insertOrUpdate.Insert, false);

			if (insertOrUpdate.Update.Items.Count > 0)
			{
				AppendIndent().AppendLine("ON DUPLICATE KEY UPDATE");

				Indent++;

				var first = true;

				foreach (var expr in insertOrUpdate.Update.Items)
				{
					if (!first)
						StringBuilder.AppendLine(Comma);
					first = false;

					AppendIndent();
					BuildExpression(expr.Column, false, true);
					StringBuilder.Append(" = ");
					var convertedExpr = ConvertElement(expr.Expression!);
					BuildExpression(convertedExpr, false, true);
				}

				Indent--;

				StringBuilder.AppendLine();
			}
			else
			{
				var sql = StringBuilder.ToString();
				var insertIndex = sql.IndexOf("INSERT", position, StringComparison.Ordinal);

				StringBuilder.Clear()
					.Append(sql.AsSpan(0, insertIndex))
					.Append("INSERT IGNORE")
					.Append(sql.AsSpan(insertIndex + "INSERT".Length));
			}
		}

		protected override void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.AppendLine("() VALUES ()");
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("AUTO_INCREMENT");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			StringBuilder.AppendJoinStrings(InlineComma, fieldNames);
			StringBuilder.Append(')');
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
			if (name.Database != null)
			{
				(escape ? Convert(sb, name.Database, ConvertType.NameToDatabase) : sb.Append(name.Database))
					.Append('.');
			}

			if (name.Package != null)
			{
				(escape ? Convert(sb, name.Package, ConvertType.NameToPackage) : sb.Append(name.Package))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is MySqlDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return string.Format(CultureInfo.InvariantCulture, "{0}", provider.Adapter.GetDbType(param));
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			if (truncateTable.ResetIdentity || truncateTable.Table!.IdentityFields.Count == 0)
				StringBuilder.Append("TRUNCATE TABLE ");
			else
				StringBuilder.Append("DELETE FROM ");
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildTag(dropTable);

			AppendIndent().Append(IsTemporaryTable(dropTable.Table.TableOptions) ? "DROP TEMPORARY TABLE " : "DROP TABLE ");

			if (dropTable.Table.TableOptions.HasDropIfExists())
				StringBuilder.Append("IF EXISTS ");

			BuildPhysicalTable(dropTable.Table!, null);
			StringBuilder.AppendLine();
		}

		private static bool IsTemporaryTable(TableOptions tableOptions)
		{
			return tableOptions.TemporaryOptionValue switch
			{
				0 => false,

				TableOptions.IsTemporary                                                                              or
				TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData or
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     or
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData or
				                                                                    TableOptions.IsLocalTemporaryData or
				                           TableOptions.IsLocalTemporaryStructure                                     or
				                           TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData =>
					true,

				var value =>
					throw new InvalidOperationException($"Incompatible table options '{value}'"),
			};
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}

		protected override void BuildGroupByBody(GroupingType groupingType,
			List<ISqlExpression>                              items)
		{
			if (groupingType is GroupingType.GroupBySets or GroupingType.Default)
			{
				base.BuildGroupByBody(groupingType, items);
				return;
			}

			AppendIndent()
				.AppendLine("GROUP BY");

			Indent++;

			for (var i = 0; i < items.Count; i++)
			{
				AppendIndent();

				var expr = items[i];
				BuildExpression(expr);

				if (i + 1 < items.Count)
					StringBuilder.AppendLine(Comma);
				else
					StringBuilder.AppendLine();
			}

			Indent--;

			switch (groupingType)
			{
				case GroupingType.Rollup:
					StringBuilder.Append("WITH ROLLUP");
					break;
				case GroupingType.Cube:
					StringBuilder.Append("WITH CUBE");
					break;
				default:
					throw new InvalidOperationException($"Unexpected grouping type: {groupingType}");
			}
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			StringBuilder.Append(IsTemporaryTable(table.TableOptions) ? "CREATE TEMPORARY TABLE " : "CREATE TABLE ");

			if (table.TableOptions.HasCreateIfNotExists())
				StringBuilder.Append("IF NOT EXISTS ");
		}

		protected StringBuilder? HintBuilder { get; set; }

		int  _hintPosition;
		bool _isTopLevelBuilder;

		protected override void StartStatementQueryExtensions(SelectQuery? selectQuery)
		{
			if (HintBuilder == null)
			{
				HintBuilder        = new();
				_isTopLevelBuilder = true;
				_hintPosition      = StringBuilder.Length;

				if (Statement is SqlInsertStatement)
					_hintPosition -= " INTO ".Length;

				if (selectQuery?.QueryName is {} queryName)
					HintBuilder
						.Append("QB_NAME(")
						.Append(queryName)
						.Append(')')
						;
			}
			else if (selectQuery?.QueryName is {} queryName)
			{
				StringBuilder
					.Append(" /*+ QB_NAME(")
					.Append(queryName)
					.Append(") */")
					;
			}
		}

		protected override void FinalizeBuildQuery(SqlStatement statement)
		{
			base.FinalizeBuildQuery(statement);

			if (statement.SqlQueryExtensions is not null && HintBuilder is not null)
			{
				if (HintBuilder.Length > 0 && HintBuilder[^1] != ' ')
					HintBuilder.Append(' ');
				BuildQueryExtensions(HintBuilder, statement.SqlQueryExtensions, null, " ", null, Sql.QueryExtensionScope.QueryHint);
			}

			if (_isTopLevelBuilder && HintBuilder!.Length > 0)
			{
				HintBuilder.Insert(0, " /*+ ");
				HintBuilder.Append(" */");

				StringBuilder.InsertBuilder(_hintPosition, HintBuilder);
			}
		}

		protected override void BuildTableExtensions(SqlTable table, string alias)
		{
			if (table.SqlQueryExtensions is not null)
			{
				if (HintBuilder is not null)
					BuildTableExtensions(HintBuilder, table, alias, null, " ", null, ext =>
						ext.Scope is
							Sql.QueryExtensionScope.TableHint or
							Sql.QueryExtensionScope.TablesInScopeHint);

				BuildTableExtensions(StringBuilder, table, alias, " ", ", ", null, ext => ext.Scope is Sql.QueryExtensionScope.IndexHint);
			}
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

				BuildQueryExtensions(StringBuilder, statement.SelectQuery!.SqlQueryExtensions, null, prefix, Environment.NewLine, Sql.QueryExtensionScope.SubQueryHint);
			}
		}

		protected override void BuildSql()
		{
			BuildSqlForUnion();
		}

		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source, IReadOnlyList<List<ISqlExpression>> rows, int row, int column)
		{
			if (row == 0)
			{
				if (rows[0][column] is SqlValue
					{
						Value: uint or long or ulong or float or double or decimal or null
					})
				{
					return true;
				}
			}

			return false;
		}
	}
}
