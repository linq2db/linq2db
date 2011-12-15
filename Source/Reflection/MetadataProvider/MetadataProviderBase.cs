using System;
using System.Collections.Generic;

using LinqToDB.Common;
using LinqToDB.DataAccess;
using LinqToDB.Extensions;

namespace LinqToDB.Reflection.MetadataProvider
{
	using Extension;
	using Mapping;

	public delegate void                 OnCreateProvider(MetadataProviderBase parentProvider);
	public delegate MetadataProviderBase CreateProvider();
	public delegate MemberMapper         EnsureMapperHandler(string mapName, string origName);

	public abstract class MetadataProviderBase
	{
		#region Provider Support

		public virtual void AddProvider(MetadataProviderBase provider)
		{
		}

		public virtual void InsertProvider(int index, MetadataProviderBase provider)
		{
		}

		public virtual MetadataProviderBase[] GetProviders()
		{
			return new MetadataProviderBase[0];
		}

		#endregion

		#region GetFieldName

		public virtual string GetFieldName(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return member.Name;
		}

		#endregion

		#region GetFieldStorage

		public virtual string GetFieldStorage(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return null;
		}

		#endregion

		#region GetInheritanceDiscriminator

		public virtual bool GetInheritanceDiscriminator(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return false;
		}

		#endregion

		#region EnsureMapper

		public virtual void EnsureMapper(TypeAccessor typeAccessor, MappingSchema mappingSchema, EnsureMapperHandler handler)
		{
		}

		#endregion

		#region GetMapIgnore

		public virtual bool GetMapIgnore(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return member.Type.IsScalar() == false;
		}

		#endregion

		#region GetTrimmable

		public virtual bool GetTrimmable(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = member.Type != typeof(string);
			return isSet? false: TrimmableAttribute.Default.IsTrimmable;
		}

		#endregion

		#region GetMapValues

		public virtual MapValue[] GetMapValues(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return null;
		}

		public virtual MapValue[] GetMapValues(TypeExtension typeExtension, Type type, out bool isSet)
		{
			isSet = false;
			return null;
		}

		#endregion

		#region GetNullable

		public virtual bool GetNullable(MappingSchema mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return
				//member.Type.IsClass ||
				member.Type.IsGenericType && member.Type.GetGenericTypeDefinition() == typeof (Nullable<>)
				/*||
				member.Type == typeof(System.Data.Linq.Binary) ||
				member.Type == typeof(byte[])*/;
		}

		#endregion

		#region GetNullValue

		public virtual object GetNullValue(MappingSchema mappingSchema, TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = false;

			if (member.Type.IsEnum)
				return null;

			var value = mappingSchema.GetNullValue(member.Type);

			if (value is Type && (Type)value == typeof(DBNull))
			{
				value = DBNull.Value;

				if (member.Type == typeof(string))
					value = null;
			}

			return value;
		}

		#endregion

		#region GetDbName

		public virtual string GetDatabaseName(Type type, ExtensionList extensions, out bool isSet)
		{
			isSet = false;
			return null;
		}

		#endregion

		#region GetOwnerName

		public virtual string GetOwnerName(Type type, ExtensionList extensions, out bool isSet)
		{
			isSet = false;
			return null;
		}

		#endregion

		#region GetTableName

		public virtual string GetTableName(Type type, ExtensionList extensions, out bool isSet)
		{
			isSet = false;
			return
				type.IsInterface && type.Name.StartsWith("I")
					? type.Name.Substring(1)
					: type.Name;
		}

		#endregion

		#region GetPrimaryKeyOrder

		public virtual int GetPrimaryKeyOrder(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return 0;
		}

		#endregion

		#region GetNonUpdatableAttribute

		public virtual NonUpdatableAttribute GetNonUpdatableAttribute(Type type, TypeExtension typeExt, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return null;
		}

		#endregion

		#region GetSqlIgnore

		public virtual bool GetSqlIgnore(TypeExtension typeExtension, MemberAccessor member, out bool isSet)
		{
			isSet = false;
			return false;
		}

		#endregion

		#region GetPrimaryKeyFields

		protected static List<string> GetPrimaryKeyFields(MappingSchema schema, TypeAccessor ta, TypeExtension tex)
		{
			var mdp  = schema.MetadataProvider;
			var keys = new List<string>();

			foreach (MemberAccessor sma in ta.Members)
			{
				bool isSetFlag;

				mdp.GetPrimaryKeyOrder(ta.Type, tex, sma, out isSetFlag);

				if (isSetFlag)
				{
					var name = mdp.GetFieldName(tex, sma, out isSetFlag);
					keys.Add(name);
				}
			}

			return keys;
		}

		#endregion

		#region GetAssociation

		public virtual Association GetAssociation(TypeExtension typeExtension, MemberAccessor member)
		{
			return null;
		}

		#endregion

		#region GetInheritanceMapping

		public virtual InheritanceMappingAttribute[] GetInheritanceMapping(Type type, TypeExtension typeExtension)
		{
			return Array<InheritanceMappingAttribute>.Empty;
		}

		#endregion

		#region Static Members

		public static event OnCreateProvider OnCreateProvider;

		private static CreateProvider _createProvider = CreateInternal;
		public  static CreateProvider  CreateProvider
		{
			get { return _createProvider; }
			set { _createProvider = value ?? new CreateProvider(CreateInternal); }
		}

		private static MetadataProviderBase CreateInternal()
		{
			var list = new MetadataProviderList();

			if (OnCreateProvider != null)
				OnCreateProvider(list);

			return list;
		}

		#endregion
	}
}
