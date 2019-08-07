using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

using Microsoft.SqlServer.Server;

namespace LinqToDB.Metadata
{
	using Common;
	using Extensions;
	using Mapping;

	/// <summary>
	/// Adds support for types and functions, defined in Microsoft.SqlServer.Types spatial types
	/// (or any other types and methods, that use <see cref="SqlMethodAttribute"/> or <see cref="SqlUserDefinedTypeAttribute"/>
	/// mapping attributes).
	/// Check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
	/// for additional required configuration steps to support SQL Server spatial types.
	/// </summary>
	public class SystemDataSqlServerAttributeReader : IMetadataReader
	{
		readonly AttributeReader _reader = new AttributeReader();

		private static readonly Type _sqlMethodAttributeType;
		private static readonly Type _sqlUserDefinedTypeAttribute;

		static SystemDataSqlServerAttributeReader()
		{
#if !NETSTANDARD2_0
			_sqlMethodAttributeType      = typeof(SqlMethodAttribute);
			_sqlUserDefinedTypeAttribute = typeof(SqlUserDefinedTypeAttribute);
#else
			try
			{
				// this type missing from xamarin PCL and referencing it directly will fail mono linker
				// more proper solution will be to add separate build targets for xamarin
				// but this class anyway will be changed a bit later when we will remove SqlClient dependency
				// so it is just a temporary build fix before we do it
				_sqlMethodAttributeType      = Type.GetType("Microsoft.SqlServer.Server.SqlMethodAttribute, System.Data.SqlClient", false);
				_sqlUserDefinedTypeAttribute = Type.GetType("Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute, System.Data.SqlClient", false);
			}
			catch
			{
			}
#endif
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
					if (_sqlMethodAttributeType != null)
					{
						if (memberInfo.IsMethodEx())
						{
							var ma = _reader.GetAttributes<Attribute>(type, memberInfo, inherit)
								.Where(a => _sqlMethodAttributeType.IsAssignableFrom(a.GetType()))
								.ToArray();

							if (ma.Length > 0)
							{
								var mi = (MethodInfo)memberInfo;
								var ps = mi.GetParameters();

								var ex = mi.IsStatic
									?
									string.Format("{0}::{1}({2})",
										memberInfo.DeclaringType.Name.ToLower().StartsWith("sql")
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
							var gm = pi.GetGetMethodEx();

							if (gm != null)
							{
								var ma = _reader.GetAttributes<Attribute>(type, gm, inherit)
									.Where(a => _sqlMethodAttributeType.IsAssignableFrom(a.GetType()))
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

			if (typeof(T) == typeof(DataTypeAttribute) && _sqlUserDefinedTypeAttribute != null)
			{
				var attrs = _reader.GetAttributes<Attribute>(memberInfo.GetMemberType(), inherit)
					.Where(a => _sqlUserDefinedTypeAttribute.IsAssignableFrom(a.GetType()))
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
