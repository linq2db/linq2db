using System;
using System.Collections.Generic;
using SDST  = System.Data.SqlTypes;
using SD   = System.Data;
using SDC   = System.Data.Common;
using L2DBD = LinqToDB.Data;
using SLE = System.Linq.Expressions;
using System.Linq;
using System.Reflection;

namespace LinqToDB.CodeGen.Model
{
	// use static constructors to ensure proper initialization order for fields
	/// <summary>
	/// This class contains pre-parsed <see cref="IType"/> definitions for some well-known system and Linq To DB types.
	/// </summary>
	public static class WellKnownTypes
	{
		private static IReadOnlyList<CodeIdentifier> SystemNamespace { get; }
		private static IReadOnlyList<CodeIdentifier> SystemReflectionNamespace { get; }
		private static IReadOnlyList<CodeIdentifier> SystemLinqNamespace { get; }
		private static IReadOnlyList<CodeIdentifier> SystemCollectionsGenericNamespace { get; }
		private static IReadOnlyList<CodeIdentifier> SystemLinqExpressionsNamespace { get; }

		internal static Dictionary<Type, IType> TypeCache { get; }

		public static IType Boolean { get; }
		public static IType String  { get; }
		public static IType Object  { get; }
		public static IType Int32   { get; }

		public static IType MethodInfo { get; }

		public static IType Func0 { get; }
		public static IType Func1 { get; }
		public static IType Func2 { get; }

		public static IType IEnumerableT { get; }
		public static IType ListT        { get; }

		public static IType LambdaExpression { get; }
		public static IType ExpressionT { get; }

		public static IType IQueryableT { get; }

		static WellKnownTypes()
		{
			TypeCache = new();

			SystemNamespace = new[]
			{
				new CodeIdentifier(nameof(System))
			};

			SystemReflectionNamespace = new[]
			{
				SystemNamespace[0],
				new CodeIdentifier(nameof(System.Reflection))
			};

			Boolean = AddType(typeof(Boolean), new RegularType(SystemNamespace, new CodeIdentifier(nameof(Boolean)), "bool"  , true , false, true));
			String  = AddType(typeof(String) , new RegularType(SystemNamespace, new CodeIdentifier(nameof(String )), "string", false, false, true));
			Object  = AddType(typeof(Object) , new RegularType(SystemNamespace, new CodeIdentifier(nameof(Object )), "object", false, false, true));
			Int32   = AddType(typeof(Int32)  , new RegularType(SystemNamespace, new CodeIdentifier(nameof(Int32  )), "int"   , true , false, true));

			MethodInfo = AddType(typeof(MethodInfo)  , new RegularType(SystemReflectionNamespace, new CodeIdentifier(nameof(MethodInfo)), null   , false, false, true));

			Func0 = new OpenGenericType(SystemNamespace, new CodeIdentifier(nameof(Func<int>)), false, false, 1, true);
			Func1 = new OpenGenericType(SystemNamespace, new CodeIdentifier(nameof(Func<int>)), false, false, 2, true);
			Func2 = new OpenGenericType(SystemNamespace, new CodeIdentifier(nameof(Func<int>)), false, false, 3, true);

			SystemCollectionsGenericNamespace = new[]
			{
				SystemNamespace[0],
				new CodeIdentifier(nameof(System.Collections)),
				new CodeIdentifier(nameof(System.Collections.Generic)),
			};

			IEnumerableT = new OpenGenericType(SystemCollectionsGenericNamespace, new CodeIdentifier(nameof(IEnumerable<int>)), false, false, 1, true);
			ListT = new OpenGenericType(SystemCollectionsGenericNamespace, new CodeIdentifier(nameof(List<int>)), false, false, 1, true);

			SystemLinqExpressionsNamespace = new[]
			{
				SystemNamespace[0],
				new CodeIdentifier(nameof(System.Linq)),
				new CodeIdentifier(nameof(System.Linq.Expressions)),
			};

			LambdaExpression = AddType(typeof(SLE.LambdaExpression) , new RegularType(SystemLinqExpressionsNamespace, new CodeIdentifier(nameof(SLE.LambdaExpression)), null, false, false, true));
			ExpressionT = new OpenGenericType(SystemLinqExpressionsNamespace, new CodeIdentifier(nameof(SLE.Expression<int>)), false, false, 1, true);

			SystemLinqNamespace = new[]
			{
				SystemNamespace[0],
				new CodeIdentifier(nameof(System.Linq))
			};

			IQueryableT = new OpenGenericType(SystemLinqNamespace, new CodeIdentifier(nameof(IQueryable<int>)), false, false, 1, true);
		}

