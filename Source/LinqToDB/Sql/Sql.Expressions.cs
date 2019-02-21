using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;

namespace LinqToDB
{
	using Common;
	using Expressions;
	using Linq;
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using Extensions;

	public partial class Sql
	{
		private class FieldNameBuilderDirect : Sql.IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var fieldExpr    = (LambdaExpression) builder.Arguments[1].Unwrap();
				var qualified    = builder.Arguments.Length <= 2 || builder.GetValue<bool>(2);
				var isExpression = builder.Member.Name == "FieldExpr";

				var column = GetColumnFromExpression(((MethodInfo)builder.Member).GetGenericArguments()[0], fieldExpr, builder.Mapping);

				if (isExpression)
				{
					builder.ResultExpression = qualified
						? new SqlExpression(typeof(string), "{0}", Precedence.Primary, new SqlField(column))
						: new SqlExpression(typeof(string), column.ColumnName, Precedence.Primary);
				}
				else
				{
					var name = column.ColumnName;

					if (qualified)
						name = (string)builder.DataContext.CreateSqlProvider().Convert(name, ConvertType.NameToQueryField);

					builder.ResultExpression = new SqlValue(name);
				}
			}
		}

		class FieldNameBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var fieldExpr    = builder.GetExpression(0);
				var qualified    = builder.Arguments.Length <= 1 || builder.GetValue<bool>(1);
				var isExpression = builder.Member.Name == "FieldExpr";

				var field = QueryHelper.GetUnderlyingField(fieldExpr);
				if (field == null)
					throw new LinqToDBException($"Can not convert expression {builder.Arguments[1]} to field.");

				if (isExpression)
				{
					builder.ResultExpression = qualified
						? new SqlExpression(typeof(string), "{0}", Precedence.Primary, new SqlField(field))
						: new SqlExpression(typeof(string), field.PhysicalName, Precedence.Primary);
				}
				else
				{
					var name = field.PhysicalName;

					if (qualified)
						name = (string)builder.DataContext.CreateSqlProvider().Convert(name, ConvertType.NameToQueryField);

					builder.ResultExpression = new SqlValue(name);
				}
			}
		}

		[Pure]
		[Sql.Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = false)]
		public static string FieldName<T>([NoEnumeration] ITable<T> table, Expression<Func<T, object>> fieldExpr)
		{
			return FieldName(table, fieldExpr, true);
		}

		[Pure]
		[Sql.Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = false)]
		public static string FieldName<T>([NoEnumeration] ITable<T> table, Expression<Func<T, object>> fieldExpr, [SqlQueryDependent] bool qualified)
		{
			var mappingSchema = MappingSchema.Default;

			var dataContext = GetDataContext(table);
			if (dataContext != null)
				mappingSchema = dataContext.MappingSchema;

			var column = GetColumnFromExpression(typeof(T), fieldExpr, mappingSchema);

			var result = column.ColumnName;
			if (qualified)
			{
				if (dataContext == null)
					throw new LinqToDBException("Can not provide information for qualified field name");

				var sqlBuilder = dataContext.CreateSqlProvider();
				result = sqlBuilder.Convert(result, ConvertType.NameToQueryField) as string;
			}
				
			return result;
		}

		[Flags]
		public enum TableQualification
		{
			None = 0x0,
			TableName    = 0x1,
			DatabaseName = 0x2,
			SchemaName   = 0x4,

			Full = DatabaseName | SchemaName
		}

		[Sql.Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = false)]
		public static ISqlExpression FieldExpr<T, TV>([NoEnumeration] ITable<T> table, Expression<Func<T, TV>> fieldExpr)
		{
			return FieldExpr(table, fieldExpr, true);
		}

		[Sql.Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = false)]
		public static ISqlExpression FieldExpr<T, TV>([NoEnumeration] ITable<T> table, Expression<Func<T, TV>> fieldExpr, bool qualified)
		{
			var mappingSchema = MappingSchema.Default;

			var dataContext = GetDataContext(table);
			if (dataContext != null)
				mappingSchema = dataContext.MappingSchema;

			var column = GetColumnFromExpression(typeof(T), fieldExpr, mappingSchema);

			if (qualified)
			{
				return new SqlField(column);
			}
				
			return new SqlExpression(column.ColumnName, Precedence.Primary);
		}

		private static IDataContext GetDataContext<T>(ITable<T> table)
		{
			if (table is ExpressionQuery<T> query)
				return query.DataContext;

			if (table is TempTable<T> temp)
				return temp.DataContext;

			return null;
		}

		private static ColumnDescriptor GetColumnFromExpression(Type entityType, LambdaExpression fieldExpr, MappingSchema mappingSchema)
		{
			var memberInfo = MemberHelper.GetMemberInfo(fieldExpr.Body);
			if (memberInfo == null)
				throw new LinqToDBException($"Can not extract member info from expression {fieldExpr.Body}");

			var ed = mappingSchema.GetEntityDescriptor(entityType);
			var column = ed.Columns.FirstOrDefault(c => c.MemberInfo == memberInfo);

			if (column == null)
				throw new LinqToDBException($"Can not find column for member {entityType.Name}.{memberInfo.Name}");
			return column;
		}

		[Pure]
		[Sql.Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static string FieldName(object fieldExpr)
		{
			throw new LinqToDBException("'Sql.FieldName' is server side only method and used only for generating custom SQL parts");
		}

		[Pure]
		[Sql.Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static string FieldName(object fieldExpr, bool qualified)
		{
			throw new LinqToDBException("'Sql.FieldName' is server side only method and used only for generating custom SQL parts");
		}

		[Pure]
		[Sql.Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static ISqlExpression FieldExpr(object fieldExpr)
		{
			throw new LinqToDBException("'Sql.FieldExpr' is server side only method and used only for generating custom SQL parts");
		}

		[Pure]
		[Sql.Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static ISqlExpression FieldExpr(object fieldExpr, bool qualified)
		{
			throw new LinqToDBException("'Sql.FieldExpr' is server side only method and used only for generating custom SQL parts");
		}

		private abstract class TableHelper
		{
			public abstract string DatabaseName { get; }
			public abstract string SchemaName   { get; }
			public abstract string TableName    { get; }
		}

		private class TableHelper<T> : TableHelper
		{
			private readonly ITable<T> _table;

			public TableHelper(ITable<T> table)
			{
				_table = table;
			}

			public override string DatabaseName => _table.DatabaseName;
			public override string SchemaName   => _table.SchemaName;
			public override string TableName    => _table.TableName;
		}


		private class TableNameBuilderDirect : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableExpr    = builder.Arguments[0].EvaluateExpression();
				var tableType    = ((MethodInfo)builder.Member).GetGenericArguments()[0];
				var helperType   = typeof(TableHelper<>).MakeGenericType(tableType);
				var tableHelper  = (TableHelper)Activator.CreateInstance(helperType, tableExpr);
				var qualified    = builder.Arguments.Length <= 1 ? TableQualification.Full : builder.GetValue<TableQualification>(1);
				var isExpression = builder.Member.Name == "TableExpr";

				if (isExpression)
				{
					if (qualified == TableQualification.None)
						builder.ResultExpression = new SqlExpression(typeof(string), tableHelper.TableName, Precedence.Primary);
					else
					{
						var table = new SqlTable(tableType);
						table.PhysicalName = tableHelper.TableName;
						table.Database     = (qualified & TableQualification.DatabaseName) != 0 ? tableHelper.DatabaseName : null;
						table.Schema       = (qualified & TableQualification.SchemaName)   != 0 ? tableHelper.SchemaName   : null;

						builder.ResultExpression = table;
					}
				}
				else
				{
					var name = tableHelper.TableName;

					if (qualified != TableQualification.None)
					{
						var sb = new StringBuilder();
						builder.DataContext.CreateSqlProvider().ConvertTableName(sb, 
							(qualified & TableQualification.DatabaseName) != 0 ? tableHelper.DatabaseName : null,
							(qualified & TableQualification.SchemaName)   != 0 ? tableHelper.SchemaName   : null,
							name);
						name = sb.ToString();
					}

					builder.ResultExpression = new SqlValue(name);
				}
			}
		}

		private class TableNameBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableExpr     = builder.GetExpression(0);
				SqlTable sqlTable = null;
				if (tableExpr is SqlTable t)
					sqlTable = t;
				else if (tableExpr is SqlField field)
				{
					sqlTable = field.Table as SqlTable;
				}
				else if (tableExpr is SqlColumn column)
				{
					sqlTable = QueryHelper.GetUnderlyingField(column)?.Table as SqlTable;
				}

				//TODO: review, maybe we need here TableSource
				if (sqlTable == null)
					throw new LinqToDBException("Can not find Table associated with expression");

				var qualified    = builder.Arguments.Length <= 1 ? TableQualification.Full : builder.GetValue<TableQualification>(1);
				var isExpression = builder.Member.Name == "TableExpr";

				var name = sqlTable.PhysicalName;

				if (qualified != TableQualification.None)
				{
					var sb = new StringBuilder();
					builder.DataContext.CreateSqlProvider().ConvertTableName(sb, 
						(qualified & TableQualification.DatabaseName) != 0 ? sqlTable.Database : null,
						(qualified & TableQualification.SchemaName)   != 0 ? sqlTable.Schema   : null,
						sqlTable.PhysicalName);
					name = sb.ToString();
				}

				builder.ResultExpression = isExpression
					? new SqlExpression(name, Precedence.Primary)
					: (ISqlExpression)new SqlValue(name);
			}
		}

		[Sql.Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static string TableName<T>([NoEnumeration] ITable<T> table)
		{
			return TableName(table, TableQualification.Full);
		}

		[Sql.Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static string TableName<T>([NoEnumeration] ITable<T> table, [SqlQueryDependent] TableQualification qualification)
		{
			var result = table.TableName;
			if (qualification != TableQualification.None)
			{
				var dataContext = GetDataContext(table);
				if (dataContext == null)
					throw new LinqToDBException("Can not provide information for qualified table name");

				var sqlBuilder = dataContext.CreateSqlProvider();
				var sb = new StringBuilder();
				sqlBuilder.ConvertTableName(sb, 
					(qualification & TableQualification.DatabaseName) != 0 ? table.DatabaseName : null,
					(qualification & TableQualification.SchemaName)   != 0 ? table.SchemaName   : null,
					table.TableName);
				result = sb.ToString();
			}
				
			return result;
		}

		[Sql.Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static string TableName(object tableExpr)
		{
			throw new LinqToDBException("'Sql.TableName' is server side only method and used only for generating custom SQL parts");
		}

		[Sql.Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static string TableName(object tableExpr, [SqlQueryDependent] TableQualification qualification)
		{
			throw new LinqToDBException("'Sql.TableName' is server side only method and used only for generating custom SQL parts");
		}

		[Sql.Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static ISqlExpression TableExpr<T>([NoEnumeration] ITable<T> table)
		{
			return TableExpr(table, TableQualification.Full);
		}

		[Sql.Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static ISqlExpression TableExpr<T>([NoEnumeration] ITable<T> table, [SqlQueryDependent] TableQualification qualification)
		{
			var name = table.TableName;

			if (qualification != TableQualification.None)
			{
				var dataContext = GetDataContext(table);
				if (dataContext == null)
					throw new LinqToDBException("Can not provide information for qualified table name");

				var sqlBuilder = dataContext.CreateSqlProvider();
				var sb = new StringBuilder();
				sqlBuilder.ConvertTableName(sb,
					(qualification & TableQualification.DatabaseName) != 0 ? table.DatabaseName : null,
					(qualification & TableQualification.SchemaName)   != 0 ? table.SchemaName   : null,
					table.TableName);
				name = sb.ToString();
			}

			return new SqlExpression(name, Precedence.Primary);
		}

		[Sql.Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static ISqlExpression TableExpr(object tableExpr)
		{
			throw new LinqToDBException("'Sql.TableExpr' is server side only method and used only for generating custom SQL parts");
		}

		[Sql.Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static ISqlExpression TableExpr(object tableExpr, [SqlQueryDependent] TableQualification qualification)
		{
			throw new LinqToDBException("'Sql.TableExpr' is server side only method and used only for generating custom SQL parts");
		}

		class CustomExtensionAttribute : Sql.ExtensionAttribute
		{
			public CustomExtensionAttribute(string expression) : base(expression)
			{
			}

			public override bool GetIsPredicate(Expression expression) => expression.Type == typeof(bool);
		}

		class ExprBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				Linq.Builder.TableBuilder.PrepareRawSqlArguments(builder.Arguments[0],
					builder.Arguments.Length > 1 ? builder.Arguments[1] : null,
					out var format, out var arguments);

				var memberType = builder.Member.GetMemberType();

				var sqlArguments = arguments.Select(builder.ConvertExpressionToSql).ToArray();

				builder.ResultExpression = new SqlExpression(memberType, format, Precedence.Primary, sqlArguments);
			}
		}

#if !NET45
		[CustomExtension("", BuilderType = typeof(ExprBuilder), ServerSideOnly = true)]
		[StringFormatMethod("sql")]
		public static T Expr<T>(
			[JetBrains.Annotations.NotNull, DataExtensions.SqlFormattableComparer] FormattableString sql
			)
		{
			throw new LinqToDBException("'Sql.Expr' is server side only method and used only for generating custom SQL parts");
		}
#endif		
		[CustomExtension("", BuilderType = typeof(ExprBuilder), ServerSideOnly = true)]
		[StringFormatMethod("sql")]
		public static T Expr<T>(
			[SqlQueryDependent]                RawSqlString sql,
			[JetBrains.Annotations.NotNull] params object[] parameters
			)
		{
			throw new LinqToDBException("'Sql.Expr' is server side only method and used only for generating custom SQL parts");
		}

	}
}
