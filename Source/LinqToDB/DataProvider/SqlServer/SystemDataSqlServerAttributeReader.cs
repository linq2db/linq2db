using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

#if NETFRAMEWORK
using Microsoft.SqlServer.Server;
#endif

namespace LinqToDB.Metadata
{
	using Common;
	using Extensions;
	using Mapping;

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
		public static IMetadataReader? SystemDataSqlClientProvider = TryCreate(
			"Microsoft.SqlServer.Server.SqlMethodAttribute, System.Data.SqlClient",
			"Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute, System.Data.SqlClient")
#if NETFRAMEWORK
			?? new SystemDataSqlServerAttributeReader(typeof(SqlMethodAttribute), typeof(SqlUserDefinedTypeAttribute))
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

		readonly ConcurrentDictionary<(MemberInfo memberInfo,Type attributeType),object> _cache = new ();
		readonly Type   _sqlMethodAttribute;
		readonly Type   _sqlUserDefinedTypeAttribute;
		readonly string _objectId;

		/// <summary>
		/// Creates new instance of <see cref="SystemDataSqlServerAttributeReader"/>.
		/// </summary>
		/// <param name="sqlMethodAttribute">SqlMethodAttribute type.</param>
		/// <param name="sqlUserDefinedTypeAttribute">SqlUserDefinedTypeAttribute type.</param>
		public SystemDataSqlServerAttributeReader(Type sqlMethodAttribute, Type sqlUserDefinedTypeAttribute)
		{
			_sqlMethodAttribute          = sqlMethodAttribute;
			_sqlUserDefinedTypeAttribute = sqlUserDefinedTypeAttribute;
			_objectId                    = $".{_sqlMethodAttribute.FullName}.{_sqlUserDefinedTypeAttribute.FullName}.";
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

		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
		{
			return Array<T>.Empty;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			// HACK: we use _sqlMethodAttribute/_sqlUserDefinedTypeAttribute as cache key part instead of typeof(T) to avoid closure generation for lambda
			// this is valid approach for current code but if we will add more attributes support we will need to add typeof(T) to key too
			// (which probably will never happen anyways)
			if (typeof(T) == typeof(Sql.ExpressionAttribute) && (memberInfo.IsMethodEx() || memberInfo.IsPropertyEx()))
			{
				return (T[])_cache.GetOrAdd((memberInfo, _sqlMethodAttribute), static key =>
				{
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
									"{0}::{1}({2})",
									name,
									((dynamic)attr).Name ?? key.memberInfo.Name,
									string.Join(", ", ps.Select((_, i) => '{' + i.ToString() + '}')));
							}
							else
							{
								ex = string.Format(
									"{{0}}.{0}({1})",
									((dynamic)attr).Name ?? key.memberInfo.Name,
									string.Join(", ", ps.Select((_, i) => '{' + (i + 1).ToString() + '}')));
							}

							return new[] { (T)(Attribute)new Sql.ExpressionAttribute(ex) { ServerSideOnly = true } };
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
								var ex = $"{{0}}.{((dynamic)attr).Name ?? key.memberInfo.Name}";

								return new[] { (T)(Attribute)new Sql.ExpressionAttribute(ex) { ServerSideOnly = true, ExpectExpression = true } };
							}
						}
					}

					return Array<T>.Empty;
				});
			}

			if (typeof(T) == typeof(DataTypeAttribute))
			{
				return (T[])_cache.GetOrAdd((memberInfo, _sqlUserDefinedTypeAttribute), static key =>
				{
					var c = FindAttribute(key.memberInfo.GetMemberType(), key.attributeType);

					if (c != null)
					{
						var n = ((dynamic)c).Name ?? key.memberInfo.GetMemberType().Name;

						if (n.ToLower().StartsWith("sql"))
							n = n.Substring(3);

						var attr = new DataTypeAttribute(DataType.Udt, n);

						return new[] { (T)(Attribute)attr };
					}

					return Array<T>.Empty;
				});
			}

			return Array<T>.Empty;
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
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;

		public string GetObjectID() => _objectId;
	}
}
