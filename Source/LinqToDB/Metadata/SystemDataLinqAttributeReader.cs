﻿#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Extensions;
	using Mapping;

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
		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
		{
			if (typeof(T).IsAssignableFrom(typeof(TableAttribute)))
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
							var names = name.Replace("[", "").Replace("]", "").Split('.');

							switch (names.Length)
							{
								case 0  : break;
								case 1  : attr.Name = names[0]; break;
								case 2  :
									attr.Name   = names[1];
									attr.Schema = names[0];
									break;
								default :
									throw new MetadataException($"Invalid table name '{name}' of type '{type.FullName}'");
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

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			if (typeof(T).IsAssignableFrom(typeof(ColumnAttribute)))
			{
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

					return new[] { (T)(Attribute)attr };
				}
			}
			
			if (typeof(T).IsAssignableFrom(typeof(AssociationAttribute)))
			{
				if (memberInfo.DeclaringType.HasAttribute<System.Data.Linq.Mapping.TableAttribute>())
				{
					var attrs = memberInfo.GetAttributes<System.Data.Linq.Mapping.AssociationAttribute>();
					if (attrs.Length > 0)
					{
						var associations = new T[attrs.Length];

						for (var i = 0; i < attrs.Length; i++)
							associations[i] = (T)(Attribute)new AssociationAttribute { ThisKey = attrs[i].ThisKey, OtherKey = attrs[i].OtherKey, Storage = attrs[i].Storage };

						return associations;
					}
				}
			}

			return Array<T>.Empty;
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;

		public string GetObjectID() => $".{nameof(SystemDataLinqAttributeReader)}.";
	}
}
#endif