		public static IType Queryable(IType elementType) => IQueryableT.WithTypeArguments(new[] { elementType });

		public static IType Enumerable(IType elementType) => IEnumerableT.WithTypeArguments(new[] { elementType });
		public static IType List(IType elementType) => ListT.WithTypeArguments(new[] { elementType });

		public static IType Expression(IType expressionType) => ExpressionT.WithTypeArguments(new[] { expressionType });

		public static IType Func(IType returnType           ) => Func0.WithTypeArguments(new[] { returnType      });
		public static IType Func(IType returnType, IType arg0) => Func1.WithTypeArguments(new[] { arg0, returnType });
		public static IType Func(IType returnType, IType arg0, IType arg1) => Func2.WithTypeArguments(new[] { arg0, arg1, returnType });

		private static IType AddType(Type type, IType itype)
		{
			TypeCache.Add(type, itype);
			return itype;
		}

		public static class AdoNet
		{
			private static IReadOnlyList<CodeIdentifier> SystemData { get; }
			private static IReadOnlyList<CodeIdentifier> SystemDataCommon { get; }

			public static IType DbDataReader { get; }
			public static IType ParameterDirection { get; }

			static AdoNet()
			{
				SystemData = new[]
				{
					SystemNamespace[0],
					new CodeIdentifier(nameof(System.Data)),
					new CodeIdentifier(nameof(SD.Common))
				};

				SystemDataCommon = new[]
				{
					SystemNamespace[0],
					new CodeIdentifier(nameof(System.Data)),
					new CodeIdentifier(nameof(SD.Common))
				};

				ParameterDirection = AddType(typeof(SDC.DbParameter), new RegularType(SystemData, new CodeIdentifier(nameof(SDC.DbParameter)), null, true, false, true));
			
				DbDataReader = AddType(typeof(SDC.DbDataReader), new RegularType(SystemDataCommon, new CodeIdentifier(nameof(SDC.DbDataReader)), null, false, false, true));
			}
		}

		public static class LinqToDB
		{
			private static IReadOnlyList<CodeIdentifier> Namespace { get; }
			public static IReadOnlyList<CodeIdentifier> DataNamespace { get; }


			public static IType ITableT { get; }

			static LinqToDB()
			{
				Namespace = new[]
				{
					new CodeIdentifier("LinqToDB")
				};

				DataNamespace = new[]
				{
					Namespace[0],
					new CodeIdentifier("Data"),
				};

				ITableT = new OpenGenericType(Namespace, new CodeIdentifier(nameof(ITable<string>)), false, false, 1, true);

			}

			public static IType ITable(IType tableType) => ITableT.WithTypeArguments(new[] { tableType });

			public static class DataParameter
			{
				public static IType Type { get; }

				public static CodeExternalPropertyOrField Direction { get; }
				public static CodeExternalPropertyOrField DbType { get; }
				public static CodeExternalPropertyOrField Size { get; }
				public static CodeExternalPropertyOrField Precision { get; }
				public static CodeExternalPropertyOrField Scale { get; }

