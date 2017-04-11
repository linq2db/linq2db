#if NETSTANDARD

using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Mapping;

	public class SystemComponentModelDataAnnotationsSchemaAttributeReader : IMetadataReader
	{
		readonly AttributeReader _reader = new AttributeReader();

		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			if (typeof(T) == typeof(TableAttribute))
			{
				var ta = _reader.GetAttributes<System.ComponentModel.DataAnnotations.Schema.TableAttribute>   (type, inherit);

				var t = ta.Length == 1 ? ta[0] : null;

				if (t != null)
				{
					var attr = new TableAttribute();

					var name = t.Name;

					if (name != null)
					{
						var names = name.Replace("[", "").Replace("]", "").Split('.');

						switch (names.Length)
						{
							case 0  : break;
							case 1  : attr.Name = names[0]; break;
							case 2  :
								attr.Name   = names[0];
								attr.Schema = names[1];
								break;
							default :
								throw new MetadataException(string.Format(
									"Invalid table name '{0}' of type '{1}'",
									name, type.FullName));
						}
					}

					return new[] { (T)(Attribute)attr };
				}
			}

			return Array<T>.Empty;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			if (typeof(T) == typeof(ColumnAttribute))
			{
				var attrs = _reader.GetAttributes<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>(type, memberInfo, inherit);

				if (attrs.Length == 1)
				{
					var c = attrs[0];

					var attr = new ColumnAttribute
					{
						Name      = c.Name,
						DbType    = c.TypeName 
					};

					return new[] { (T)(Attribute)attr };
				}
			}

			return Array<T>.Empty;
		}
	}
}

#endif