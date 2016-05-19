using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Mapping;

using Microsoft.SqlServer.Server;

namespace LinqToDB.Metadata
{
	using Common;

	public class SystemDataSqlServerAttributeReader : IMetadataReader
	{
		readonly AttributeReader _reader = new AttributeReader();

		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			return Array<T>.Empty;
		}

		static readonly Dictionary<MemberInfo,object> _cache = new Dictionary<MemberInfo,object>();

		public T[] GetAttributes<T>(MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			if (typeof(T) == typeof(Sql.ExpressionAttribute) && (memberInfo.IsMethodEx() || memberInfo.IsPropertyEx()))
			{
				object attrs;

				if (!_cache.TryGetValue(memberInfo, out attrs))
				{
					lock (_cache)
					{
						if (!_cache.TryGetValue(memberInfo, out attrs))
						{
							if (memberInfo.IsMethodEx())
							{
								var ma = _reader.GetAttributes<SqlMethodAttribute>(memberInfo);

								if (ma.Length > 0)
								{
									var mi = (MethodInfo)memberInfo;
									var ps = mi.GetParameters();

									var ex = mi.IsStatic
										?
										"{0}::{1}({2})".Args(
											memberInfo.DeclaringType.Name.ToLower().StartsWith("sql")
												? memberInfo.DeclaringType.Name.Substring(3)
												: memberInfo.DeclaringType.Name,
											ma[0].Name ?? memberInfo.Name,
											string.Join(", ", ps.Select((_,i) => '{' + i.ToString() + '}').ToArray()))
										:
										"{{0}}.{0}({1})".Args(
											ma[0].Name ?? memberInfo.Name,
												string.Join(", ", ps.Select((_,i) => '{' + (i + 1).ToString() + '}').ToArray()));

									attrs = new [] { (T)(Attribute)new Sql.ExpressionAttribute(ex) { ServerSideOnly = true } };
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
									var ma = _reader.GetAttributes<SqlMethodAttribute>(gm);

									if (ma.Length > 0)
									{
										var ex = "{{0}}.{0}".Args(ma[0].Name ?? memberInfo.Name);

										attrs = new [] { (T)(Attribute)new Sql.ExpressionAttribute(ex) { ServerSideOnly = true, ExpectExpression = true } };
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

							_cache[memberInfo] = attrs;
						}
					}
				}

				return (T[])attrs;
			}

			if (typeof(T) == typeof(DataTypeAttribute))
			{
				var attrs = _reader.GetAttributes<SqlUserDefinedTypeAttribute>(memberInfo.GetMemberType(), inherit);

				if (attrs.Length == 1)
				{
					var c = attrs[0];
					var n = c.Name ?? memberInfo.GetMemberType().Name;

					if (n.ToLower().StartsWith("sql"))
						n = n.Substring(3);

					var attr = new DataTypeAttribute(DataType.Udt, n);

					return new[] { (T)(Attribute)attr };
				}
			}

			return Array<T>.Empty;
		}
	}
}
