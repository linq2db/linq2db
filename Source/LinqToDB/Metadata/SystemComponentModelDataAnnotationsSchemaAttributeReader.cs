using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Mapping;

	public class SystemComponentModelDataAnnotationsSchemaAttributeReader : IMetadataReader
	{
		public T[] GetAttributes<T>(Type type, bool inherit)
			where T : Attribute
		{
			if (typeof(T) == typeof(TableAttribute))
			{
				var ta = AttributeReader.Instance.GetAttributes<System.ComponentModel.DataAnnotations.Schema.TableAttribute>(type, inherit);

				var t = ta.Length == 1 ? ta[0] : null;

				if (t != null)
				{
					var     name   = t.Name;
					string? schema = null;

					if (name != null)
					{
						var names = name.Replace("[", "").Replace("]", "").Split('.');

						switch (names.Length)
						{
							case 0  : break;
							case 1  : name = names[0]; break;
							case 2  :
								name   = names[0];
								schema = names[1];
								break;
							default :
								throw new MetadataException(string.Format(
									"Invalid table name '{0}' of type '{1}'",
									name, type.FullName));
						}
					}

					return new[] { (T)(Attribute)new TableAttribute(name) { Schema = schema } };
				}
			}

			return Array<T>.Empty;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit)
			where T : Attribute
		{
			if (typeof(T) == typeof(ColumnAttribute))
			{
				var attrs = AttributeReader.Instance.GetAttributes<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>(type, memberInfo, inherit);

				if (attrs.Length == 1)
				{
					var c = attrs[0];

					var attr = new ColumnAttribute
					{
						Name   = c.Name,
						DbType = c.TypeName
					};

					return new[] { (T)(Attribute)attr };
				}
			}

			return Array<T>.Empty;
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;
	}
}
