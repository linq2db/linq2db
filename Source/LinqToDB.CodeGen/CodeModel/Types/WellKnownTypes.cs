using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.CodeGen.Model
{
	// use static constructors to ensure proper initialization order for fields
	/// <summary>
	/// This class contains pre-parsed <see cref="IType"/> definitions for some well-known system and Linq To DB types.
	/// </summary>
	public static class WellKnownTypes
	{
		// use C# type parser for known types parsing (it doesn't affect parsing of types in this file)
		private static ITypeParser Parser => CSharpLanguageProvider.Instance.TypeParser;

		public static class System
		{
			public static IType Boolean { get; }
			public static IType String { get; }
			public static IType Object { get; }
			public static IType Int32 { get; }
			public static IType Int64 { get; }

			public static IType ObjectNullable { get; }
			public static IType ObjectArrayNullable { get; }

			public static IType InvalidOperationException { get; }

			//private static IType Func0 { get; }
			private static IType Func1 { get; }
			private static IType Func2 { get; }

			static System()
			{
				Boolean = Parser.Parse<bool>();
				String = Parser.Parse<string>();
				Object = Parser.Parse<object>();
				Int32 = Parser.Parse<int>();
				Int64 = Parser.Parse<long>();

				ObjectNullable = Object.WithNullability(true);
				ObjectArrayNullable = new ArrayType(Object, new int?[] { null }, true);

				InvalidOperationException = Parser.Parse<InvalidOperationException>();


				Func1 = Parser.Parse(typeof(Func<,>));
				Func2 = Parser.Parse(typeof(Func<,,>));

				//	Func0 = new OpenGenericType(SystemNamespace, new CodeIdentifier(nameof(Func<int>)), false, false, 1, true);

			}

			//public static IType Func(IType returnType           ) => Func0.WithTypeArguments(new[] { returnType      });
			public static IType Func(IType returnType, IType arg0) => Func1.WithTypeArguments(arg0, returnType);
			public static IType Func(IType returnType, IType arg0, IType arg1) => Func2.WithTypeArguments(arg0, arg1, returnType);

			public class Reflection
			{
				public static IType MethodInfo { get; }

				static Reflection()
				{
					MethodInfo = Parser.Parse<MethodInfo>();
				}
			}

			public static class Data
			{
				public static class Common
				{
					public static IType DbDataReader { get; }

					static Common()
					{
						DbDataReader = Parser.Parse<DbDataReader>();
					}
				}

				public static class SqlTypes
				{
					public static IType SqlBinary { get; }
					public static IType SqlBoolean { get; }
					public static IType SqlByte { get; }
					public static IType SqlDateTime { get; }
					public static IType SqlDecimal { get; }
					public static IType SqlDouble { get; }
					public static IType SqlGuid { get; }
					public static IType SqlInt16 { get; }
					public static IType SqlInt32 { get; }
					public static IType SqlInt64 { get; }
					public static IType SqlMoney { get; }
					public static IType SqlSingle { get; }
					public static IType SqlString { get; }
					public static IType SqlXml { get; }

					static SqlTypes()
					{
						SqlBinary = Parser.Parse<SqlBinary>();
						SqlBoolean = Parser.Parse<SqlBoolean>();
						SqlByte = Parser.Parse<SqlByte>();
						SqlDateTime = Parser.Parse<SqlDateTime>();
						SqlDecimal = Parser.Parse<SqlDecimal>();
						SqlDouble = Parser.Parse<SqlDouble>();
						SqlGuid = Parser.Parse<SqlGuid>();
						SqlInt16 = Parser.Parse<SqlInt16>();
						SqlInt32 = Parser.Parse<SqlInt32>();
						SqlInt64 = Parser.Parse<SqlInt64>();
						SqlMoney = Parser.Parse<SqlMoney>();
						SqlSingle = Parser.Parse<SqlSingle>();
						SqlString = Parser.Parse<SqlString>();
						SqlXml = Parser.Parse<SqlXml>();
					}
				}
			}

			public static class Linq
			{
				private static IType IQueryableT { get; }

				public static IType Enumerable { get; }
				public static IType Queryable { get; }

				public static CodeIdentifier Queryable_First { get; }
				public static CodeIdentifier Queryable_FirstOrDefault { get; }
				public static CodeIdentifier Queryable_Where { get; }

				static Linq()
				{
					IQueryableT = Parser.Parse(typeof(IQueryable<>));
					Enumerable = Parser.Parse(typeof(Enumerable));
					Queryable = Parser.Parse(typeof(Queryable));

					Queryable_First = new CodeIdentifier(nameof(global::System.Linq.Queryable.First));
					Queryable_FirstOrDefault = new CodeIdentifier(nameof(global::System.Linq.Queryable.FirstOrDefault));
					Queryable_Where = new CodeIdentifier(nameof(global::System.Linq.Queryable.Where));
				}

				public static IType IQueryable(IType elementType) => IQueryableT.WithTypeArguments(elementType);

				public static class Expressions
				{
					public static IType LambdaExpression { get; }

					private static IType ExpressionT { get; }

					static Expressions()
					{
						ExpressionT = Parser.Parse(typeof(Expression<>));
						LambdaExpression = Parser.Parse<LambdaExpression>();

					}

					public static IType Expression(IType expressionType) => ExpressionT.WithTypeArguments(expressionType);
				}
			}

			public static class Collections
			{
				public static class Generic
				{
					private static IType IEnumerableT { get; }
					private static IType ListT { get; }

					static Generic()
					{
						IEnumerableT = Parser.Parse(typeof(IEnumerable<>));
						ListT = Parser.Parse(typeof(List<>));
					}

					public static IType IEnumerable(IType elementType) => IEnumerableT.WithTypeArguments(elementType);
					public static IType List(IType elementType) => ListT.WithTypeArguments(elementType);
				}
			}
		}

		public static class Microsoft
		{
			public static class SqlServer
			{
				public static class Types
				{
					public static IType SqlHierarchyId { get; }

					static Types()
					{
						SqlHierarchyId = Parser.Parse("Microsoft.SqlServer.Types.SqlHierarchyId", true);
					}
				}
			}
		}

		public static class LinqToDB
		{
			public static IType ITableT { get; }
			public static IType SqlFunctionAttribute { get; }
			public static IType SqlTableFunctionAttribute { get; }
			public static IType DataType { get; }
			public static IType IDataContext { get; }
			public static IType DataExtensions { get; }

			public static CodeReference IDataContext_MappingSchema { get; }

			public static CodeIdentifier DataExtensions_GetTable { get; }

			static LinqToDB()
			{
				ITableT = Parser.Parse(typeof(ITable<>));
				SqlFunctionAttribute = Parser.Parse<Sql.FunctionAttribute>();
				SqlTableFunctionAttribute = Parser.Parse<Sql.TableFunctionAttribute>();
				DataType = Parser.Parse<DataType>();
				IDataContext = Parser.Parse<IDataContext>();
				DataExtensions = Parser.Parse(typeof(DataExtensions));

				IDataContext_MappingSchema = PropertyOrField((IDataContext ctx) => ctx.MappingSchema, false);

				DataExtensions_GetTable = new CodeIdentifier(nameof(global::LinqToDB.DataExtensions.GetTable));
			}

			public static IType ITable(IType tableType) => ITableT.WithTypeArguments(tableType);

			public static class Expressions
			{
				public static IType MemberHelper { get; }
				static Expressions()
				{
					MemberHelper = Parser.Parse(typeof(MemberHelper));
				}
			}

			public static class Common
			{
				public static IType Converter { get; }
				static Common()
				{
					Converter = Parser.Parse(typeof(Converter));
				}
			}

			public static class Configuration
			{
				public static IType LinqToDbConnectionOptions { get; }
				private static IType LinqToDbConnectionOptionsT { get; }

				static Configuration()
				{
					LinqToDbConnectionOptions = Parser.Parse<LinqToDbConnectionOptions>();
					LinqToDbConnectionOptionsT = Parser.Parse(typeof(LinqToDbConnectionOptions<>));
				}

				public static IType LinqToDbConnectionOptionsWithType(IType contextType) => LinqToDbConnectionOptionsT.WithTypeArguments(contextType);

			}

			public static class Mapping
			{
				public static IType AssociationAttribute { get; }
				public static IType NotColumnAttribute { get; }
				public static IType ColumnAttribute { get; }
				public static IType TableAttribute { get; }
				public static IType MappingSchema { get; }

				public static CodeIdentifier MappingSchema_SetConvertExpression { get; }

				static Mapping()
				{
					AssociationAttribute = Parser.Parse<AssociationAttribute>();
					NotColumnAttribute = Parser.Parse<NotColumnAttribute>();
					ColumnAttribute = Parser.Parse<ColumnAttribute>();
					TableAttribute = Parser.Parse<TableAttribute>();
					MappingSchema = Parser.Parse<MappingSchema>();

					MappingSchema_SetConvertExpression = new CodeIdentifier(nameof(global::LinqToDB.Mapping.MappingSchema.SetConvertExpression));
				}
			}

			public static class Data
			{
				public static IType DataParameter { get; }
				public static IType DataParameterArray { get; }
				public static IType DataConnectionExtensions { get; }
				public static IType DataConnection { get; }

				public static CodeIdentifier DataConnection_GetTable { get; }

				public static CodeReference DataParameter_Direction { get; }
				public static CodeReference DataParameter_DbType { get; }
				public static CodeReference DataParameter_Size { get; }
				public static CodeReference DataParameter_Precision { get; }
				public static CodeReference DataParameter_Scale { get; }
				public static CodeReference DataParameter_Value { get; }

				static Data()
				{
					DataParameter = Parser.Parse<DataParameter>();
					DataParameterArray = new ArrayType(DataParameter, new int?[] { null }, false);
					DataConnectionExtensions = Parser.Parse(typeof(DataConnectionExtensions));
					DataConnection = Parser.Parse<DataConnection>();

					DataParameter_Direction = PropertyOrField((DataParameter dp) => dp.Direction, false);
					DataParameter_DbType = PropertyOrField((DataParameter dp) => dp.DbType, true);
					DataParameter_Size = PropertyOrField((DataParameter dp) => dp.Size, false);
					DataParameter_Precision = PropertyOrField((DataParameter dp) => dp.Precision, false);
					DataParameter_Scale = PropertyOrField((DataParameter dp) => dp.Scale, false);
					DataParameter_Value = PropertyOrField((DataParameter dp) => dp.Value, true);

					DataConnection_GetTable = new CodeIdentifier(nameof(global::LinqToDB.Data.DataConnection.GetTable));
				}
			}
		}

		// TODO: replace forceNullable with NRT annotation lookup
		private static CodeReference PropertyOrField<TObject, TProperty>(Expression<Func<TObject, TProperty>> accessor, bool forceNullable)
		{
			var member = ((MemberExpression)accessor.Body).Member;
			if (member is PropertyInfo pi)
				return new CodeExternalPropertyOrField(new CodeIdentifier(member.Name), new(Parser.Parse(pi.PropertyType).WithNullability(forceNullable))).Reference;
			if (member is FieldInfo fi)
				return new CodeExternalPropertyOrField(new CodeIdentifier(member.Name), new(Parser.Parse(fi.FieldType).WithNullability(forceNullable))).Reference;

			throw new InvalidOperationException();
		}
	}
}
