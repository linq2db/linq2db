using System;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Metadata provider using mapping attributes from <see cref="System.ComponentModel.DataAnnotations.Schema"/> namespace:
	/// <list type="bullet">
	/// <item><see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute"/></item>
	/// <item><see cref="System.ComponentModel.DataAnnotations.Schema.ColumnAttribute"/></item>
	/// </list>
	/// </summary>
	public class SystemComponentModelDataAnnotationsSchemaAttributeReader : IMetadataReader
	{
		public MappingAttribute[] GetAttributes(Type type)
		{
			var t = type.GetAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();

			if (t != null)
			{
				var attr = new TableAttribute();

				var name = t.Name;

				if (name != null)
				{
					var names = name.Replace("[", "", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal).Split('.');

					switch (names.Length)
					{
						case 0: break;
						case 1: attr.Name = names[0]; break;
						case 2:
							attr.Name   = names[0];
							attr.Schema = names[1];
							break;
						default:
							throw new MetadataException($"Invalid table name '{name}' of type '{type.FullName}'");
					}
				}

				return new MappingAttribute[] { attr };
			}

			return [];
		}

		public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
		{
			var c = memberInfo.GetAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();

			if (c != null)
			{
				var attr = new ColumnAttribute()
				{
					Name   = c.Name,
					DbType = c.TypeName,
				};

				return new MappingAttribute[] { attr };
			}

			return [];
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => [];

		public string GetObjectID() => $".{nameof(SystemComponentModelDataAnnotationsSchemaAttributeReader)}.";
	}
}
