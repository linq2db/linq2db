using System;
using System.Collections.Generic;

namespace LinqToDB.Reflection.MetadataProvider
{
	using Extension;
	using Mapping;

	public class MetadataProviderList : MetadataProviderBase
	{
		#region Init

		public MetadataProviderList()
		{
			AddProvider(new ExtensionMetadataProvider());
			AddProvider(new AttributeMetadataProvider());
#if !SILVERLIGHT
			AddProvider(new LinqMetadataProvider());
#endif
		}

		private readonly List<MetadataProviderBase> _list = new List<MetadataProviderBase>(3);

		#endregion

		#region Provider Support

		public override void AddProvider(MetadataProviderBase provider)
		{
			_list.Add(provider);
		}

		public override void InsertProvider(int index, MetadataProviderBase provider)
		{
			_list.Insert(index, provider);
		}

		public override MetadataProviderBase[] GetProviders()
		{
			return _list.ToArray();
		}

		#endregion

		#region GetFieldName

		public override string GetFieldName(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var name = p.GetFieldName(typeExtension, member, out isSet);

				if (isSet)
					return name;
			}

			return base.GetFieldName(typeExtension, member, out isSet);
		}

		#endregion

		#region GetFieldStorage

		public override string GetFieldStorage(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var name = p.GetFieldStorage(typeExtension, member, out isSet);

				if (isSet)
					return name;
			}

			return base.GetFieldStorage(typeExtension, member, out isSet);
		}

		#endregion

		#region GetInheritanceDiscriminator

		public override bool GetInheritanceDiscriminator(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetInheritanceDiscriminator(typeExtension, member, out isSet);

				if (isSet)
					return value;
			}

			return base.GetInheritanceDiscriminator(typeExtension, member, out isSet);
		}

		#endregion

		#region EnsureMapper

		public override void EnsureMapper(TypeAccessor typeAccessor, MappingSchemaOld mappingSchema, EnsureMapperHandler handler)
		{
			foreach (var p in _list)
				p.EnsureMapper(typeAccessor, mappingSchema, handler);
		}

		#endregion

		#region GetMapIgnore

		public override bool GetMapIgnore(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var ignore = p.GetMapIgnore(typeExtension, member, out isSet);

				if (isSet)
					return ignore;
			}

			return base.GetMapIgnore(typeExtension, member, out isSet);
		}

		#endregion

		#region GetTrimmable

		public override bool GetTrimmable(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			if (member.Type == typeof(string))
			{
				foreach (var p in _list)
				{
					var trimmable = p.GetTrimmable(typeExtension, member, out isSet);

					if (isSet)
						return trimmable;
				}
			}

			return base.GetTrimmable(typeExtension, member, out isSet);
		}

		#endregion

		#region GetMapValues

		public override MapValue[] GetMapValues(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetMapValues(typeExtension, member, out isSet);

				if (isSet)
					return value;
			}

			return base.GetMapValues(typeExtension, member, out isSet);
		}

		public override MapValue[] GetMapValues(TypeExtension typeExt, Type type, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetMapValues(typeExt, type, out isSet);

				if (isSet)
					return value;
			}

			return base.GetMapValues(typeExt, type, out isSet);
		}

		#endregion

		#region GetNullable

		public override bool GetNullable(MappingSchemaOld mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetNullable(mappingSchema, typeExtension, member, out isSet);

				if (isSet)
					return value;
			}

			return base.GetNullable(mappingSchema, typeExtension, member, out isSet);
		}

		#endregion

		#region GetNullValue

		public override object GetNullValue(MappingSchemaOld mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetNullValue(mappingSchema, typeExtension, member, out isSet);

				if (isSet)
					return value;
			}

			return base.GetNullValue(mappingSchema, typeExtension, member, out isSet);
		}

		#endregion

		#region GetDbName

		public override string GetDatabaseName(Type type, ExtensionList extensions, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetDatabaseName(type, extensions, out isSet);

				if (isSet)
					return value;
			}

			return base.GetDatabaseName(type, extensions, out isSet);
		}

		#endregion

		#region GetOwnerName

		public override string GetOwnerName(Type type, ExtensionList extensions, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetOwnerName(type, extensions, out isSet);

				if (isSet)
					return value;
			}

			return base.GetOwnerName(type, extensions, out isSet);
		}

		#endregion

		#region GetTableName

		public override string GetTableName(Type type, ExtensionList extensions, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetTableName(type, extensions, out isSet);

				if (isSet)
					return value;
			}

			return base.GetTableName(type, extensions, out isSet);
		}

		#endregion

		#region GetPrimaryKeyOrder

		public override int GetPrimaryKeyOrder(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetPrimaryKeyOrder(type, typeExt, member, out isSet);

				if (isSet)
					return value;
			}

			return base.GetPrimaryKeyOrder(type, typeExt, member, out isSet);
		}

		#endregion

		#region GetNonUpdatableFlag

		public override NonUpdatableAttribute GetNonUpdatableAttribute(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			foreach (var p in _list)
			{
				var value = p.GetNonUpdatableAttribute(type, typeExt, member, out isSet);

				if (isSet)
					return value;
			}

			return base.GetNonUpdatableAttribute(type, typeExt, member, out isSet);
		}

		#endregion

		#region GetAssociation

		public override Association GetAssociation(TypeExtension typeExtension, MemberAccessor member)
		{
			foreach (var p in _list)
			{
				var attr = p.GetAssociation(typeExtension, member);

				if (attr != null)
					return attr;
			}

			return base.GetAssociation(typeExtension, member);
		}

		#endregion

		#region GetInheritanceMapping

		public override InheritanceMappingAttribute[] GetInheritanceMapping(Type type, TypeExtension typeExtension)
		{
			foreach (var p in _list)
			{
				var attrs = p.GetInheritanceMapping(type, typeExtension);

				if (attrs.Length > 0)
					return attrs;
			}

			return base.GetInheritanceMapping(type, typeExtension);
		}

		#endregion
	}
}
