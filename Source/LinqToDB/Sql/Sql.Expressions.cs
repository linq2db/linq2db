using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB
{
	public partial class Sql
	{
		private sealed class FieldsExprBuilderDirect : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var fieldsExpr = (LambdaExpression)builder.Arguments[1].Unwrap();
				var qualified = builder.Arguments.Length <= 2 || builder.GetValue<bool>(2);

				var columns = GetColumnsFromExpression(((MethodInfo)builder.Member).GetGenericArguments()[0], fieldsExpr, builder.Mapping, builder.DataContext.Options);

				var columnExpressions = new ISqlExpression[columns.Length];

				for (var i = 0; i < columns.Length; i++)
					columnExpressions[i] = qualified
						? new SqlField(columns[i])
						: new SqlExpression(columns[i].GetDbDataType(true), columns[i].ColumnName, Precedence.Primary);

				if (columns.Length == 1)
					builder.ResultExpression = columnExpressions[0];
				else
					builder.ResultExpression = new SqlFragment(
						string.Join(", ", Enumerable.Range(0, columns.Length).Select(i => FormattableString.Invariant($"{{{i}}}"))),
						columnExpressions);
			}
		}

		private sealed class FieldNameBuilderDirect : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var fieldExpr    = (LambdaExpression) builder.Arguments[1].Unwrap();
				var qualified    = builder.Arguments.Length <= 2 || builder.GetValue<bool>(2);
				var isExpression = builder.Member.Name == "FieldExpr";

				var column = GetColumnFromExpression(((MethodInfo)builder.Member).GetGenericArguments()[0], fieldExpr, builder.Mapping, builder.DataContext.Options);

				if (isExpression)
				{
					builder.ResultExpression = qualified
						? new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), "{0}", Precedence.Primary, new SqlField(column))
						: new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), column.ColumnName, Precedence.Primary);
				}
				else
				{
					var name = column.ColumnName;

					if (qualified)
						name = builder.DataContext.CreateSqlBuilder().ConvertInline(name, ConvertType.NameToQueryField);

					builder.ResultExpression = new SqlValue(name);
				}
			}
		}

		sealed class FieldNameBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var fieldExpr    = builder.GetExpression(0)!;
				var qualified    = builder.Arguments.Length <= 1 || builder.GetValue<bool>(1);
				var isExpression = builder.Member.Name == "FieldExpr";

				var field = QueryHelper.ExtractField(fieldExpr);
				if (field == null)
					throw new LinqToDBException($"Cannot convert expression {builder.Arguments[0]} to field.");

				if (isExpression)
				{
					builder.ResultExpression = qualified
						? new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), "{0}", Precedence.Primary, new SqlField(field))
						: new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), field.PhysicalName, Precedence.Primary);
				}
				else
				{
					var name = field.PhysicalName;

					if (qualified)
						name = builder.DataContext.CreateSqlBuilder().ConvertInline(name, ConvertType.NameToQueryField);

					builder.ResultExpression = new SqlValue(name);
				}
			}
		}

		[Pure]
		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static string FieldName<T>([NoEnumeration] ITable<T> table, Expression<Func<T, object>> fieldExpr)
			where T : notnull
		{
			return FieldName(table, fieldExpr, true);
		}

		[Pure]
		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static string FieldName<T>([NoEnumeration] ITable<T> table, Expression<Func<T, object>> fieldExpr, [SqlQueryDependent] bool qualified)
			where T : notnull
		{
			var column = GetColumnFromExpression(typeof(T), fieldExpr, table.DataContext.MappingSchema, table.DataContext.Options);

			var result = column.ColumnName;
			if (qualified)
			{
				var sqlBuilder = table.DataContext.CreateSqlBuilder();
				result         = sqlBuilder.ConvertInline(result, ConvertType.NameToQueryField);
			}

			return result;
		}

		[Flags]
		public enum TableQualification
		{
			None         = 0b00000000,
			TableName    = 0b00000001,
			DatabaseName = 0b00000010,
			SchemaName   = 0b00000100,
			ServerName   = 0b00001000,
			TableOptions = 0b00010000,

			Full         = TableName | DatabaseName | SchemaName | ServerName | TableOptions
		}

		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static ISqlExpression FieldExpr<T, TV>([NoEnumeration] ITable<T> table, Expression<Func<T, TV>> fieldExpr)
			where T : notnull
		{
			return FieldExpr(table, fieldExpr, true);
		}

		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static ISqlExpression FieldExpr<T, TV>([NoEnumeration] ITable<T> table, Expression<Func<T, TV>> fieldExpr, bool qualified)
			where T : notnull
		{
			var column = GetColumnFromExpression(typeof(T), fieldExpr, table.DataContext.MappingSchema, table.DataContext.Options);

			if (qualified)
			{
				return new SqlField(column);
			}

			return new SqlExpression(column.GetDbDataType(true), column.ColumnName, Precedence.Primary);
		}

		[Extension("", BuilderType = typeof(FieldsExprBuilderDirect), ServerSideOnly = false)]
		internal static ISqlExpression FieldsExpr<T>([NoEnumeration] ITable<T> table, Expression<Func<T, object?>> fieldsExpr)
			where T : notnull
		{
			return FieldsExpr(table, fieldsExpr, true);
		}

		[Extension("", BuilderType = typeof(FieldsExprBuilderDirect), ServerSideOnly = false)]
		internal static ISqlExpression FieldsExpr<T>([NoEnumeration] ITable<T> table, Expression<Func<T, object?>> fieldsExpr, bool qualified)
			where T : notnull
		{
			var columns = GetColumnsFromExpression(typeof(T), fieldsExpr, table.DataContext.MappingSchema, table.DataContext.Options);

			var columnExpressions = new ISqlExpression[columns.Length];

			for (var i = 0; i < columns.Length; i++)
				columnExpressions[i] = qualified
					? new SqlField(columns[i])
					: new SqlExpression(columns[i].GetDbDataType(true), columns[i].ColumnName, Precedence.Primary);

			if (columns.Length == 1)
				return columnExpressions[0];

			return new SqlFragment(
				string.Join(", ", Enumerable.Range(0, columns.Length).Select(i => FormattableString.Invariant($"{{{i}}}"))),
				columnExpressions);
		}

		private static ColumnDescriptor[] GetColumnsFromExpression(Type entityType, LambdaExpression fieldExpr, MappingSchema mappingSchema, DataOptions options)
		{
			if (!(fieldExpr.Body is NewExpression init))
				return new[] { GetColumnFromExpression(entityType, fieldExpr, mappingSchema, options) };

			if (init.Arguments == null || init.Arguments.Count == 0)
				throw new LinqToDBException($"Cannot extract columns info from expression {fieldExpr.Body}");

			var ed = mappingSchema.GetEntityDescriptor(entityType, options.ConnectionOptions.OnEntityDescriptorCreated);
			var columns = new ColumnDescriptor[init.Arguments.Count];
			for (var i = 0; i < init.Arguments.Count; i++)
			{
				var memberInfo = MemberHelper.GetMemberInfo(init.Arguments[i]);
				if (memberInfo == null)
					throw new LinqToDBException($"Cannot extract member info from expression {init.Arguments[i]}");

				var column = ed.FindColumnDescriptor(memberInfo);

				columns[i] = column ?? throw new LinqToDBException($"Cannot find column for member {entityType.Name}.{memberInfo.Name}");
			}

			return columns;
		}

		private static ColumnDescriptor GetColumnFromExpression(Type entityType, LambdaExpression fieldExpr, MappingSchema mappingSchema, DataOptions options)
		{
			var memberInfo = MemberHelper.GetMemberInfo(fieldExpr.Body);
			if (memberInfo == null)
				throw new LinqToDBException($"Cannot extract member info from expression {fieldExpr.Body}");

			var ed     = mappingSchema.GetEntityDescriptor(entityType, options.ConnectionOptions.OnEntityDescriptorCreated);
			var column = ed.FindColumnDescriptor(memberInfo);

			if (column == null)
				throw new LinqToDBException($"Cannot find column for member {entityType.Name}.{memberInfo.Name}");
			return column;
		}

		[Pure]
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static string FieldName(object fieldExpr)
			=> throw new ServerSideOnlyException(nameof(FieldName));

		[Pure]
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static string FieldName(object fieldExpr, bool qualified)
			=> throw new ServerSideOnlyException(nameof(FieldName));

		[Pure]
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static ISqlExpression FieldExpr(object fieldExpr)
			=> throw new ServerSideOnlyException(nameof(FieldExpr));

		[Pure]
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static ISqlExpression FieldExpr(object fieldExpr, bool qualified)
			=> throw new ServerSideOnlyException(nameof(FieldExpr));

		private abstract class TableHelper
		{
			public abstract string?      ServerName   { get; }
			public abstract string?      DatabaseName { get; }
			public abstract string?      SchemaName   { get; }
			public abstract string       TableName    { get; }
			public abstract TableOptions TableOptions { get; }
		}

		private sealed class TableHelper<T> : TableHelper
			where T : notnull
		{
			private readonly ITable<T> _table;

			public TableHelper(ITable<T> table)
			{
				_table = table;
			}

			public override string?      ServerName   => _table.ServerName;
			public override string?      DatabaseName => _table.DatabaseName;
			public override string?      SchemaName   => _table.SchemaName;
			public override string       TableName    => _table.TableName;
			public override TableOptions TableOptions => _table.TableOptions;
		}

		private sealed class TableNameBuilderDirect : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableExpr    = builder.EvaluateExpression(builder.Arguments[0]);
				var tableType    = ((MethodInfo)builder.Member).GetGenericArguments()[0];
				var helperType   = typeof(TableHelper<>).MakeGenericType(tableType);
				var tableHelper  = ActivatorExt.CreateInstance<TableHelper>(helperType, tableExpr);
				var qualified    = builder.Arguments.Length <= 1 ? TableQualification.Full : builder.GetValue<TableQualification>(1);
				var isExpression = builder.Member.Name == "TableExpr";

				if (isExpression)
				{
					if (qualified == TableQualification.None)
						builder.ResultExpression = new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), tableHelper.TableName, Precedence.Primary);
					else
					{
						var tableName = new SqlObjectName(
							tableHelper.TableName,
							Server  : (qualified & TableQualification.ServerName)   != 0 ? tableHelper.ServerName   : null,
							Database: (qualified & TableQualification.DatabaseName) != 0 ? tableHelper.DatabaseName : null,
							Schema  : (qualified & TableQualification.SchemaName)   != 0 ? tableHelper.SchemaName   : null);
						var table = new SqlTable(builder.Mapping.GetEntityDescriptor(tableType, builder.DataContext.Options.ConnectionOptions.OnEntityDescriptorCreated))
						{
							TableName    = tableName,
							TableOptions = (qualified & TableQualification.TableOptions) != 0 ? tableHelper.TableOptions : TableOptions.NotSet,
						};

						builder.ResultExpression = table;
					}
				}
				else
				{
					var name = tableHelper.TableName;

					if (qualified != TableQualification.None)
					{
						using var sb = Pools.StringBuilder.Allocate();
						builder.DataContext.CreateSqlBuilder().BuildObjectName(
							sb.Value,
							new SqlObjectName(
								name,
								Server  : (qualified & TableQualification.ServerName)   != 0 ? tableHelper.ServerName   : null,
								Database: (qualified & TableQualification.DatabaseName) != 0 ? tableHelper.DatabaseName : null,
								Schema  : (qualified & TableQualification.SchemaName)   != 0 ? tableHelper.SchemaName   : null),
							ConvertType.NameToQueryTable,
							true,
							(qualified & TableQualification.TableOptions) != 0 ? tableHelper.TableOptions : TableOptions.NotSet);
						name = sb.Value.ToString();
					}

					builder.ResultExpression = new SqlValue(name);
				}
			}
		}

		private sealed class TableNameBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableExpr = builder.GetExpression(0);
				var sqlTable  = QueryHelper.ExtractSqlTable(tableExpr);

				//TODO: review, maybe we need here TableSource
				if (sqlTable == null)
					throw new LinqToDBException("Cannot find Table associated with expression");

				var qualified    = builder.Arguments.Length <= 1 ? TableQualification.Full : builder.GetValue<TableQualification>(1);
				var isExpression = builder.Member.Name == "TableExpr";

				var name = sqlTable.TableName.Name;

				if (qualified != TableQualification.None)
				{
					using var sb = Pools.StringBuilder.Allocate();

					builder.DataContext.CreateSqlBuilder().BuildObjectName(
						sb.Value,
						new SqlObjectName(
							sqlTable.TableName.Name,
							Server  : (qualified & TableQualification.ServerName)   != 0 ? sqlTable.TableName.Server       : null,
							Database: (qualified & TableQualification.DatabaseName) != 0 ? sqlTable.TableName.Database     : null,
							Schema  : (qualified & TableQualification.SchemaName)   != 0 ? sqlTable.TableName.Schema       : null),
						sqlTable.SqlTableType == SqlTableType.Function ? ConvertType.NameToProcedure : ConvertType.NameToQueryTable,
						true,
						(qualified & TableQualification.TableOptions) != 0 ? sqlTable.TableOptions : TableOptions.NotSet);

					name = sb.Value.ToString();
				}

				builder.ResultExpression = isExpression
					? new SqlFragment(name)
					: new SqlValue(name);
			}
		}

		private sealed class TableSourceBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				builder.ResultExpression = SqlAliasPlaceholder.Instance;
			}
		}

		private sealed class TableOrColumnAsFieldBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableOrColumnExpr = builder.GetExpression(0)!;

				var anchor = new SqlAnchor(tableOrColumnExpr, SqlAnchor.AnchorKindEnum.TableAsSelfColumnOrField);

				builder.ResultExpression = anchor;
			}
		}

		private sealed class TableAsFieldBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableExpr = builder.GetExpression(0)!;

				var anchor = new SqlAnchor(tableExpr, SqlAnchor.AnchorKindEnum.TableAsSelfColumn);

				builder.ResultExpression = anchor;
			}
		}

		[ExpressionMethod(nameof(TableFieldIml))]
		public static TColumn TableField<TEntity, TColumn>([NoEnumeration] TEntity entity, string fieldName)
			=> throw new ServerSideOnlyException(nameof(TableField));

		static Expression<Func<TEntity, string, TColumn>> TableFieldIml<TEntity, TColumn>()
		{
			return (entity, fieldName) => Property<TColumn>(entity, fieldName);
		}

		[Extension("", BuilderType = typeof(TableOrColumnAsFieldBuilder), ServerSideOnly = true)]
		internal static TColumn TableOrColumnAsField<TColumn>([NoEnumeration] object? entityOrColumn)
			=> throw new ServerSideOnlyException(nameof(TableOrColumnAsField));

		[Extension("", BuilderType = typeof(TableAsFieldBuilder), ServerSideOnly = true)]
		internal static TColumn TableAsField<TEntity, TColumn>([NoEnumeration] TEntity entity)
			=> throw new ServerSideOnlyException(nameof(TableAsField));

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static string TableName<T>([NoEnumeration] ITable<T> table)
			where T : notnull
		{
			return TableName(table, TableQualification.Full);
		}

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static string TableName<T>([NoEnumeration] ITable<T> table, [SqlQueryDependent] TableQualification qualification)
			where T : notnull
		{
			var result = table.TableName;

			if (qualification != TableQualification.None)
			{
				var sqlBuilder = table.DataContext.CreateSqlBuilder();
				using var sb   = Pools.StringBuilder.Allocate();
				sqlBuilder.BuildObjectName(
					sb.Value,
					new SqlObjectName(
						table.TableName,
						Server  : (qualification & TableQualification.ServerName)   != 0 ? table.ServerName   : null,
						Database: (qualification & TableQualification.DatabaseName) != 0 ? table.DatabaseName : null,
						Schema  : (qualification & TableQualification.SchemaName)   != 0 ? table.SchemaName   : null),
					ConvertType.NameToQueryTable,
					true,
					(qualification & TableQualification.TableOptions) != 0 ? table.TableOptions : TableOptions.NotSet);
				result = sb.Value.ToString();
			}

			return result;
		}

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static string TableName(object tableExpr)
			=> throw new ServerSideOnlyException(nameof(TableName));

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static string TableName(object tableExpr, [SqlQueryDependent] TableQualification qualification)
			=> throw new ServerSideOnlyException(nameof(TableName));

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static ISqlExpression TableExpr<T>([NoEnumeration] ITable<T> table)
			where T : notnull
		{
			return TableExpr(table, TableQualification.Full);
		}

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static ISqlExpression TableExpr<T>([NoEnumeration] ITable<T> table, [SqlQueryDependent] TableQualification qualification)
			where T : notnull
		{
			var name = table.TableName;

			if (qualification != TableQualification.None)
			{
				var sqlBuilder = table.DataContext.CreateSqlBuilder();
				using var sb   = Pools.StringBuilder.Allocate();

				sqlBuilder.BuildObjectName(
					sb.Value,
					new SqlObjectName(
						table.TableName,
						Server  : (qualification & TableQualification.ServerName)   != 0 ? table.ServerName   : null,
						Database: (qualification & TableQualification.DatabaseName) != 0 ? table.DatabaseName : null,
						Schema  : (qualification & TableQualification.SchemaName)   != 0 ? table.SchemaName   : null),
					ConvertType.NameToQueryTable,
					true,
					(qualification & TableQualification.TableOptions) != 0 ? table.TableOptions : TableOptions.NotSet);

				name = sb.Value.ToString();
			}

			return new SqlFragment(name);
		}

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static ISqlExpression TableExpr(object tableExpr)
			=> throw new ServerSideOnlyException(nameof(TableExpr));

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static ISqlExpression TableExpr(object tableExpr, [SqlQueryDependent] TableQualification qualification)
			=> throw new ServerSideOnlyException(nameof(TableExpr));

		sealed class AliasExprBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				builder.ResultExpression = SqlAliasPlaceholder.Instance;
			}
		}

		/// <summary>
		/// Useful for specifying place of alias when using <see cref="DataExtensions.FromSql{TEntity}(IDataContext, RawSqlString, object?[])"/> method.
		/// </summary>
		/// <remarks>
		///		If <see cref="DataExtensions.FromSql{TEntity}(IDataContext, RawSqlString, object?[])"/> contains at least one <see cref="AliasExpr"/>,
		///		automatic alias for the query will be not generated.
		/// </remarks>
		/// <returns>ISqlExpression which is Alias Placeholder.</returns>
		/// <example>
		/// The following <see cref="DataExtensions.FromSql{TEntity}(IDataContext, RawSqlString, object?[])"/> calls are equivalent.
		/// <code>
		/// db.FromSql&lt;int&gt;($"select 1 as value from TableA {Sql.AliasExpr()}")
		/// db.FromSql&lt;int&gt;($"select 1 as value from TableA")
		/// </code>
		/// </example>
		[Extension(builderType: typeof(AliasExprBuilder), ServerSideOnly = true)]
		public static ISqlExpression AliasExpr() => SqlAliasPlaceholder.Instance;

		sealed class ExprBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				Linq.Builder.TableBuilder.PrepareRawSqlArguments(builder.Arguments[0],
					builder.Arguments.Length > 1 ? builder.Arguments[1] : null,
					out var format, out var arguments);

				var memberType = builder.Member.GetMemberType();

				var sqlArguments = arguments.Select(e => builder.ConvertExpressionToSql(e)).ToArray();

				if (sqlArguments.Any(a => a == null))
					builder.IsConvertible = false;
				else
				{
					builder.ResultExpression = new SqlExpression(
						builder.Mapping.GetDbDataType(memberType),
						format,
						Precedence.Primary,
						memberType == typeof(bool) ? SqlFlags.IsPredicate | SqlFlags.IsPure : SqlFlags.IsPure,
						ExpressionAttribute.ToParametersNullabilityType(builder.IsNullable),
						builder.CanBeNull,
						sqlArguments!);
				}
			}
		}

		[Extension("", BuilderType = typeof(ExprBuilder), ServerSideOnly = true)]
		[StringFormatMethod("sql")]
		public static T Expr<T>(FormattableString sql)
			=> throw new ServerSideOnlyException(nameof(Expr));

		[Extension("", BuilderType = typeof(ExprBuilder), ServerSideOnly = true)]
		[StringFormatMethod("sql")]
		public static T Expr<T>(
			[SqlQueryDependent]              RawSqlString sql,
			[SqlQueryDependentParams] params object[]     parameters
			)
			=> throw new ServerSideOnlyException(nameof(Expr));

	}
}
