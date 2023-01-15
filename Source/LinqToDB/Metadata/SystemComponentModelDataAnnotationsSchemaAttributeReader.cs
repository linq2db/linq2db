﻿using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Extensions;
	using Mapping;

	/// <summary>
	/// Metadata provider using mapping attributes from <see cref="System.ComponentModel.DataAnnotations.Schema"/> namespace:
	/// <list type="bullet">
	/// <item><see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute"/></item>
	/// <item><see cref="System.ComponentModel.DataAnnotations.Schema.ColumnAttribute"/></item>
	/// </list>
	/// </summary>
	public class SystemComponentModelDataAnnotationsSchemaAttributeReader : IMetadataReader
	{
		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
		{
			if (typeof(T).IsAssignableFrom(typeof(TableAttribute)))
			{
				var t = type.GetAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();

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
								throw new MetadataException($"Invalid table name '{name}' of type '{type.FullName}'");
						}
					}

					return new[] { (T)(Attribute)attr };
				}
			}

			return Array<T>.Empty;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			if (typeof(T).IsAssignableFrom(typeof(ColumnAttribute)))
			{
				var c = memberInfo.GetAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();

				if (c != null)
				{
					var attr = new ColumnAttribute()
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

		public string GetObjectID() => $".{nameof(SystemComponentModelDataAnnotationsSchemaAttributeReader)}.";
	}
}
