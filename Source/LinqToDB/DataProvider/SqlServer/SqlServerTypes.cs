using System;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.SqlServer
{
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
			catch
			{
				// ignore
			}

			return [];
		}, true);

		public static bool UpdateTypes()
		{
			try
			{
				return UpdateTypes(Assembly.Load(AssemblyName));
			}
			catch
			{
				// ignore
			}

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
			catch
			{
				// ignore
			}

			return false;
		}

		private static TypeInfo[] LoadTypes(Assembly assembly)
		{
			var types = new TypeInfo[3];

			types[0] = LoadType(SqlHierarchyIdType);
			types[1] = LoadType(SqlGeographyType);
			types[2] = LoadType(SqlGeometryType);

			return types;

			TypeInfo LoadType(string typeName)
			{
				var type = assembly.GetType($"{TypesNamespace}.{typeName}", true)!;

				var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(ExpressionHelper.Property(type, "Null"), typeof(object))).CompileExpression();

				return new TypeInfo()
				{
					Type     = type,
					TypeName = type.Name.Substring(3).ToLowerInvariant(),
					Null     = getNullValue()
				};
			}
		}

		sealed class TypeInfo
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
