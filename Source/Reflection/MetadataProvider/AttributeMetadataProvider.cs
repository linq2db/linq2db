using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LinqToDB.Extensions;

namespace LinqToDB.Reflection.MetadataProvider
{
	using DataAccess;
	using Extension;
	using Mapping;

	public class AttributeMetadataProvider : MetadataProviderBase
	{
		#region Helpers

		private  TypeAccessor _typeAccessor;
		private  object[]     _mapFieldAttributes;
		private  object[]     _nonUpdatableAttributes;
		readonly object       _sync = new object();

		void EnsureMapper(TypeAccessor typeAccessor)
		{
			if (_typeAccessor != typeAccessor)
			{
				_typeAccessor           = typeAccessor;
				_mapFieldAttributes     = null;
				_nonUpdatableAttributes = null;
			}
		}

		object[] GetMapFieldAttributes(TypeAccessor typeAccessor)
		{
			lock (_sync)
			{
				EnsureMapper(typeAccessor);

#if NEMERLE
				return ((object[])_mapFieldAttributes) ?? (_mapFieldAttributes = (object[])typeAccessor.Type.GetAttributes<MapFieldAttribute>());
#else
				return _mapFieldAttributes ?? (_mapFieldAttributes = typeAccessor.Type.GetAttributes<MapFieldAttribute>());
#endif
			}
		}

		object[] GetNonUpdatableAttributes(TypeAccessor typeAccessor)
		{
			lock (_sync)
			{
				EnsureMapper(typeAccessor);

#if NEMERLE
				return ((object[])_nonUpdatableAttributes) ?? (_nonUpdatableAttributes = (object[])typeAccessor.Type.GetAttributes<NonUpdatableAttribute>());
#else
				return _nonUpdatableAttributes ?? (_nonUpdatableAttributes = typeAccessor.Type.GetAttributes<NonUpdatableAttribute>());
#endif
			}
		}

		#endregion

		#region GetFieldName

		public override string GetFieldName(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var a = member.GetAttribute<MapFieldAttribute>();

			if (a != null && a.MapName != null)
			{
				isSet = true;
				return a.MapName;
			}

			foreach (MapFieldAttribute attr in GetMapFieldAttributes(member.TypeAccessor))
			{
				if (attr.MapName != null && string.Equals(attr.OrigName, member.Name, StringComparison.InvariantCultureIgnoreCase))
				{
					isSet = true;
					return attr.MapName;
				}
			}

			return base.GetFieldName(typeExtension, member, out isSet);
		}

		#endregion

		#region GetFieldStorage

		public override string GetFieldStorage(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var a = member.GetAttribute<MapFieldAttribute>();

			if (a != null)
			{
				isSet = true;
				return a.Storage;
			}

			foreach (MapFieldAttribute attr in GetMapFieldAttributes(member.TypeAccessor))
			{
				if (string.Equals(attr.OrigName, member.Name, StringComparison.InvariantCultureIgnoreCase))
				{
					isSet = true;
					return attr.Storage;
				}
			}

			return base.GetFieldStorage(typeExtension, member, out isSet);
		}

		#endregion

		#region GetInheritanceDiscriminator

		public override bool GetInheritanceDiscriminator(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var a = member.GetAttribute<MapFieldAttribute>();

			if (a != null)
			{
				isSet = true;
				return a.IsInheritanceDiscriminator;
			}

			foreach (MapFieldAttribute attr in GetMapFieldAttributes(member.TypeAccessor))
			{
				if (string.Equals(attr.OrigName, member.Name, StringComparison.InvariantCultureIgnoreCase))
				{
					isSet = true;
					return attr.IsInheritanceDiscriminator;
				}
			}

			return base.GetInheritanceDiscriminator(typeExtension, member, out isSet);
		}

		#endregion

		#region EnsureMapper

		public override void EnsureMapper(TypeAccessor typeAccessor, MappingSchemaOld mappingSchema, EnsureMapperHandler handler)
		{
			foreach (MapFieldAttribute attr in GetMapFieldAttributes(typeAccessor))
			{
				if (attr.OrigName != null)
					handler(attr.MapName, attr.OrigName);
				else
				{
					var ma = typeAccessor[attr.MapName];

					foreach (MemberMapper inner in mappingSchema.GetObjectMapper(ma.Type))
						handler(string.Format(attr.Format, inner.Name), ma.Name + "." + inner.MemberName);
				}
			}
		}

		#endregion

		#region GetMapIgnore

		public override bool GetMapIgnore(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var attr = member.GetAttribute<MapIgnoreAttribute>() ?? member.Type.GetFirstAttribute<MapIgnoreAttribute>();

			if (attr != null)
			{
				isSet = true;
				return attr.Ignore;
			}

			if (member.GetAttribute<MapFieldAttribute>()    != null ||
				member.GetAttribute<MapImplicitAttribute>() != null ||
				member.Type.GetFirstAttribute<MapImplicitAttribute>() != null)
			{
				isSet = true;
				return false;
			}

			return base.GetMapIgnore(typeExtension, member, out isSet) || member.GetAttribute<AssociationAttribute>() != null;
		}

