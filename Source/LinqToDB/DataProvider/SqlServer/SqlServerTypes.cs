using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Linq.Expressions;
	using System.Reflection;
	using LinqToDB.Common;
	using LinqToDB.Mapping;

	internal static class SqlServerTypes
	{
		public const string AssemblyName   = "Microsoft.SqlServer.Types";
		public const string TypesNamespace = "Microsoft.SqlServer.Types";

		public const string SqlHierarchyIdType = "SqlHierarchyId";
		public const string SqlGeographyType   = "SqlGeography";
		public const string SqlGeometryType    = "SqlGeometry";

		private static Lazy<TypeInfo[]> _types = new Lazy<TypeInfo[]>(() =>
		{
			try
			{
				var assembly = Assembly.Load(AssemblyName);

				if (assembly != null)
					return LoadTypes(assembly);
			}
			catch { }

			return Array<TypeInfo>.Empty;
		}, true);

		public static bool UpdateTypes()
		{
			try
			{
				return UpdateTypes(Assembly.Load(AssemblyName));
			}
			catch { }

			return false;
		}

		public static bool UpdateTypes(Assembly assembly)
		{
			try
			{
				var newTypes = new Lazy<TypeInfo[]>(() => LoadTypes(assembly));
				if (newTypes.Value.Length > 0)
				{
					_types = newTypes;
					return true;
				}
			}
			catch { }

			return false;
		}

		private static TypeInfo[] LoadTypes(Assembly assembly)
		{
			var types = new TypeInfo[3];

			types[0] = loadType(SqlHierarchyIdType);
			types[1] = loadType(SqlGeographyType);
			types[2] = loadType(SqlGeometryType);

			return types;

			TypeInfo loadType(string typeName)
			{
				var type = assembly.GetType($"{TypesNamespace}.{typeName}", true);

				var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Property(null, type, "Null"), typeof(object))).Compile();

				return new TypeInfo()
				{ 
					Type     = type,
					TypeName = type.Name.Substring(3).ToLower(),
					Null     = getNullValue()
				};
			}
		}

		class TypeInfo
		{
			public string  TypeName { get; set; } = null!;
			public Type    Type     { get; set; } = null!;
			public object? Null     { get; set; }
		}

		internal static void Configure(MappingSchema mappingSchema)
		{
			foreach (var type in _types.Value)
				mappingSchema.AddScalarType(type.Type, type.Null, true, DataType.Udt);
		}

		internal static void Configure(SqlServerDataProvider provider)
		{
			foreach (var type in _types.Value)
				provider.AddUdtType(type.Type, type.TypeName, type.Null, DataType.Udt);
		}
	}
}
