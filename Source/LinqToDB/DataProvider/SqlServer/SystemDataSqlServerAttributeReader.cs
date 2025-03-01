using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;

#if NETFRAMEWORK
using Microsoft.SqlServer.Server;
#endif

using LinqToDB;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using LinqToDB.Internal.Common;

namespace LinqToDB.DataProvider.SqlServer
{
	/// <summary>
	/// Adds support for types and functions, defined in Microsoft.SqlServer.Types spatial types
	/// (or any other types and methods, that use SqlMethodAttribute or SqlUserDefinedTypeAttribute mapping attributes).
	/// Check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
	/// for additional required configuration steps to support SQL Server spatial types.
	/// </summary>
	public class SystemDataSqlServerAttributeReader : IMetadataReader
	{
		/// <summary>
		/// Provider instance, which use mapping attributes from System.Data.SqlClient assembly.
		/// Could be null of assembly not found.
		/// </summary>
		public static IMetadataReader? SystemDataSqlClientProvider =
#if NETFRAMEWORK
			new SystemDataSqlServerAttributeReader(typeof(SqlMethodAttribute), typeof(SqlUserDefinedTypeAttribute))
#else
			TryCreate(
				"Microsoft.SqlServer.Server.SqlMethodAttribute, System.Data.SqlClient",
				"Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute, System.Data.SqlClient")
#endif
			;

		/// <summary>
		/// Provider instance, which use mapping attributes from Microsoft.Data.SqlClient assembly (v1-4).
		/// Could be null of assembly not found.
		/// </summary>
		public static IMetadataReader? MicrosoftDataSqlClientProvider = TryCreate(
			"Microsoft.Data.SqlClient.Server.SqlMethodAttribute, Microsoft.Data.SqlClient",
			"Microsoft.Data.SqlClient.Server.SqlUserDefinedTypeAttribute, Microsoft.Data.SqlClient");

		/// <summary>
		/// Provider instance, which uses mapping attributes from Microsoft.SqlServer.Server assembly.
		/// Used with Microsoft.Data.SqlClient provider starting from v5.
		/// Could be null of assembly not found.
		/// </summary>
		public static IMetadataReader? MicrosoftSqlServerServerProvider = TryCreate(
			"Microsoft.SqlServer.Server.SqlMethodAttribute, Microsoft.SqlServer.Server",
			"Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute, Microsoft.SqlServer.Server");

		readonly ConcurrentDictionary<(MemberInfo memberInfo,Type attributeType), MappingAttribute[]> _cache = new ();
		readonly Type   _sqlMethodAttribute;
		readonly Type   _sqlUserDefinedTypeAttribute;
		readonly string _objectId;

		readonly Func<object, string?> _methodNameGetter;
		readonly Func<object, string?> _typeNameGetter;

		/// <summary>
		/// Creates new instance of <see cref="SystemDataSqlServerAttributeReader"/>.
		/// </summary>
		/// <param name="sqlMethodAttribute">SqlMethodAttribute type.</param>
		/// <param name="sqlUserDefinedTypeAttribute">SqlUserDefinedTypeAttribute type.</param>
		public SystemDataSqlServerAttributeReader(Type sqlMethodAttribute, Type sqlUserDefinedTypeAttribute)
		{
			_sqlMethodAttribute          = sqlMethodAttribute;
			_sqlUserDefinedTypeAttribute = sqlUserDefinedTypeAttribute;
			_objectId                    = $".{_sqlMethodAttribute.AssemblyQualifiedName}.{_sqlUserDefinedTypeAttribute.AssemblyQualifiedName}.";

			var methodNameGetter = _sqlMethodAttribute.GetProperty("Name")!.GetMethod!;
			_methodNameGetter    = attr => methodNameGetter.InvokeExt<string?>(attr, null);

			var udtNameGetter = _sqlUserDefinedTypeAttribute.GetProperty("Name")!.GetMethod!;
			_typeNameGetter   = attr => udtNameGetter.InvokeExt<string?>(attr, null);
		}

		static SystemDataSqlServerAttributeReader? TryCreate(string sqlMethodAttributeType, string sqlUserDefinedTypeAttributeType)
		{
			// throwOnError: false not used as it doesn't really work
			// see https://github.com/linq2db/linq2db/issues/3630
			try
			{
				var methodAttr = Type.GetType(sqlMethodAttributeType         , true)!;
				var typeAttr   = Type.GetType(sqlUserDefinedTypeAttributeType, true)!;
				return new SystemDataSqlServerAttributeReader(methodAttr, typeAttr);
			}
			catch
			{
			}

			return null;
		}