		#endregion

		#region GetTrimmable

		public override bool GetTrimmable(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			if (member.Type == typeof(string))
			{
				var attr = member.GetAttribute<TrimmableAttribute>();

				if (attr != null)
				{
					isSet = true;
					return attr.IsTrimmable;
				}

				attr = member.MemberInfo.DeclaringType.GetFirstAttribute<TrimmableAttribute>();

				if (attr != null)
				{
					isSet = true;
					return attr.IsTrimmable;
				}
			}

			return base.GetTrimmable(typeExtension, member, out isSet);
		}

		#endregion

		#region GetMapValues

		public override MapValue[] GetMapValues(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			List<MapValue> list = null;

			var attrs = member.GetAttributes<MapValueAttribute>();

			if (attrs != null)
			{
				list = new List<MapValue>(attrs.Length);

				foreach (MapValueAttribute a in attrs)
					list.Add(new MapValue(a.OrigValue, a.Values));
			}

			attrs = member.GetTypeAttributes<MapValueAttribute>();

			if (attrs != null && attrs.Length > 0)
			{
				if (list == null)
					list = new List<MapValue>(attrs.Length);

				foreach (MapValueAttribute a in attrs)
					if (a.Type == null && a.OrigValue != null && a.OrigValue.GetType() == member.Type ||
						a.Type is Type && (Type)a.Type == member.Type)
						list.Add(new MapValue(a.OrigValue, a.Values));
			}

			var typeMapValues = GetMapValues(typeExtension, member.Type, out isSet);

			if (list == null)
				return typeMapValues;

			if (typeMapValues != null)
				list.AddRange(typeMapValues);

			isSet = true;

			return list.ToArray();
		}

		const FieldAttributes EnumField = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal;

		static List<MapValue> GetEnumMapValues(Type type)
		{
			var list   = null as List<MapValue>;
			var fields = type.GetFields();

			foreach (var fi in fields)
			{
				if ((fi.Attributes & EnumField) == EnumField)
				{
					var enumAttributes = Attribute.GetCustomAttributes(fi, typeof(MapValueAttribute));

					foreach (MapValueAttribute attr in enumAttributes)
					{
						if (list == null)
							list = new List<MapValue>(fields.Length);

						var origValue = Enum.Parse(type, fi.Name, false);

						list.Add(new MapValue(origValue, attr.Values));
					}
				}
			}

			return list;
		}

		public override MapValue[] GetMapValues(TypeExtension typeExtension, Type type, out bool isSet)
		{
			List<MapValue> list = null;

			if (type.IsNullable())
				type = type.GetGenericArguments()[0];

			if (type.IsEnum)
				list = GetEnumMapValues(type);

			var attrs = type.GetAttributes<MapValueAttribute>();

			if (attrs != null && attrs.Length != 0)
			{
				if (list == null)
					list = new List<MapValue>(attrs.Length);

				for (var i = 0; i < attrs.Length; i++)
				{
					var a = attrs[i];
					list.Add(new MapValue(a.OrigValue, a.Values));
				}
			}

			isSet = list != null;

			return isSet? list.ToArray(): null;
		}

		#endregion

		#region GetNullable

		public override bool GetNullable(MappingSchemaOld mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			// Check member [Nullable(true | false)]
			//
			var attr1 = member.GetAttribute<NullableAttribute>();

			if (attr1 != null)
			{
				isSet = true;
				return attr1.IsNullable;
			}

			// Check member [NullValue(0)]
			//
			var attr2 = member.GetAttribute<NullValueAttribute>();

			if (attr2 != null)
				return isSet = true;

			// Check type [Nullable(true || false)]
			//
			attr1 = member.MemberInfo.DeclaringType.GetFirstAttribute<NullableAttribute>();

			if (attr1 != null)
			{
				isSet = true;
				return attr1.IsNullable;
			}

			// Check type [NullValues(typeof(int), 0)]
			//
			var attrs = member.GetTypeAttributes<NullValueAttribute>();

			foreach (NullValueAttribute a in attrs)
				if (a.Type == null && a.Value != null && a.Value.GetType() == member.Type ||
					a.Type != null && a.Type == member.Type)
					return isSet = true;

			if (member.Type.IsEnum)
				return isSet = mappingSchema.GetNullValue(member.Type) != null;

			if (member.Type.IsClass)
			{
				var pk = member.GetAttribute<PrimaryKeyAttribute>();

				if (pk != null)
				{
					isSet = false;
					return false;
				}
			}

			return base.GetNullable(mappingSchema, typeExtension, member, out isSet);
		}

		#endregion

		#region GetNullValue

