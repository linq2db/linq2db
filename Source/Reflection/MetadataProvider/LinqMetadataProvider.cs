using System;
using System.Linq;

namespace LinqToDB.Reflection.MetadataProvider
{
	using Mapping;
	using Extension;

	public class LinqMetadataProvider : MetadataProviderBase
	{
		#region Helpers

		private  Type   _type;
		private  bool?  _isLinqObject;
		readonly object _sync = new object();

		void EnsureMapper(Type type)
		{
			if (_type != type)
			{
				_type         = type;
				_isLinqObject = null;
			}
		}

		bool IsLinqObject(Type type)
		{
			lock (_sync)
			{
				EnsureMapper(type);

				if (_isLinqObject == null)
				{
					var attrs = type.GetCustomAttributes(typeof(System.Data.Linq.Mapping.TableAttribute), true);
					_isLinqObject = attrs.Length > 0;
				}

				return _isLinqObject.Value;
			}
		}

		#endregion

		#region GetFieldName

		public override string GetFieldName(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			if (IsLinqObject(member.TypeAccessor.Type))
			{
				var a = member.GetAttribute<System.Data.Linq.Mapping.ColumnAttribute>();

				if (a != null && !string.IsNullOrEmpty(a.Name))
				{
					isSet = true;
					return a.Name;
				}
			}

			return base.GetFieldName(typeExtension, member, out isSet);
		}

		#endregion

		#region GetFieldStorage

		public override string GetFieldStorage(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			if (IsLinqObject(member.TypeAccessor.Type))
			{
				var a = member.GetAttribute<System.Data.Linq.Mapping.ColumnAttribute>();

				if (a != null && !string.IsNullOrEmpty(a.Name))
				{
					isSet = true;
					return a.Storage;
				}
			}

			return base.GetFieldStorage(typeExtension, member, out isSet);
		}

		#endregion

		#region GetInheritanceDiscriminator

		public override bool GetInheritanceDiscriminator(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			if (IsLinqObject(member.TypeAccessor.Type))
			{
				var a = member.GetAttribute<System.Data.Linq.Mapping.ColumnAttribute>();

				if (a != null && !string.IsNullOrEmpty(a.Name))
				{
					isSet = true;
					return a.IsDiscriminator;
				}
			}

			return base.GetInheritanceDiscriminator(typeExtension, member, out isSet);
		}

		#endregion

		#region GetMapIgnore

		public override bool GetMapIgnore(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			if (member.GetAttribute<System.Data.Linq.Mapping.AssociationAttribute>() != null)
			{
				isSet = true;
				return true;
			}

			if (IsLinqObject(member.TypeAccessor.Type))
			{
				isSet = true;
				return member.GetAttribute<System.Data.Linq.Mapping.ColumnAttribute>() == null;
			}

			return base.GetMapIgnore(typeExtension, member, out isSet);
		}

		#endregion

		#region GetNullable

		public override bool GetNullable(MappingSchemaOld mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			if (IsLinqObject(member.TypeAccessor.Type))
			{
				var attr = member.GetAttribute<System.Data.Linq.Mapping.ColumnAttribute>();

				if (attr != null)
				{
					isSet = true;
					return attr.CanBeNull;
				}
			}

			return base.GetNullable(mappingSchema, typeExtension, member, out isSet);
		}

		#endregion

		#region GetTableName

		public override string GetTableName(Type type, ExtensionList extensions, out bool isSet)
		{
			if (IsLinqObject(type))
			{
				isSet = true;

				var attrs = type.GetCustomAttributes(typeof(System.Data.Linq.Mapping.TableAttribute), true);

				return ((System.Data.Linq.Mapping.TableAttribute)attrs[0]).Name;
			}

			return base.GetTableName(type, extensions, out isSet);
		}

		#endregion

		#region GetPrimaryKeyOrder

		public override int GetPrimaryKeyOrder(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			if (IsLinqObject(type))
			{
				var a = member.GetAttribute<System.Data.Linq.Mapping.ColumnAttribute>();

				if (a != null && a.IsPrimaryKey)
				{
					isSet = true;
					return 0;
				}
			}

			return base.GetPrimaryKeyOrder(type, typeExt, member, out isSet);
		}

		#endregion

		#region GetNonUpdatableFlag

		public override NonUpdatableAttribute GetNonUpdatableAttribute(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			if (IsLinqObject(member.TypeAccessor.Type))
			{
				var a = member.GetAttribute<System.Data.Linq.Mapping.ColumnAttribute>();

				if (a != null)
				{
					isSet = true;
					return a.IsDbGenerated ? new IdentityAttribute() : null;
				}
			}

			return base.GetNonUpdatableAttribute(type, typeExt, member, out isSet);
		}

		#endregion

		#region GetInheritanceMapping

		public override LinqToDB.Mapping.InheritanceMappingAttribute[] GetInheritanceMapping(Type type, TypeExtension typeExtension)
		{
			if (IsLinqObject(type))
			{
				var attrs = type.GetCustomAttributes(typeof(LinqToDB.Mapping.InheritanceMappingAttribute), true);

				if (attrs.Length > 0)
					return attrs.Select(a => (LinqToDB.Mapping.InheritanceMappingAttribute)a).ToArray();
			}

			return base.GetInheritanceMapping(type, typeExtension);
		}

		#endregion
	}
}
