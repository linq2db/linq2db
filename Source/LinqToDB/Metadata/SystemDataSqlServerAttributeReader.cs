using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

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
		readonly AttributeReader _reader = new AttributeReader();

		private static readonly Type[] _sqlMethodAttributes;
		private static readonly Type[] _sqlUserDefinedTypeAttributes;

		static SystemDataSqlServerAttributeReader()
		{
			_sqlMethodAttributes = new[]
			{
#if NET45 || NET46
				typeof(Microsoft.SqlServer.Server.SqlMethodAttribute),
#endif
				Type.GetType("Microsoft.SqlServer.Server.SqlMethodAttribute, System.Data.SqlClient", false),
				Type.GetType("Microsoft.Data.SqlClient.Server.SqlMethodAttribute, Microsoft.Data.SqlClient", false)
			}.Where(_ => _ != null).Distinct().ToArray()!;

			_sqlUserDefinedTypeAttributes = new[]
			{
#if NET45 || NET46
				typeof(Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute),
#endif
				Type.GetType("Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute, System.Data.SqlClient", false),
				Type.GetType("Microsoft.Data.SqlClient.Server.SqlUserDefinedTypeAttribute, Microsoft.Data.SqlClient", false)
			}.Where(_ => _ != null).Distinct().ToArray()!;
		}

		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			return Array<T>.Empty;
		}

		static readonly ConcurrentDictionary<MemberInfo,object> _cache = new ConcurrentDictionary<MemberInfo,object>();

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			if (typeof(T) == typeof(Sql.ExpressionAttribute) && (memberInfo.IsMethodEx() || memberInfo.IsPropertyEx()))
			{
				if (!_cache.TryGetValue(memberInfo, out var attrs))
				{
					if (_sqlMethodAttributes.Length > 0)
					{
						if (memberInfo.IsMethodEx())
						{
							var ma = _reader.GetAttributes<Attribute>(type, memberInfo, inherit)
								.Where(a => _sqlMethodAttributes.Any(_ => _.IsAssignableFrom(a.GetType())))
								.ToArray();

							if (ma.Length > 0)
							{
								var mi = (MethodInfo)memberInfo;
								var ps = mi.GetParameters();

								var ex = mi.IsStatic
									?
									string.Format("{0}::{1}({2})",
										memberInfo.DeclaringType!.Name.ToLower().StartsWith("sql")
											? memberInfo.DeclaringType.Name.Substring(3)
											: memberInfo.DeclaringType.Name,
											((dynamic)ma[0]).Name ?? memberInfo.Name,
										string.Join(", ", ps.Select((_, i) => '{' + i.ToString() + '}').ToArray()))
									:
									string.Format("{{0}}.{0}({1})",
											((dynamic)ma[0]).Name ?? memberInfo.Name,
										string.Join(", ", ps.Select((_, i) => '{' + (i + 1).ToString() + '}').ToArray()));

								attrs = new[] { (T)(Attribute)new Sql.ExpressionAttribute(ex) { ServerSideOnly = true } };
							}
							else
							{
								attrs = Array<T>.Empty;
							}
						}
						else
						{
							var pi = (PropertyInfo)memberInfo;
							var gm = pi.GetGetMethod();

							if (gm != null)
							{
								var ma = _reader.GetAttributes<Attribute>(type, gm, inherit)
									.Where(a => _sqlMethodAttributes.Any(_ => _.IsAssignableFrom(a.GetType())))
									.ToArray();

								if (ma.Length > 0)
								{
									var ex = $"{{0}}.{((dynamic)ma[0]).Name ?? memberInfo.Name}";

									attrs = new[] { (T)(Attribute)new Sql.ExpressionAttribute(ex) { ServerSideOnly = true, ExpectExpression = true } };
								}
								else
								{
									attrs = Array<T>.Empty;
								}
							}
							else
							{
								attrs = Array<T>.Empty;
							}
						}
					}
					else
						attrs = Array<T>.Empty;

					_cache[memberInfo] = attrs;
				}

				return (T[])attrs;
			}

			if (typeof(T) == typeof(DataTypeAttribute) && _sqlUserDefinedTypeAttributes.Length > 0)
			{
				var attrs = _reader.GetAttributes<Attribute>(memberInfo.GetMemberType(), inherit)
					.Where(a => _sqlUserDefinedTypeAttributes.Any(_ => _.IsAssignableFrom(a.GetType())))
					.ToArray();

				if (attrs.Length == 1)
				{
					var c = attrs[0];
					var n = ((dynamic)c).Name ?? memberInfo.GetMemberType().Name;

					if (n.ToLower().StartsWith("sql"))
						n = n.Substring(3);

					var attr = new DataTypeAttribute(DataType.Udt, n);

					return new[] { (T)(Attribute)attr };
				}
			}

			return Array<T>.Empty;
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type)
			=> _reader.GetDynamicColumns(type);
	}
}
