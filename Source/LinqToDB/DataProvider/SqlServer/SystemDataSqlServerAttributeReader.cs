using System.Collections.Concurrent;
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
		static readonly ConcurrentDictionary<MemberInfo,object> _cache  = new ();
		static readonly AttributeReader                         _reader = new ();
		static readonly Type[]                                  _sqlMethodAttributes;
		static readonly Type[]                                  _sqlUserDefinedTypeAttributes;

		// TODO: v5 convert to Instance field
		static SystemDataSqlServerAttributeReader()
		{
			// try/catch applied as Type.GetType(throwOnError: false) still can throw for sfx builds
			// see https://github.com/linq2db/linq2db/issues/3630

			Type? methodAttr1 = null;
			Type? methodAttr2 = null;
			Type? methodAttr3 = null;
			Type? typeAttr1   = null;
			Type? typeAttr2   = null;
			Type? typeAttr3   = null;

			try
			{
				methodAttr1 = Type.GetType("Microsoft.SqlServer.Server.SqlMethodAttribute, System.Data.SqlClient"         , false);
				typeAttr1   = Type.GetType("Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute, System.Data.SqlClient", false);
			}
			catch
			{
			}

			try
			{
				methodAttr2 = Type.GetType("Microsoft.Data.SqlClient.Server.SqlMethodAttribute, Microsoft.Data.SqlClient"         , false);
				typeAttr2   = Type.GetType("Microsoft.Data.SqlClient.Server.SqlUserDefinedTypeAttribute, Microsoft.Data.SqlClient", false);
			}
			catch
			{
			}

			// added since https://github.com/dotnet/SqlClient/releases/tag/v5.0.0-preview3
			try
			{
				methodAttr3 = Type.GetType("Microsoft.SqlServer.Server.SqlMethodAttribute, Microsoft.SqlServer.Server"         , false);
				typeAttr3   = Type.GetType("Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute, Microsoft.SqlServer.Server", false);
			}
			catch
			{
			}

			_sqlMethodAttributes = new[]
			{
				methodAttr1,
				methodAttr2,
				methodAttr3,
#if NETFRAMEWORK
				typeof(Microsoft.SqlServer.Server.SqlMethodAttribute),
#endif
			}.Where(t => t != null).Distinct().ToArray()!;

			_sqlUserDefinedTypeAttributes = new[]
			{
				typeAttr1,
				typeAttr2,
				typeAttr3,
#if NETFRAMEWORK
				typeof(Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute),
#endif
			}.Where(t => t != null).Distinct().ToArray()!;
		}

		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			return Array<T>.Empty;
		}

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
											? memberInfo.DeclaringType.Name.ToLower().Substring(3)
											: memberInfo.DeclaringType.Name.ToLower(),
											((dynamic)ma[0]).Name ?? memberInfo.Name,
										string.Join(", ", ps.Select((_, i) => '{' + i.ToString() + '}')))
									:
									string.Format("{{0}}.{0}({1})",
											((dynamic)ma[0]).Name ?? memberInfo.Name,
										string.Join(", ", ps.Select((_, i) => '{' + (i + 1).ToString() + '}')));

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