		public MappingAttribute[] GetAttributes(Type type) => [];

		public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
		{
			// HACK: we use _sqlMethodAttribute/_sqlUserDefinedTypeAttribute as cache key part instead of typeof(T) to avoid closure generation for lambda
			// this is valid approach for current code but if we will add more attributes support we will need to add typeof(T) to key too
			// (which probably will never happen anyways)

			MappingAttribute[]? result = null;
			if (memberInfo.IsMethodEx() || memberInfo.IsPropertyEx())
			{
				result = _cache.GetOrAdd(
					(memberInfo, _sqlMethodAttribute),
#if NETFRAMEWORK || NETSTANDARD2_0
					key =>
					{
						var nameGetter = _methodNameGetter;
#else
					static (key, nameGetter) =>
					{
#endif
						if (key.memberInfo.IsMethodEx())
						{
							var attr = FindAttribute(key.memberInfo, key.attributeType);

							if (attr != null)
							{
								var mi = (MethodInfo)key.memberInfo;
								var ps = mi.GetParameters();

								string ex;
								if (mi.IsStatic)
								{
									var name = key.memberInfo.DeclaringType!.Name.ToLowerInvariant();
									name = name.StartsWith("sql") ? name.Substring(3) : name;

									ex = string.Format(
										CultureInfo.InvariantCulture,
										"{0}::{1}({2})",
										name,
										nameGetter(attr) ?? key.memberInfo.Name,
										string.Join(", ", ps.Select((_, i) => '{' + i.ToString(NumberFormatInfo.InvariantInfo) + '}')));
								}
								else
								{
									ex = string.Format(
										CultureInfo.InvariantCulture,
										"{{0}}.{0}({1})",
										nameGetter(attr) ?? key.memberInfo.Name,
										string.Join(", ", ps.Select((_, i) => '{' + (i + 1).ToString(NumberFormatInfo.InvariantInfo) + '}')));
								}

								return new MappingAttribute[] { new Sql.ExpressionAttribute(ex) { ServerSideOnly = true } };
							}
						}
						else
						{
							var pi = (PropertyInfo)key.memberInfo;
							var gm = pi.GetGetMethod();

							if (gm != null)
							{
								var attr = FindAttribute(gm, key.attributeType);

								if (attr != null)
								{
									var ex = $"{{0}}.{nameGetter(attr) ?? key.memberInfo.Name}";

									return new MappingAttribute[] { new Sql.ExpressionAttribute(ex) { ServerSideOnly = true, ExpectExpression = true } };
								}
							}
						}

						return [];
#if NETFRAMEWORK || NETSTANDARD2_0
					});
#else
					}, _methodNameGetter);
#endif
			}

			var res = _cache.GetOrAdd(
				(memberInfo, _sqlUserDefinedTypeAttribute),
#if NETFRAMEWORK || NETSTANDARD2_0
				key =>
				{
					var nameGetter = _typeNameGetter;
#else
				static (key, nameGetter) =>
				{
#endif
					var c = FindAttribute(key.memberInfo.GetMemberType(), key.attributeType);

					if (c != null)
					{
						var n = nameGetter(c) ?? key.memberInfo.GetMemberType().Name;

						if (n.StartsWith("sql", StringComparison.OrdinalIgnoreCase))
							n = n.Substring(3);

						var attr = new DataTypeAttribute(DataType.Udt, n);

						return new MappingAttribute[] { attr };
					}

					return [];
#if NETFRAMEWORK || NETSTANDARD2_0
				});
#else
				}, _typeNameGetter);
#endif

			result = result == null || result.Length == 0
				? res
				: res.Length == 0
					? result
					: result.Concat(res).ToArray();

			return result ?? [];
		}

		private static Attribute? FindAttribute(ICustomAttributeProvider source, Type attributeType)
		{
			foreach (var attr in source.GetAttributes<Attribute>())
			{
				if (attributeType.IsAssignableFrom(attr.GetType()))
					return attr;
			}

			return null;
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => [];

		public string GetObjectID() => _objectId;
	}
}
