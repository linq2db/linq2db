using System;
using System.Linq;

namespace LinqToDB.Reflection.MetadataProvider
{
	using Extension;
	using Extensions;
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
				return _mapFieldAttributes ?? (_mapFieldAttributes = typeAccessor.Type.GetAttributes<MapFieldAttribute>());
			}
		}

		object[] GetNonUpdatableAttributes(TypeAccessor typeAccessor)
		{
			lock (_sync)
			{
				EnsureMapper(typeAccessor);
				return _nonUpdatableAttributes ?? (_nonUpdatableAttributes = typeAccessor.Type.GetAttributes<NonUpdatableAttribute>());
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

			foreach (var a in attrs)
				if (a.Type == null && a.Value != null && a.Value.GetType() == member.Type ||
					a.Type != null && a.Type == member.Type)
					return isSet = true;

			if (member.Type.IsEnum)
			{
				var values = mappingSchema.NewSchema.GetMapValues(member.Type);
				return isSet = values != null && values.Any(v => v.MapValues.Any(m => m.Value == null));
			}

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
