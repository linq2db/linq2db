#if NETFRAMEWORK
using System;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Mapping;

	public class SystemDataLinqAttributeReader : IMetadataReader
	{
		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			if (typeof(T) == typeof(TableAttribute))
			{
				var ta = AttributeReader.Instance.GetAttributes<System.Data.Linq.Mapping.TableAttribute>   (type, inherit);
				var da = AttributeReader.Instance.GetAttributes<System.Data.Linq.Mapping.DatabaseAttribute>(type, inherit);

				var t = ta.Length == 1 ? ta[0] : null;
				var d = da.Length == 1 ? da[0] : null;

				if (t != null || d != null)
				{
					string? name     = null;
					string? schema   = null;
					string? database = null;

					if (t != null)
					{
						name = t.Name;

						if (name != null)
						{
							var names = name.Replace("[", "").Replace("]", "").Split('.');

							switch (names.Length)
							{
								case 0  : break;
								case 1  : name = names[0]; break;
								case 2  :
									name   = names[1];
									schema = names[0];
									break;
								default :
									throw new MetadataException(string.Format(
										"Invalid table name '{0}' of type '{1}'",
										name, type.FullName));
							}
						}
					}

					if (d != null)
						database = d.Name;

					return new[] { (T)(Attribute)new TableAttribute(name) { Schema = schema, Database = database } };
				}
			}

			return Array<T>.Empty;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			if (typeof(T) == typeof(ColumnAttribute))
			{
				var attrs = AttributeReader.Instance.GetAttributes<System.Data.Linq.Mapping.ColumnAttribute>(type, memberInfo, inherit);

				if (attrs.Length == 1)
				{
					var c = attrs[0];

					var attr = new ColumnAttribute
					{
						Name            = c.Name,
						DbType          = c.DbType,
						CanBeNull       = c.CanBeNull,
						Storage         = c.Storage,
						IsPrimaryKey    = c.IsPrimaryKey,
						IsIdentity      = c.IsDbGenerated,
						IsDiscriminator = c.IsDiscriminator,
					};

					return new[] { (T)(Attribute)attr };
				}
			}
			else if (typeof(T) == typeof(AssociationAttribute))
			{
				var ta = AttributeReader.Instance.GetAttributes<System.Data.Linq.Mapping.TableAttribute>(type, memberInfo.DeclaringType, inherit);

				if (ta.Length == 1)
				{
					return AttributeReader.Instance
						.GetAttributes<System.Data.Linq.Mapping.AssociationAttribute>(type, memberInfo, inherit)
						.Select(a => (T)(Attribute)new AssociationAttribute { ThisKey = a.ThisKey, OtherKey = a.OtherKey, Storage = a.Storage })
						.ToArray();
				}
			}

			return Array<T>.Empty;
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;
	}
}
#endif