				static DataParameter()
				{
					Type = AddType(typeof(L2DBD.DataParameter), new RegularType(DataNamespace, new CodeIdentifier(nameof(L2DBD.DataParameter)), null, false, false, true));

					Direction = new CodeExternalPropertyOrField(new CodeIdentifier(nameof(L2DBD.DataParameter.Direction)), new (AdoNet.ParameterDirection.WithNullability(true)));
					DbType = new CodeExternalPropertyOrField(new CodeIdentifier(nameof(L2DBD.DataParameter.DbType)), new (String.WithNullability(true)));
					Size = new CodeExternalPropertyOrField(new CodeIdentifier(nameof(L2DBD.DataParameter.Size)), new (Int32.WithNullability(true)));
					Precision = new CodeExternalPropertyOrField(new CodeIdentifier(nameof(L2DBD.DataParameter.Precision)), new (Int32.WithNullability(true)));
					Scale = new CodeExternalPropertyOrField(new CodeIdentifier(nameof(L2DBD.DataParameter.Scale)), new (Int32.WithNullability(true)));
				}
			}
		}

		public static class SqlServerTypes
		{
			public static IReadOnlyList<CodeIdentifier> Namespace { get; }

			public static IType SqlHierarchyId { get; }

			static SqlServerTypes()
			{
				Namespace = new[]
				{
					new CodeIdentifier("Microsoft"),
					new CodeIdentifier("SqlServer"),
					new CodeIdentifier("Types"),
				};

				SqlHierarchyId = new RegularType(Namespace, new CodeIdentifier("SqlHierarchyId"), null, true, false, true);
			}
		}

		public static class SqlTypes
		{
			public static IReadOnlyList<CodeIdentifier> Namespace { get; }

			public static IType SqlBinary   { get; }
			public static IType SqlBoolean  { get; }
			public static IType SqlByte     { get; }
			public static IType SqlDateTime { get; }
			public static IType SqlDecimal  { get; }
			public static IType SqlDouble   { get; }
			public static IType SqlGuid     { get; }
			public static IType SqlInt16    { get; }
			public static IType SqlInt32    { get; }
			public static IType SqlInt64    { get; }
			public static IType SqlMoney    { get; }
			public static IType SqlSingle   { get; }
			public static IType SqlString   { get; }

			static SqlTypes()
			{
				Namespace = new[]
				{
					SystemNamespace[0],
					new CodeIdentifier(nameof(System.Data)),
					new CodeIdentifier(nameof(SD.SqlTypes)),
				};

				SqlBinary   = AddType(typeof(SDST.SqlBinary)  , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlBinary  )), null, true, false, true));
				SqlBoolean  = AddType(typeof(SDST.SqlBoolean) , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlBoolean )), null, true, false, true));
				SqlByte     = AddType(typeof(SDST.SqlByte)    , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlByte    )), null, true, false, true));
				SqlDateTime = AddType(typeof(SDST.SqlDateTime), new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlDateTime)), null, true, false, true));
				SqlDecimal  = AddType(typeof(SDST.SqlDecimal) , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlDecimal )), null, true, false, true));
				SqlDouble   = AddType(typeof(SDST.SqlDouble)  , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlDouble  )), null, true, false, true));
				SqlGuid     = AddType(typeof(SDST.SqlGuid)    , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlGuid    )), null, true, false, true));
				SqlInt16    = AddType(typeof(SDST.SqlInt16)   , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlInt16   )), null, true, false, true));
				SqlInt32    = AddType(typeof(SDST.SqlInt32)   , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlInt32   )), null, true, false, true));
				SqlInt64    = AddType(typeof(SDST.SqlInt64)   , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlInt64   )), null, true, false, true));
				SqlMoney    = AddType(typeof(SDST.SqlMoney)   , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlMoney   )), null, true, false, true));
				SqlSingle   = AddType(typeof(SDST.SqlSingle)  , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlSingle  )), null, true, false, true));
				SqlString   = AddType(typeof(SDST.SqlString)  , new RegularType(Namespace, new CodeIdentifier(nameof(SDST.SqlString  )), null, true, false, true));
			}
		}
	}
}
