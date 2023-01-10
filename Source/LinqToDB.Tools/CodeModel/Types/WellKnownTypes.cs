using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// This class contains pre-parsed <see cref="IType"/> definitions and member references for well-known system and Linq To DB types,
	/// used during code generation.
	/// </summary>
	public static class WellKnownTypes
	{
		// use C# type parser for known types parsing (it doesn't affect parsing of types in this file)
		private static ITypeParser Parser => CSharpLanguageProvider.Instance.TypeParser;

		public static class System
		{
			private static readonly IType _func1       = Parser.Parse(typeof(Func<,>));
			private static readonly IType _func2       = Parser.Parse(typeof(Func<,,>));
			private static readonly IType _iequatableT = Parser.Parse(typeof(IEquatable<>));

			/// <summary>
			/// <see cref="bool"/> type descriptor.
			/// </summary>
			public static IType Boolean                   { get; } = Parser.Parse<bool>();
			/// <summary>
			/// <see cref="string"/> type descriptor.
			/// </summary>
			public static IType String                    { get; } = Parser.Parse<string>();
			/// <summary>
			/// <see cref="object"/> type descriptor.
			/// </summary>
			public static IType Object                    { get; } = Parser.Parse<object>();
			/// <summary>
			/// <see cref="int"/> type descriptor.
			/// </summary>
			public static IType Int32                     { get; } = Parser.Parse<int>();
			/// <summary>
			/// <see cref="long"/> type descriptor.
			/// </summary>
			public static IType Int64                     { get; } = Parser.Parse<long>();
			/// <summary>
			/// <see cref="InvalidOperationException"/> type descriptor.
			/// </summary>
			public static IType InvalidOperationException { get; } = Parser.Parse<InvalidOperationException>();
			/// <summary>
			/// <see cref="object"/>? type descriptor.
			/// </summary>
			public static IType ObjectNullable            { get; } = Object.WithNullability(true);
			/// <summary>
			/// <see cref="object"/>?[] type descriptor.
			/// </summary>
			public static IType ObjectArrayNullable       { get; } = new ArrayType(Object, new int?[] { null }, true);

			/// <summary>
			/// Returns <see cref="Func{T, TResult}"/> type descriptor.
			/// </summary>
			/// <param name="returnType">Return value type.</param>
			/// <param name="arg0">Argument type.</param>
			/// <returns>Type descriptor.</returns>
			public static IType Func(IType returnType, IType arg0) => _func1.WithTypeArguments(arg0, returnType);
			/// <summary>
			/// Returns <see cref="Func{T1, T2, TResult}"/> type descriptor.
			/// </summary>
			/// <param name="returnType">Return value type.</param>
			/// <param name="arg0">First argument type.</param>
			/// <param name="arg1">Second argument type.</param>
			/// <returns>Type descriptor.</returns>
			public static IType Func(IType returnType, IType arg0, IType arg1) => _func2.WithTypeArguments(arg0, arg1, returnType);

			/// <summary>
			/// Returns <see cref="IEquatable{T}"/> type descriptor.
			/// </summary>
			/// <param name="type">Compared type.</param>
			/// <returns>Type descriptor.</returns>
			public static IType IEquatable(IType type) => _iequatableT.WithTypeArguments(type);

			/// <summary>
			/// <see cref="IEquatable{T}.Equals(T)"/> method reference.
			/// </summary>
			public static CodeIdentifier IEquatable_Equals  { get; } = new CodeIdentifier(nameof(IEquatable<int>.Equals), true);

			/// <summary>
			/// <see cref="IEquatable{T}.Equals(T)"/> parameter name.
			/// </summary>
			public static CodeIdentifier IEquatable_Equals_Parameter { get; } = new CodeIdentifier("other", true);

			/// <summary>
			/// <see cref="object.GetHashCode()"/> method reference.
			/// </summary>
			public static CodeIdentifier Object_GetHashCode { get; } = new CodeIdentifier(nameof(global::System.Object.GetHashCode), true);

			/// <summary>
			/// <see cref="object.Equals(object)"/> method reference.
			/// </summary>
			public static CodeIdentifier Object_Equals      { get; } = new CodeIdentifier(nameof(global::System.Object.Equals), true);

			/// <summary>
			/// <see cref="object.Equals(object)"/> parameter name.
			/// </summary>
			public static CodeIdentifier Object_Equals_Parameter { get; } = new CodeIdentifier("obj", true);

			public class Reflection
			{
				/// <summary>
				/// <see cref="MethodInfo"/> type descriptor.
				/// </summary>
				public static IType MethodInfo { get; } = Parser.Parse<MethodInfo>();
			}

			public static class Data
			{
				public static class Common
				{
					/// <summary>
					/// <see cref="DbDataReader"/> type descriptor.
					/// </summary>
					public static IType DbDataReader { get; } = Parser.Parse<DbDataReader>();

					/// <summary>
					/// <see cref="DbDataReader.GetValue(int)"/> method reference.
					/// </summary>
					public static CodeIdentifier DbDataReader_GetValue { get; } = new CodeIdentifier(nameof(global::System.Data.Common.DbDataReader.GetValue), true);
				}

				public static class SqlTypes
				{
					/// <summary>
					/// <see cref="SqlBinary"/> type descriptor.
					/// </summary>
					public static IType SqlBinary   { get; } = Parser.Parse<SqlBinary>();
					/// <summary>
					/// <see cref="SqlBoolean"/> type descriptor.
					/// </summary>
					public static IType SqlBoolean  { get; } = Parser.Parse<SqlBoolean>();
					/// <summary>
					/// <see cref="SqlByte"/> type descriptor.
					/// </summary>
					public static IType SqlByte     { get; } = Parser.Parse<SqlByte>();
					/// <summary>
					/// <see cref="SqlDateTime"/> type descriptor.
					/// </summary>
					public static IType SqlDateTime { get; } = Parser.Parse<SqlDateTime>();
					/// <summary>
					/// <see cref="SqlDecimal"/> type descriptor.
					/// </summary>
					public static IType SqlDecimal  { get; } = Parser.Parse<SqlDecimal>();
					/// <summary>
					/// <see cref="SqlDouble"/> type descriptor.
					/// </summary>
					public static IType SqlDouble   { get; } = Parser.Parse<SqlDouble>();
					/// <summary>
					/// <see cref="SqlGuid"/> type descriptor.
					/// </summary>
					public static IType SqlGuid     { get; } = Parser.Parse<SqlGuid>();
					/// <summary>
					/// <see cref="SqlInt16"/> type descriptor.
					/// </summary>
					public static IType SqlInt16    { get; } = Parser.Parse<SqlInt16>();
					/// <summary>
					/// <see cref="SqlInt32"/> type descriptor.
					/// </summary>
					public static IType SqlInt32    { get; } = Parser.Parse<SqlInt32>();
					/// <summary>
					/// <see cref="SqlInt64"/> type descriptor.
					/// </summary>
					public static IType SqlInt64    { get; } = Parser.Parse<SqlInt64>();
					/// <summary>
					/// <see cref="SqlMoney"/> type descriptor.
					/// </summary>
					public static IType SqlMoney    { get; } = Parser.Parse<SqlMoney>();
					/// <summary>
					/// <see cref="SqlSingle"/> type descriptor.
					/// </summary>
					public static IType SqlSingle   { get; } = Parser.Parse<SqlSingle>();
					/// <summary>
					/// <see cref="SqlString"/> type descriptor.
					/// </summary>
					public static IType SqlString   { get; } = Parser.Parse<SqlString>();
					/// <summary>
					/// <see cref="SqlXml"/> type descriptor.
					/// </summary>
					public static IType SqlXml      { get; } = Parser.Parse<SqlXml>();
				}
			}

			public static class Linq
			{
				private static readonly IType _iqueryableT = Parser.Parse(typeof(IQueryable<>));

				/// <summary>
				/// <see cref="Enumerable"/> type descriptor.
				/// </summary>
				public static IType Enumerable { get; } = Parser.Parse(typeof(Enumerable));
				/// <summary>
				/// <see cref="Queryable"/> type descriptor.
				/// </summary>
				public static IType Queryable  { get; } = Parser.Parse(typeof(Queryable));

				/// <summary>
				/// <see cref="Enumerable.ToList{TSource}(IEnumerable{TSource})"/> method reference.
				/// </summary>
				public static CodeIdentifier Enumerable_ToList { get; } = new CodeIdentifier(nameof(global::System.Linq.Enumerable.ToList), true);
				/// <summary>
				/// <see cref="Queryable.First{TSource}(IQueryable{TSource})"/> method reference.
				/// </summary>
				public static CodeIdentifier Queryable_First { get; } = new CodeIdentifier(nameof(global::System.Linq.Queryable.First), true);
				/// <summary>
				/// <see cref="Queryable.FirstOrDefault{TSource}(IQueryable{TSource})"/> method reference.
				/// </summary>
				public static CodeIdentifier Queryable_FirstOrDefault { get; } = new CodeIdentifier(nameof(global::System.Linq.Queryable.FirstOrDefault), true);
				/// <summary>
				/// <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> method reference.
				/// </summary>
				public static CodeIdentifier Queryable_Where { get; } = new CodeIdentifier(nameof(global::System.Linq.Queryable.Where), true);

				/// <summary>
				/// Returns <see cref="IQueryable{T}"/> type descriptor.
				/// </summary>
				/// <param name="elementType">Element type.</param>
				/// <returns>Type descriptor.</returns>
				public static IType IQueryable(IType elementType) => _iqueryableT.WithTypeArguments(elementType);

				public static class Expressions
				{
					private static readonly IType _expressionT = Parser.Parse(typeof(Expression<>));

					/// <summary>
					/// <see cref="LambdaExpression"/> type descriptor.
					/// </summary>
					public static IType LambdaExpression { get; } = Parser.Parse<LambdaExpression>();

					/// <summary>
					/// Returns <see cref="Expression{TDelegate}"/> type descriptor.
					/// </summary>
					/// <param name="expressionType">Expression type.</param>
					/// <returns>Type descriptor.</returns>
					public static IType Expression(IType expressionType) => _expressionT.WithTypeArguments(expressionType);
				}
			}

			public static class Collections
			{
				public static class Generic
				{
					private static readonly IType _ienumerableT       = Parser.Parse(typeof(IEnumerable<>));
					private static readonly IType _listT              = Parser.Parse(typeof(List<>));
					private static readonly IType _iequalityComparerT = Parser.Parse(typeof(IEqualityComparer<>));

					/// <summary>
					/// Returns <see cref="IEnumerable{T}"/> type descriptor.
					/// </summary>
					/// <param name="elementType">Element type.</param>
					/// <returns>Type descriptor.</returns>
					public static IType IEnumerable(IType elementType) => _ienumerableT.WithTypeArguments(elementType);
					/// <summary>
					/// Returns <see cref="List{T}"/> type descriptor.
					/// </summary>
					/// <param name="elementType">Element type.</param>
					/// <returns>Type descriptor.</returns>
					public static IType List(IType elementType) => _listT.WithTypeArguments(elementType);

					/// <summary>
					/// Returns <see cref="IEqualityComparer{T}"/> type descriptor.
					/// </summary>
					/// <param name="type">Compared type.</param>
					/// <returns>Type descriptor.</returns>
					public static IType IEqualityComparer(IType type) => _iequalityComparerT.WithTypeArguments(type);

					/// <summary>
					/// <see cref="IEqualityComparer{T}.GetHashCode(T)"/> method reference.
					/// </summary>
					public static CodeIdentifier IEqualityComparer_GetHashCode { get; } = new CodeIdentifier(nameof(IEqualityComparer<int>.GetHashCode), true);

					/// <summary>
					/// <see cref="IEqualityComparer{T}.Equals(T, T)"/> method reference.
					/// </summary>
					public static CodeIdentifier IEqualityComparer_Equals { get; } = new CodeIdentifier(nameof(IEqualityComparer<int>.Equals), true);
				}
			}

			public static class Threading
			{
				/// <summary>
				/// <see cref="global::System.Threading.CancellationToken"/> type descriptor.
				/// </summary>
				public static IType CancellationToken { get; } = Parser.Parse(typeof(CancellationToken));

				public static class Tasks
				{
					private static readonly IType _taskT = Parser.Parse(typeof(Task<>));

					/// <summary>
					/// Returns <see cref="Task{TResult}"/> type descriptor.
					/// </summary>
					/// <param name="valueType">Value type.</param>
					/// <returns>Type descriptor.</returns>
					public static IType Task(IType valueType) => _taskT.WithTypeArguments(valueType);
				}
			}
		}

		public static class Microsoft
		{
			public static class SqlServer
			{
				public static class Types
				{
					/// <summary>
					/// Microsoft.SqlServer.Types.SqlHierarchyId type descriptor.
					/// </summary>
					public static IType SqlHierarchyId { get; } = Parser.Parse("Microsoft.SqlServer.Types.SqlHierarchyId", true);
				}
			}
		}

		public static class LinqToDB
		{
			/// <summary>
			/// <see cref="ITable{T}"/> open generic type descriptor.
			/// </summary>
			public static IType ITableT                   { get; } = Parser.Parse(typeof(ITable<>));
			/// <summary>
			/// <see cref="Sql.FunctionAttribute"/> type descriptor.
			/// </summary>
			public static IType SqlFunctionAttribute      { get; } = Parser.Parse<Sql.FunctionAttribute>();
			/// <summary>
			/// <see cref="Sql.TableFunctionAttribute"/> type descriptor.
			/// </summary>
			public static IType SqlTableFunctionAttribute { get; } = Parser.Parse<Sql.TableFunctionAttribute>();
			/// <summary>
			/// <see cref="DataType"/> type descriptor.
			/// </summary>
			public static IType DataType                  { get; } = Parser.Parse<DataType>();
			/// <summary>
			/// <see cref="IDataContext"/> type descriptor.
			/// </summary>
			public static IType IDataContext              { get; } = Parser.Parse<IDataContext>();
			/// <summary>
			/// <see cref="DataExtensions"/> type descriptor.
			/// </summary>
			public static IType DataExtensions            { get; } = Parser.Parse(typeof(DataExtensions));

			/// <summary>
			/// <see cref="global::LinqToDB.AsyncExtensions"/> type descriptor.
			/// </summary>
			public static IType AsyncExtensions           { get; } = Parser.Parse(typeof(AsyncExtensions));

			/// <summary>
			/// <see cref="IDataContext.MappingSchema"/> property reference.
			/// </summary>
			public static CodeReference IDataContext_MappingSchema { get; } = PropertyOrField((IDataContext ctx) => ctx.MappingSchema, false);

			/// <summary>
			/// DataExtensions.GetTable method reference.
			/// </summary>
			public static CodeIdentifier DataExtensions_GetTable { get; } = new CodeIdentifier(nameof(global::LinqToDB.DataExtensions.GetTable), true);

			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.Configuration"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_Configuration    { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.Configuration), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.ServerSideOnly"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_ServerSideOnly   { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.ServerSideOnly), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.PreferServerSide"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_PreferServerSide { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.PreferServerSide), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.InlineParameters"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_InlineParameters { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.InlineParameters), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.IsPredicate"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_IsPredicate      { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.IsPredicate), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.IsAggregate"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_IsAggregate      { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.IsAggregate), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.IsWindowFunction"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_IsWindowFunction { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.IsWindowFunction), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.IsPure"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_IsPure           { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.IsPure), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.CanBeNull"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_CanBeNull        { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.CanBeNull), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.IsNullable"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_IsNullable       { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.IsNullable), true);
			/// <summary>
			/// <see cref="Sql.ExpressionAttribute.ArgIndices"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_ExpressionAttribute_ArgIndices       { get; } = new CodeIdentifier(nameof(Sql.ExpressionAttribute.ArgIndices), true);

			/// <summary>
			/// <see cref="Sql.TableFunctionAttribute.Schema"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_TableFunctionAttribute_Schema        { get; } = new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Schema), true);
			/// <summary>
			/// <see cref="Sql.TableFunctionAttribute.Database"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_TableFunctionAttribute_Database      { get; } = new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Database), true);
			/// <summary>
			/// <see cref="Sql.TableFunctionAttribute.Server"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_TableFunctionAttribute_Server        { get; } = new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Server), true);
			/// <summary>
			/// <see cref="Sql.TableFunctionAttribute.Package"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_TableFunctionAttribute_Package       { get; } = new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Package), true);
			/// <summary>
			/// <see cref="Sql.TableFunctionAttribute.Configuration"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_TableFunctionAttribute_Configuration { get; } = new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Configuration), true);
			/// <summary>
			/// <see cref="Sql.TableFunctionAttribute.ArgIndices"/> property reference.
			/// </summary>
			public static CodeIdentifier Sql_TableFunctionAttribute_ArgIndices    { get; } = new CodeIdentifier(nameof(Sql.TableFunctionAttribute.ArgIndices), true);

			/// <summary>
			/// <see cref="AsyncExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/> method reference.
			/// </summary>
			public static CodeIdentifier AsyncExtensions_FirstOrDefaultAsync { get; } = new CodeIdentifier(nameof(global::LinqToDB.AsyncExtensions.FirstOrDefaultAsync), true);
			/// <summary>
			/// <see cref="AsyncExtensions.ToListAsync{TSource}(IQueryable{TSource}, CancellationToken)"/> method reference.
			/// </summary>
			public static CodeIdentifier AsyncExtensions_ToListAsync { get; } = new CodeIdentifier(nameof(global::LinqToDB.AsyncExtensions.ToListAsync), true);

			/// <summary>
			/// Returns <see cref="ITable{T}"/> type descriptor.
			/// </summary>
			/// <param name="tableType">Record type.</param>
			/// <returns>Type descriptor.</returns>
			public static IType ITable(IType tableType) => ITableT.WithTypeArguments(tableType);

			public static class Expressions
			{
				/// <summary>
				/// <see cref="global::LinqToDB.Expressions.MemberHelper"/> type descriptor.
				/// </summary>
				public static IType MemberHelper { get; } = Parser.Parse(typeof(MemberHelper));

				/// <summary>
				/// <see cref="MemberHelper.MethodOf(Expression{Action})"/> method reference.
				/// </summary>
				public static CodeIdentifier MemberHelper_MethodOf { get; } = new CodeIdentifier(nameof(global::LinqToDB.Expressions.MemberHelper.MethodOf), true);
			}

			public static class Common
			{
				/// <summary>
				/// <see cref="global::LinqToDB.Common.Converter"/> type descriptor.
				/// </summary>
				public static IType Converter { get; } = Parser.Parse(typeof(Converter));

				/// <summary>
				/// <see cref="Converter.ChangeTypeTo{T}(object?, MappingSchema?)"/> method reference.
				/// </summary>
				public static CodeIdentifier Converter_ChangeTypeTo { get; } = new CodeIdentifier(nameof(global::LinqToDB.Common.Converter.ChangeTypeTo), true);
			}

			public static class Configuration
			{
				private static readonly IType _linqToDBConnectionOptionsT = Parser.Parse(typeof(LinqToDBConnectionOptions<>));

				/// <summary>
				/// <see cref="global::LinqToDB.Configuration.LinqToDBConnectionOptions"/> type descriptor.
				/// </summary>
				public static IType LinqToDBConnectionOptions { get; } = Parser.Parse<LinqToDBConnectionOptions>();

				/// <summary>
				/// Returns <see cref="LinqToDBConnectionOptions{T}"/> type descriptor.
				/// </summary>
				/// <param name="contextType">Context type.</param>
				/// <returns>Type descriptor.</returns>
				public static IType LinqToDBConnectionOptionsWithType(IType contextType) => _linqToDBConnectionOptionsT.WithTypeArguments(contextType);
			}

			public static class Mapping
			{
				/// <summary>
				/// <see cref="global::LinqToDB.Mapping.AssociationAttribute"/> type descriptor.
				/// </summary>
				public static IType AssociationAttribute { get; } = Parser.Parse<AssociationAttribute>();
				/// <summary>
				/// <see cref="global::LinqToDB.Mapping.NotColumnAttribute"/> type descriptor.
				/// </summary>
				public static IType NotColumnAttribute   { get; } = Parser.Parse<NotColumnAttribute>();
				/// <summary>
				/// <see cref="global::LinqToDB.Mapping.ColumnAttribute"/> type descriptor.
				/// </summary>
				public static IType ColumnAttribute      { get; } = Parser.Parse<ColumnAttribute>();
				/// <summary>
				/// <see cref="global::LinqToDB.Mapping.TableAttribute"/> type descriptor.
				/// </summary>
				public static IType TableAttribute       { get; } = Parser.Parse<TableAttribute>();
				/// <summary>
				/// <see cref="global::LinqToDB.Mapping.MappingSchema"/> type descriptor.
				/// </summary>
				public static IType MappingSchema        { get; } = Parser.Parse<MappingSchema>();

				/// <summary>
				/// <see cref="MappingSchema.SetConvertExpression(DbDataType, DbDataType, LambdaExpression, bool)"/> method reference.
				/// </summary>
				public static CodeIdentifier MappingSchema_SetConvertExpression { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.MappingSchema.SetConvertExpression), true);

				/// <summary>
				/// <see cref="AssociationAttribute.CanBeNull"/> property reference.
				/// </summary>
				public static CodeIdentifier AssociationAttribute_CanBeNull             { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.AssociationAttribute.CanBeNull), true);
				/// <summary>
				/// <see cref="AssociationAttribute.ExpressionPredicate"/> property reference.
				/// </summary>
				public static CodeIdentifier AssociationAttribute_ExpressionPredicate   { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.AssociationAttribute.ExpressionPredicate), true);
				/// <summary>
				/// <see cref="AssociationAttribute.QueryExpressionMethod"/> property reference.
				/// </summary>
				public static CodeIdentifier AssociationAttribute_QueryExpressionMethod { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.AssociationAttribute.QueryExpressionMethod), true);
				/// <summary>
				/// <see cref="AssociationAttribute.ThisKey"/> property reference.
				/// </summary>
				public static CodeIdentifier AssociationAttribute_ThisKey               { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.AssociationAttribute.ThisKey), true);
				/// <summary>
				/// <see cref="AssociationAttribute.OtherKey"/> property reference.
				/// </summary>
				public static CodeIdentifier AssociationAttribute_OtherKey              { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.AssociationAttribute.OtherKey), true);
				/// <summary>
				/// <see cref="AssociationAttribute.Configuration"/> property reference.
				/// </summary>
				public static CodeIdentifier AssociationAttribute_Configuration         { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.AssociationAttribute.Configuration), true);
				/// <summary>
				/// <see cref="AssociationAttribute.AliasName"/> property reference.
				/// </summary>
				public static CodeIdentifier AssociationAttribute_AliasName             { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.AssociationAttribute.AliasName), true);
				/// <summary>
				/// <see cref="AssociationAttribute.Storage"/> property reference.
				/// </summary>
				public static CodeIdentifier AssociationAttribute_Storage               { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.AssociationAttribute.Storage), true);
				/// <summary>
				/// <see cref="TableAttribute.Schema"/> property reference.
				/// </summary>
				public static CodeIdentifier TableAttribute_Schema                    { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.TableAttribute.Schema), true);
				/// <summary>
				/// <see cref="TableAttribute.Database"/> property reference.
				/// </summary>
				public static CodeIdentifier TableAttribute_Database                  { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.TableAttribute.Database), true);
				/// <summary>
				/// <see cref="TableAttribute.Server"/> property reference.
				/// </summary>
				public static CodeIdentifier TableAttribute_Server                    { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.TableAttribute.Server), true);
				/// <summary>
				/// <see cref="TableAttribute.IsView"/> property reference.
				/// </summary>
				public static CodeIdentifier TableAttribute_IsView                    { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.TableAttribute.IsView), true);
				/// <summary>
				/// <see cref="TableAttribute.Configuration"/> property reference.
				/// </summary>
				public static CodeIdentifier TableAttribute_Configuration             { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.TableAttribute.Configuration), true);
				/// <summary>
				/// <see cref="TableAttribute.IsColumnAttributeRequired"/> property reference.
				/// </summary>
				public static CodeIdentifier TableAttribute_IsColumnAttributeRequired { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.TableAttribute.IsColumnAttributeRequired), true);
				/// <summary>
				/// <see cref="TableAttribute.IsTemporary"/> property reference.
				/// </summary>
				public static CodeIdentifier TableAttribute_IsTemporary               { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.TableAttribute.IsTemporary), true);
				/// <summary>
				/// <see cref="TableAttribute.TableOptions"/> property reference.
				/// </summary>
				public static CodeIdentifier TableAttribute_TableOptions              { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.TableAttribute.TableOptions), true);

				/// <summary>
				/// <see cref="ColumnAttribute.CanBeNull"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_CanBeNull         { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.CanBeNull), true);
				/// <summary>
				/// <see cref="ColumnAttribute.Configuration"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_Configuration     { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.Configuration), true);
				/// <summary>
				/// <see cref="ColumnAttribute.DataType"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_DataType          { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.DataType), true);
				/// <summary>
				/// <see cref="ColumnAttribute.DbType"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_DbType            { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.DbType), true);
				/// <summary>
				/// <see cref="ColumnAttribute.Length"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_Length            { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.Length), true);
				/// <summary>
				/// <see cref="ColumnAttribute.Precision"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_Precision         { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.Precision), true);
				/// <summary>
				/// <see cref="ColumnAttribute.Scale"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_Scale             { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.Scale), true);
				/// <summary>
				/// <see cref="ColumnAttribute.IsPrimaryKey"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_IsPrimaryKey      { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.IsPrimaryKey), true);
				/// <summary>
				/// <see cref="ColumnAttribute.PrimaryKeyOrder"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_PrimaryKeyOrder   { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.PrimaryKeyOrder), true);
				/// <summary>
				/// <see cref="ColumnAttribute.IsIdentity"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_IsIdentity        { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.IsIdentity), true);
				/// <summary>
				/// <see cref="ColumnAttribute.SkipOnInsert"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_SkipOnInsert      { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.SkipOnInsert), true);
				/// <summary>
				/// <see cref="ColumnAttribute.SkipOnUpdate"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_SkipOnUpdate      { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.SkipOnUpdate), true);
				/// <summary>
				/// <see cref="ColumnAttribute.SkipOnEntityFetch"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_SkipOnEntityFetch { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.SkipOnEntityFetch), true);
				/// <summary>
				/// <see cref="ColumnAttribute.MemberName"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_MemberName        { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.MemberName), true);
				/// <summary>
				/// <see cref="ColumnAttribute.Storage"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_Storage           { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.Storage), true);
				/// <summary>
				/// <see cref="ColumnAttribute.CreateFormat"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_CreateFormat      { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.CreateFormat), true);
				/// <summary>
				/// <see cref="ColumnAttribute.IsDiscriminator"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_IsDiscriminator   { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.IsDiscriminator), true);
				/// <summary>
				/// <see cref="ColumnAttribute.Order"/> property reference.
				/// </summary>
				public static CodeIdentifier ColumnAttribute_Order             { get; } = new CodeIdentifier(nameof(global::LinqToDB.Mapping.ColumnAttribute.Order), true);
			}

			public static class Data
			{
				/// <summary>
				/// <see cref="global::LinqToDB.Data.DataParameter"/> type descriptor.
				/// </summary>
				public static IType DataParameter            { get; } = Parser.Parse<DataParameter>();
				/// <summary>
				/// <see cref="global::LinqToDB.Data.DataParameter"/>[] type descriptor.
				/// </summary>
				public static IType DataParameterArray       { get; } = new ArrayType(DataParameter, new int?[] { null }, false);
				/// <summary>
				/// <see cref="global::LinqToDB.Data.DataConnectionExtensions"/> type descriptor.
				/// </summary>
				public static IType DataConnectionExtensions { get; } = Parser.Parse(typeof(DataConnectionExtensions));
				/// <summary>
				/// <see cref="global::LinqToDB.Data.DataConnection"/> type descriptor.
				/// </summary>
				public static IType DataConnection           { get; } = Parser.Parse<DataConnection>();

				/// <summary>
				/// DataConnectionExtensions.ExecuteProc method reference.
				/// </summary>
				public static CodeIdentifier DataConnectionExtensions_ExecuteProc      { get; } = new CodeIdentifier(nameof(global::LinqToDB.Data.DataConnectionExtensions.ExecuteProc), true);
				/// <summary>
				/// DataConnectionExtensions.ExecuteProcAsync method reference.
				/// </summary>
				public static CodeIdentifier DataConnectionExtensions_ExecuteProcAsync { get; } = new CodeIdentifier(nameof(global::LinqToDB.Data.DataConnectionExtensions.ExecuteProcAsync), true);
				/// <summary>
				/// DataConnectionExtensions.QueryProc method reference.
				/// </summary>
				public static CodeIdentifier DataConnectionExtensions_QueryProc        { get; } = new CodeIdentifier(nameof(global::LinqToDB.Data.DataConnectionExtensions.QueryProc), true);
				/// <summary>
				/// DataConnectionExtensions.QueryProcAsync method reference.
				/// </summary>
				public static CodeIdentifier DataConnectionExtensions_QueryProcAsync   { get; } = new CodeIdentifier(nameof(global::LinqToDB.Data.DataConnectionExtensions.QueryProcAsync), true);

				/// <summary>
				/// <see cref="DataConnection.CommandTimeout"/> property reference.
				/// </summary>
				public static CodeReference DataConnection_CommandTimeout { get; } = PropertyOrField((DataConnection dc) => dc.CommandTimeout, false);

				/// <summary>
				/// <see cref="DataParameter.Direction"/> property reference.
				/// </summary>
				public static CodeReference DataParameter_Direction { get; } = PropertyOrField((DataParameter dp) => dp.Direction, false);
				/// <summary>
				/// <see cref="DataParameter.DbType"/> property reference.
				/// </summary>
				public static CodeReference DataParameter_DbType    { get; } = PropertyOrField((DataParameter dp) => dp.DbType, true);
				/// <summary>
				/// <see cref="DataParameter.Size"/> property reference.
				/// </summary>
				public static CodeReference DataParameter_Size      { get; } = PropertyOrField((DataParameter dp) => dp.Size, false);
				/// <summary>
				/// <see cref="DataParameter.Precision"/> property reference.
				/// </summary>
				public static CodeReference DataParameter_Precision { get; } = PropertyOrField((DataParameter dp) => dp.Precision, false);
				/// <summary>
				/// <see cref="DataParameter.Scale"/> property reference.
				/// </summary>
				public static CodeReference DataParameter_Scale     { get; } = PropertyOrField((DataParameter dp) => dp.Scale, false);
				/// <summary>
				/// <see cref="DataParameter.Value"/> property reference.
				/// </summary>
				public static CodeReference DataParameter_Value     { get; } = PropertyOrField((DataParameter dp) => dp.Value, true);
			}

			public static class Tools
			{
				public static class Comparers
				{
					/// <summary>
					/// <see cref="global::LinqToDB.Tools.Comparers.ComparerBuilder"/> type descriptor.
					/// </summary>
					public static IType ComparerBuilder { get; } = Parser.Parse(typeof(ComparerBuilder));
					/// <summary>
					/// <see cref="ComparerBuilder.GetEqualityComparer{T}(Expression{Func{T, object?}}[])"/> method reference.
					/// </summary>
					public static CodeIdentifier ComparerBuilder_GetEqualityComparer { get; } = new CodeIdentifier(nameof(global::LinqToDB.Tools.Comparers.ComparerBuilder.GetEqualityComparer), true);
				}
			}
		}

		// TODO: replace forceNullable with NRT annotation lookup
		public static CodeReference PropertyOrField<TObject, TProperty>(Expression<Func<TObject, TProperty>> accessor, bool forceNullable)
		{
			var member = ((MemberExpression)accessor.Body).Member;
			if (member is PropertyInfo pi)
				return new CodeExternalPropertyOrField(new CodeIdentifier(member.Name, true), new(Parser.Parse(pi.PropertyType).WithNullability(forceNullable))).Reference;
			if (member is FieldInfo fi)
				return new CodeExternalPropertyOrField(new CodeIdentifier(member.Name, true), new(Parser.Parse(fi.FieldType).WithNullability(forceNullable))).Reference;

			throw new InvalidOperationException();
		}
	}
}
