using System;

namespace LinqToDB.Metadata
{
	using Common;

	public class SystemDataLinqAttributeReader : IMetadataReader
	{
		readonly AttributeReader _reader = new AttributeReader();

		public T[] GetAttributes<T>(Type type)
			where T : Attribute
		{
			if (typeof(T) == typeof(TableAttribute))
			{
				var ta = _reader.GetAttributes<System.Data.Linq.Mapping.TableAttribute>   (type);
				var da = _reader.GetAttributes<System.Data.Linq.Mapping.DatabaseAttribute>(type);

				var t = ta.Length == 1 ? ta[0] : null;
				var d = da.Length == 1 ? da[0] : null;

				if (t != null || d != null)
				{
					var attr = new TableAttribute();

					if (t != null)
					{
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
					}

					if (d != null)
						attr.Database = d.Name;

					return new[] { (T)(Attribute)attr };
				}
			}

			return Array<T>.Empty;
		}

		public T[] GetAttributes<T>(Type type, string memberName)
			where T : Attribute
		{
			if (typeof(T) == typeof(ColumnAttribute))
			{
				var attrs = _reader.GetAttributes<System.Data.Linq.Mapping.ColumnAttribute>(type, memberName);

				if (attrs.Length == 1)
				{
					var c = attrs[0];

					var attr = new ColumnAttribute
					{
						Name      = c.Name,
						DbType    = c.DbType,
						CanBeNull = c.CanBeNull,
					};

					return new[] { (T)(Attribute)attr };
				}
			}

			return Array<T>.Empty;
		}
	}
}
