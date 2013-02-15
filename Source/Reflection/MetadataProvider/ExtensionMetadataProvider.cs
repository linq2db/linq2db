using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Extensions;
using Convert=System.Convert;

namespace LinqToDB.Reflection.MetadataProvider
{
	using Extension;
	using Mapping;

	public class ExtensionMetadataProvider : MetadataProviderBase
	{
		#region Helpers

		private static object GetValue(TypeExtension typeExtension, MemberAccessor member, string elemName, out bool isSet)
		{
			var value = typeExtension[member.Name][elemName].Value;

			isSet = value != null;

			return value;
		}

		#endregion

		#region GetFieldName

		public override string GetFieldName(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var value = GetValue(typeExtension, member, "MapField", out isSet);

			if (value != null)
				return value.ToString();

			return base.GetFieldName(typeExtension, member, out isSet);
		}

		#endregion

		#region GetFieldStorage

		public override string GetFieldStorage(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var value = GetValue(typeExtension, member, "FieldStorage", out isSet);

			if (value != null)
				return value.ToString();

			return base.GetFieldStorage(typeExtension, member, out isSet);
		}

		#endregion

		#region GetInheritanceDiscriminator

		public override bool GetInheritanceDiscriminator(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var value = GetValue(typeExtension, member, "IsInheritanceDiscriminator", out isSet);

			if (value != null)
				return TypeExtension.ToBoolean(value);

			return base.GetInheritanceDiscriminator(typeExtension, member, out isSet);
		}

		#endregion

		#region GetMapIgnore

		public override bool GetMapIgnore(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			var value = GetValue(typeExtension, member, "MapIgnore", out isSet);

			if (value != null)
				return TypeExtension.ToBoolean(value);

			return base.GetMapIgnore(typeExtension, member, out isSet);
		}

		#endregion

		#region GetTrimmable

		public override bool GetTrimmable(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			if (member.Type == typeof(string))
			{
				var value = GetValue(typeExtension, member, "Trimmable", out isSet);

				if (value != null)
					return TypeExtension.ToBoolean(value);
			}

			return base.GetTrimmable(typeExtension, member, out isSet);
		}

		#endregion

		#region GetNullable

		public override bool GetNullable(MappingSchemaOld mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			// Check extension <Member1 Nullable='true' />
			//
			var value = GetValue(typeExtension, member, "Nullable", out isSet);

			if (isSet)
				return TypeExtension.ToBoolean(value);

			// Check extension <Member1 NullValue='-1' />
			//
			if (GetValue(typeExtension, member, "NullValue", out isSet) != null)
				return true;

			return base.GetNullable(mappingSchema, typeExtension, member, out isSet);
		}

		#endregion

		#region GetNullable

		public override object GetNullValue(MappingSchemaOld mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			// Check extension <Member1 NullValue='-1' />
			//
			var value = GetValue(typeExtension, member, "NullValue", out isSet);

			return isSet? TypeExtension.ChangeType(value, member.Type): null;
		}

		#endregion

		#region GetDbName

		public override string GetDatabaseName(Type type, ExtensionList extensions, out bool isSet)
		{
			var typeExt = TypeExtension.GetTypeExtension(type, extensions);
			var value   = typeExt.Attributes["DatabaseName"].Value;

			if (value != null)
			{
				isSet = true;
				return value.ToString();
			}

			return base.GetDatabaseName(type, extensions, out isSet);
		}

		#endregion

		#region GetOwnerName

		public override string GetOwnerName(Type type, ExtensionList extensions, out bool isSet)
		{
			var typeExt = TypeExtension.GetTypeExtension(type, extensions);
			var value   = typeExt.Attributes["OwnerName"].Value;

			if (value != null)
			{
				isSet = true;
				return value.ToString();
			}

			return base.GetOwnerName(type, extensions, out isSet);
		}

		#endregion

		#region GetTableName

		public override string GetTableName(Type type, ExtensionList extensions, out bool isSet)
		{
			var typeExt = TypeExtension.GetTypeExtension(type, extensions);
			var value   = typeExt.Attributes["TableName"].Value;

			if (value != null)
			{
				isSet = true;
				return value.ToString();
			}

			return base.GetTableName(type, extensions, out isSet);
		}

		#endregion

		#region GetPrimaryKeyOrder

		public override int GetPrimaryKeyOrder(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			var value = typeExt[member.Name]["PrimaryKey"].Value;

			if (value != null)
			{
				isSet = true;
				return (int)TypeExtension.ChangeType(value, typeof(int));
			}

			return base.GetPrimaryKeyOrder(type, typeExt, member, out isSet);
		}

		#endregion

		#region GetNonUpdatableFlag

		public override NonUpdatableAttribute GetNonUpdatableAttribute(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			var value = typeExt[member.Name]["NonUpdatable"].Value;

			if (value != null)
			{
				isSet = true;
				return (bool)TypeExtension.ChangeType(value, typeof(bool)) ? new NonUpdatableAttribute() : null;
			}

			value = typeExt[member.Name]["Identity"].Value;

			if (value != null)
			{
				isSet = true;
				return (bool)TypeExtension.ChangeType(value, typeof(bool)) ? new IdentityAttribute() : null;
			}

			return base.GetNonUpdatableAttribute(type, typeExt, member, out isSet);
		}

		#endregion

		#region GetInheritanceMapping

		public override InheritanceMappingAttribute[] GetInheritanceMapping(Type type, TypeExtension typeExtension)
		{
			var extList = typeExtension.Attributes["InheritanceMapping"];

			if (extList == AttributeExtensionCollection.Null)
				return Array<InheritanceMappingAttribute>.Empty;

			var attrs = new InheritanceMappingAttribute[extList.Count];

			for (var i = 0; i < extList.Count; i++)
			{
				var ext = extList[i];

				attrs[i] = new InheritanceMappingAttribute
				{
					Code      = ext["Code"],
					IsDefault = TypeExtension.ToBoolean(ext["IsDefault", "False"], false),
					Type      = Type.GetType(Convert.ToString(ext["Type"]))
				};
			}

			return attrs;
		}

		#endregion
	}
}