		private static object CheckNullValue(object value, MemberAccessor member)
		{
			if (value is Type && (Type)value == typeof(DBNull))
			{
				value = DBNull.Value;

				if (member.Type == typeof(string))
					value = null;
			}

			return value;
		}

		public override object GetNullValue(MappingSchemaOld mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			// Check member [NullValue(0)]
			//
			var attr = member.GetAttribute<NullValueAttribute>();

			if (attr != null)
			{
				isSet = true;
				return CheckNullValue(attr.Value, member);
			}

			// Check type [NullValues(typeof(int), 0)]
			//
			var attrs = member.GetTypeAttributes<NullValueAttribute>();

			foreach (NullValueAttribute a in attrs)
			{
				if (a.Type == null && a.Value != null && a.Value.GetType() == member.Type ||
					a.Type != null && a.Type == member.Type)
				{
					isSet = true;
					return CheckNullValue(a.Value, member);
				}
			}

			if (member.Type.IsEnum)
			{
				var value = CheckNullValue(mappingSchema.GetNullValue(member.Type), member);

				if (value != null)
				{
					isSet = true;
					return value;
				}
			}

			isSet = false;
			return null;
		}

		#endregion

		#region GetDbName

		public override string GetDatabaseName(Type type, ExtensionList extensions, out bool isSet)
		{
			var attrs = type.GetCustomAttributes(typeof(TableNameAttribute), true);

			if (attrs.Length > 0)
			{
				var name = ((TableNameAttribute)attrs[0]).Database;
				isSet = name != null;
				return name;
			}

			return base.GetDatabaseName(type, extensions, out isSet);
		}

		#endregion

		#region GetTableName

		public override string GetOwnerName(Type type, ExtensionList extensions, out bool isSet)
		{
			var attrs = type.GetCustomAttributes(typeof(TableNameAttribute), true);

			if (attrs.Length > 0)
			{
				var name = ((TableNameAttribute)attrs[0]).Owner;
				isSet = name != null;
				return name;
			}

			return base.GetOwnerName(type, extensions, out isSet);
		}

		#endregion

		#region GetTableName

		public override string GetTableName(Type type, ExtensionList extensions, out bool isSet)
		{
			var attrs = type.GetCustomAttributes(typeof(TableNameAttribute), true);

			if (attrs.Length > 0)
			{
				var name = ((TableNameAttribute)attrs[0]).Name;
				isSet = name != null;
				return name;
			}

			return base.GetTableName(type, extensions, out isSet);
		}

		#endregion

		#region GetPrimaryKeyOrder

		public override int GetPrimaryKeyOrder(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			var attr = member.GetAttribute<PrimaryKeyAttribute>();

			if (attr != null)
			{
				isSet = true;
				return attr.Order;
			}

			return base.GetPrimaryKeyOrder(type, typeExt, member, out isSet);
		}

		#endregion

		#region GetNonUpdatableFlag

		public override NonUpdatableAttribute GetNonUpdatableAttribute(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			var attr = member.GetAttribute<NonUpdatableAttribute>();

			if (attr != null)
			{
				isSet = true;
				return attr;
			}

			foreach (NonUpdatableAttribute a in GetNonUpdatableAttributes(member.TypeAccessor))
			{
				if (string.Equals(a.FieldName, member.Name, StringComparison.InvariantCultureIgnoreCase))
				{
					isSet = true;
					return a;
				}
			}

			return base.GetNonUpdatableAttribute(type, typeExt, member, out isSet);
		}

		#endregion

		#region GetSqlIgnore

		public override bool GetSqlIgnore(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var attr = member.GetAttribute<SqlIgnoreAttribute>() ?? member.Type.GetFirstAttribute<SqlIgnoreAttribute>();

			if (attr != null)
			{
				isSet = true;
				return attr.Ignore;
			}

			return base.GetSqlIgnore(typeExtension, member, out isSet);
		}

		#endregion

		#region GetAssociation

		public override Association GetAssociation(TypeExtension typeExtension, MemberAccessor member)
		{
			var aa = member.GetAttribute<AssociationAttribute>();

			if (aa == null)
				return base.GetAssociation(typeExtension, member);

			return new Association(
				member,
				aa.GetThisKeys(),
				aa.GetOtherKeys(),
				aa.Storage,
				aa.CanBeNull);
		}

		#endregion

		#region GetInheritanceMapping

		public override InheritanceMappingAttribute[] GetInheritanceMapping(Type type, TypeExtension typeExtension)
		{
			var attrs = type.GetCustomAttributes(typeof(InheritanceMappingAttribute), true);

			if (attrs.Length > 0)
			{
				var maps = new InheritanceMappingAttribute[attrs.Length];

				for (var i = 0; i < attrs.Length; i++)
					maps[i] = (InheritanceMappingAttribute)attrs[i];

				return maps;
			}

			return base.GetInheritanceMapping(type, typeExtension);
		}

		#endregion
	}
}
