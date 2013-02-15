using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace LinqToDB.Mapping
{
	using Reflection;
	using Reflection.Extension;
	using Reflection.MetadataProvider;

	[DebuggerDisplay("Type = {TypeAccessor.Type}, OriginalType = {TypeAccessor.OriginalType}")]
	public class ObjectMapper : IEnumerable<MemberMapper>
	{
		#region Protected Members

		protected virtual MemberMapper CreateMemberMapper(MapMemberInfo mapMemberInfo)
		{
			if (mapMemberInfo == null) throw new ArgumentNullException("mapMemberInfo");

			MemberMapper mm = MemberMapper.CreateMemberMapper(mapMemberInfo);

			mm.Init(mapMemberInfo);

			return mm;
		}

		protected virtual void Add(MemberMapper memberMapper)
		{
			if (memberMapper == null) throw new ArgumentNullException("memberMapper");

			memberMapper.SetOrdinal(_members.Count);

			_members           .Add(memberMapper);
			_nameToMember      .Add(memberMapper.Name.ToLower(), memberMapper);
			_memberNameToMember.Add(memberMapper.MemberName,     memberMapper);
		}

		protected virtual MetadataProviderBase CreateMetadataProvider()
		{
			return MetadataProviderBase.CreateProvider();
		}

		#endregion

		#region Public Members

		private readonly List<MemberMapper> _members = new List<MemberMapper>();
		public  MemberMapper this[int index]
		{
			get { return _members[index]; }
		}

		readonly List<AssociationDescriptor> _associations = new List<AssociationDescriptor>();
		public   List<AssociationDescriptor>  Associations
		{
			get { return _associations; }
		}

		readonly List<InheritanceMappingAttribute> _inheritanceMapping = new List<InheritanceMappingAttribute>();
		public   List<InheritanceMappingAttribute>  InheritanceMapping
		{
			get { return _inheritanceMapping; }
		}

		private TypeExtension _extension;
		public  TypeExtension  Extension
		{
			get { return _extension;  }
			set { _extension = value; }
		}

		private MetadataProviderBase _metadataProvider;
		public  MetadataProviderBase  MetadataProvider
		{
			get { return _metadataProvider ?? (_metadataProvider = CreateMetadataProvider()); }
			set { _metadataProvider = value; }
		}

		private string[] _fieldNames;
		public  string[]  FieldNames
		{
			get
			{
				if (_fieldNames == null)
				{
					_fieldNames = new string[_members.Count];

					for (var i = 0; i < _fieldNames.Length; i++)
					{
						_fieldNames[i] = _members[i].Name;
					}
				}

				return _fieldNames;
			}
		}

		private readonly Dictionary<string,MemberMapper> _nameToMember       = new Dictionary<string,MemberMapper>();
		private readonly Dictionary<string,MemberMapper> _memberNameToMember = new Dictionary<string,MemberMapper>();
		public  MemberMapper this[string name]
		{
			get
			{
				if (name == null) throw new ArgumentNullException("name");

				lock (_nameToMember)
				{
					MemberMapper mm;

					if (!_nameToMember.TryGetValue(name, out mm))
					{
						if (!_nameToMember.TryGetValue(name.ToLower(CultureInfo.CurrentCulture), out mm))
						{
							lock (_memberNameToMember)
								if (_memberNameToMember.ContainsKey(name) || _memberNameToMember.ContainsKey(name.ToLower(CultureInfo.CurrentCulture)))
									return null;

							mm = GetComplexMapper(name, name);

							if (mm != null)
							{
								if (_members.Contains(mm))
								{
									//throw new MappingException(string.Format(
									//    "Wrong mapping field name: '{0}', type: '{1}'. Use field name '{2}' instead.",
									//    name, _typeAccessor.OriginalType.Name, mm.Name));
									return null;
								}

								Add(mm);
							}
						}
						else
							_nameToMember.Add(name, mm);
					}

					return mm;
					
				}
			}
		}

		public MemberMapper this[string name, bool byPropertyName]
		{
			get
			{
				MemberMapper mm;

				if (byPropertyName)
					lock (_memberNameToMember)
						return _memberNameToMember.TryGetValue(name, out mm) ? mm : null;

				return this[name];
			}
		}

		internal EntityDescriptor EntityDescriptor { get; private set; }
		public   TypeAccessor     TypeAccessor     { get; private set; }
		public   MappingSchemaOld MappingSchema    { get; private set; }

		#endregion

		#region Init Mapper

		public virtual void Init(MappingSchemaOld mappingSchema, Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			TypeAccessor     = TypeAccessor.GetAccessor(type);
			MappingSchema    = mappingSchema;
			EntityDescriptor = new EntityDescriptor(mappingSchema.NewSchema, type);
			_extension       = TypeExtension.GetTypeExtension(TypeAccessor.Type, mappingSchema.Extensions);

			_inheritanceMapping.AddRange(GetInheritanceMapping());

			foreach (var ma in TypeAccessor.Members)
			{
				var aa = mappingSchema.NewSchema.GetAttribute<AssociationAttribute>(ma.MemberInfo, attr => attr.Configuration);

				if (aa != null)
				{
					_associations.Add(new AssociationDescriptor(type, ma.MemberInfo, aa.GetThisKeys(), aa.GetOtherKeys(), aa.Storage, aa.CanBeNull));
					continue;
				}

				if (GetMapIgnore(ma))
					continue;

				var mapFieldAttr = ma.GetAttribute<MapFieldAttribute>();

				if (mapFieldAttr == null || (mapFieldAttr.OrigName == null && mapFieldAttr.Format == null))
				{
					var mi = new MapMemberInfo();

					var dbTypeAttribute = ma.GetAttribute<DbTypeAttribute>();

					if (dbTypeAttribute != null)
					{
						mi.DbType      = dbTypeAttribute.DbType;
						mi.IsDbTypeSet = true;

						if (dbTypeAttribute.Size != null)
						{
							mi.DbSize      = dbTypeAttribute.Size.Value;
							mi.IsDbSizeSet = true;
						}
					}

					mi.MemberAccessor             = ma;
					mi.Type                       = ma.Type;
					mi.MappingSchema              = mappingSchema;
					mi.MemberExtension            = _extension[ma.Name];
					mi.Name                       = GetFieldName   (ma);
					mi.MemberName                 = ma.Name;
					mi.Storage                    = GetFieldStorage(ma);
					mi.IsInheritanceDiscriminator = GetInheritanceDiscriminator(ma);
					mi.Trimmable                  = GetTrimmable   (ma);
					mi.MapValues                  = GetMapValues   (ma);
					mi.Nullable                   = GetNullable    (ma);
					mi.NullValue                  = GetNullValue   (ma, mi.Nullable);

					Add(CreateMemberMapper(mi));
				}
				else if (mapFieldAttr.OrigName != null)
				{
					EnsureMapper(mapFieldAttr.MapName, ma.Name + "." + mapFieldAttr.OrigName);
				}
				else //if (mapFieldAttr.Format != null)
				{
					foreach (MemberMapper inner in MappingSchema.GetObjectMapper(ma.Type))
						EnsureMapper(string.Format(mapFieldAttr.Format, inner.Name), ma.Name + "." + inner.MemberName);
				}
			}

			foreach (var ae in _extension.Attributes["MapField"])
			{
				var mapName  = (string)ae["MapName"];
				var origName = (string)ae["OrigName"];

				if (mapName == null || origName == null)
					throw new MappingException(string.Format(
						"Type '{0}' has invalid  extension. MapField MapName='{1}' OrigName='{2}'.",
							type.FullName, mapName, origName));

				EnsureMapper(mapName, origName);
			}

			MetadataProvider.EnsureMapper(TypeAccessor, MappingSchema, EnsureMapper);
		}

		private MemberMapper EnsureMapper(string mapName, string origName)
		{
			var mm = this[mapName];

			if (mm == null)
			{
				var name = mapName.ToLower();

				foreach (var m in _members)
				{
					if (m.MemberAccessor.Name.ToLower() == name)
					{
						_nameToMember.Add(name, m);
						return m;
					}
				}

				mm = GetComplexMapper(mapName, origName);

				if (mm != null)
					Add(mm);
			}

			return mm;
		}

		private readonly Dictionary<string,MemberMapper> _nameToComplexMapper = new Dictionary<string,MemberMapper>();

		protected MemberMapper GetComplexMapper(string mapName, string origName)
		{
			if (origName == null) throw new ArgumentNullException("origName");

			var name = origName.ToLower();
			var idx  = origName.IndexOf('.');

			lock (_nameToComplexMapper)
			{
				MemberMapper mm;

				if (_nameToComplexMapper.TryGetValue(name, out mm))
					return mm;

				if (idx > 0)
				{
					name = name.Substring(0, idx);

					foreach (var ma in TypeAccessor.Members)
					{
						if (ma.Name.Length == name.Length && ma.Name.ToLower() == name)
						{
							var om = MappingSchema.GetObjectMapper(ma.Type);

							if (om != null)
							{
								mm = om.GetComplexMapper(mapName, origName.Substring(idx + 1));

								if (mm != null)
								{
									var mi = new MapMemberInfo
									{
										MemberAccessor        = ma,
										ComplexMemberAccessor = mm.ComplexMemberAccessor,
										Type                  = mm.Type,
										MappingSchema         = MappingSchema,
										Name                  = mapName,
										MemberName            = origName
									};

									var mapper = new MemberMapper.ComplexMapper(mm);
									var key    = origName.ToLower();

									mapper.Init(mi);

									if (_nameToComplexMapper.ContainsKey(key))
										_nameToComplexMapper[key] = mapper;
									else
										_nameToComplexMapper.Add(key, mapper);

									return mapper;
								}
							}

							break;
						}
					}
				}
				else
				{
					foreach (var m in _members)
						if (m.MemberAccessor.Name.Length == name.Length && m.MemberAccessor.Name.ToLower() == name)
						{
							if (_nameToComplexMapper.ContainsKey(name))
								_nameToComplexMapper[name] = m;
							else
								_nameToComplexMapper.Add(name, m);

							return m;
						}
				}

				// Under some conditions, this way lead to memory leaks.
				// In other hand, shaking mappers up every time lead to performance loss.
				// So we cache failed requests.
				// If this optimization is a memory leak for you, just comment out next line.
				//
				if (_nameToComplexMapper.ContainsKey(name))
					_nameToComplexMapper[name] = null;
				else
					_nameToComplexMapper.Add(name, null);

				return null;
			}

		}

		private MapValue[] GetMapValues(MemberAccessor member)
		{
			return MappingSchema.NewSchema.GetMapValues(member.Type);
		}

		protected virtual bool GetNullable(MemberAccessor memberAccessor)
		{
			bool isSet;
			return MetadataProvider.GetNullable(MappingSchema, Extension, memberAccessor, out isSet);
		}

		protected virtual bool GetMapIgnore(MemberAccessor memberAccessor)
		{
			bool isSet;
			return MetadataProvider.GetMapIgnore(Extension, memberAccessor, out isSet);
		}

		protected virtual string GetFieldName(MemberAccessor memberAccessor)
		{
			bool isSet;
			return MetadataProvider.GetFieldName(Extension, memberAccessor, out isSet);
		}

		protected virtual string GetFieldStorage(MemberAccessor memberAccessor)
		{
			bool isSet;
			return MetadataProvider.GetFieldStorage(Extension, memberAccessor, out isSet);
		}

		protected virtual bool GetInheritanceDiscriminator(MemberAccessor memberAccessor)
		{
			bool isSet;
			return MetadataProvider.GetInheritanceDiscriminator(Extension, memberAccessor, out isSet);
		}

		protected virtual bool GetTrimmable(MemberAccessor memberAccessor)
		{
			bool isSet;
			return MetadataProvider.GetTrimmable(Extension, memberAccessor, out isSet);
		}

		protected virtual object GetNullValue(MemberAccessor memberAccessor, bool isNullable)
		{
			return MappingSchema.NewSchema.GetDefaultValue(memberAccessor.Type);
		}

		protected virtual InheritanceMappingAttribute[] GetInheritanceMapping()
		{
			return MetadataProvider.GetInheritanceMapping(TypeAccessor.Type, Extension);
		}

		#endregion

		#region IObjectMappper Members

		public virtual object CreateInstance()
		{
			return TypeAccessor.CreateInstanceEx();
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _members.GetEnumerator();
		}

		IEnumerator<MemberMapper> IEnumerable<MemberMapper>.GetEnumerator()
		{
			return _members.GetEnumerator();
		}

		#endregion
	}
}
