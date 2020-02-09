using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Collections.Generic;
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

		private static readonly Lazy<TypeInfo[]> _types = new Lazy<TypeInfo[]>(() =>
		{
			Assembly? assembly = null;
			try
			{
				assembly = Assembly.Load(AssemblyName);
			}
			catch { }

			if (assembly == null)
				return Array<TypeInfo>.Empty;

			var types = new List<TypeInfo>();

			loadType(SqlHierarchyIdType, SqlServerTools.SqlHierarchyIdType);
			loadType(SqlGeographyType  , SqlServerTools.SqlGeographyType);
			loadType(SqlGeometryType   , SqlServerTools.SqlGeometryType);

			return types.ToArray();

			void loadType(string typeName, Type? type)
			{
				type ??= assembly?.GetType($"{TypesNamespace}.{typeName}", false);
				if (type == null)
					return;

				var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Property(null, type, "Null"), typeof(object))).Compile();

				SqlServerDataProvider.SetUdtType(type, type.Name.Substring(3).ToLower());

				types.Add(new TypeInfo() { Type = type, Null = getNullValue() });
			}
		}, true);

		class TypeInfo
		{
			public Type    Type { get; set; } = null!;
			public object? Null { get; set; }
		}

		internal static void Configure(MappingSchema mappingSchema)
		{
			foreach (var type in _types.Value)
			{
				mappingSchema.AddScalarType(type.Type, type.Null, true, DataType.Udt);
			}
		}
	}
}
