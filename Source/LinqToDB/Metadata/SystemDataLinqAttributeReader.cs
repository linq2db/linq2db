#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Metadata provider using mapping attributes from <see cref="System.Data.Linq.Mapping"/> namespace:
	/// <list type="bullet">
	/// <item><see cref="System.Data.Linq.Mapping.TableAttribute"/></item>
	/// <item><see cref="System.Data.Linq.Mapping.DatabaseAttribute"/></item>
	/// <item><see cref="System.Data.Linq.Mapping.ColumnAttribute"/></item>
	/// <item><see cref="System.Data.Linq.Mapping.AssociationAttribute"/></item>
	/// </list>
	/// </summary>
	public class SystemDataLinqAttributeReader : IMetadataReader
	{
		public MappingAttribute[] GetAttributes(Type type)
		{
			var t = type.GetAttribute<System.Data.Linq.Mapping.TableAttribute>   ();
			var d = type.GetAttribute<System.Data.Linq.Mapping.DatabaseAttribute>();

			if (t != null || d != null)
			{
				var attr = new TableAttribute();

				if (t != null)
				{
					var name = t.Name;

					if (name != null)
					{
						var names = name.Replace("[", "", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal).Split('.');

						switch (names.Length)
						{
							case 0: break;
							case 1: attr.Name = names[0]; break;
							case 2:
								attr.Name   = names[1];
								attr.Schema = names[0];
								break;
							default:
								throw new MetadataException($"Invalid table name '{name}' of type '{type.FullName}'");
						}
					}
				}

				if (d != null)
					attr.Database = d.Name;

				return new MappingAttribute[] { attr };
			}

			return [];
		}

		public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
		{
			List<MappingAttribute>? results = null;
			var c = memberInfo.GetAttribute<System.Data.Linq.Mapping.ColumnAttribute>();

			if (c != null)
			{
				var attr = new ColumnAttribute()
				{
					Name            = c.Name,
					DbType          = c.DbType,
					CanBeNull       = c.CanBeNull,
					Storage         = c.Storage,
					IsPrimaryKey    = c.IsPrimaryKey,
					IsIdentity      = c.IsDbGenerated,
					IsDiscriminator = c.IsDiscriminator,
				};

				(results = new()).Add(attr);
			}

			if (memberInfo.DeclaringType.HasAttribute<System.Data.Linq.Mapping.TableAttribute>())
			{
				var attrs = memberInfo.GetAttributes<System.Data.Linq.Mapping.AssociationAttribute>();
				if (attrs.Length > 0)
				{
					results ??= new(attrs.Length);

					for (var i = 0; i < attrs.Length; i++)
						results.Add(new AssociationAttribute { ThisKey = attrs[i].ThisKey, OtherKey = attrs[i].OtherKey, Storage = attrs[i].Storage });
				}
			}

			return results?.ToArray() ?? [];
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => [];

		public string GetObjectID() => $".{nameof(SystemDataLinqAttributeReader)}.";
	}
}
#endif
