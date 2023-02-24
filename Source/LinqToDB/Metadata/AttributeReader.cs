﻿using System;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Mapping;
	using Extensions;

	public class AttributeReader : IMetadataReader
	{
		readonly static MappingAttributesCache _cache = new (
			static (_, source) =>
			{
				var res = source.GetAttributes<MappingAttribute>(inherit: false);
				// API returns object[] for old frameworks and typed array for new
				return res.Length == 0 ? Array<MappingAttribute>.Empty : res is MappingAttribute[] attrRes ? attrRes : res.Cast<MappingAttribute>().ToArray();
			});

		public MappingAttribute[] GetAttributes(Type type)
			=> _cache.GetMappingAttributes<MappingAttribute>(type);

		public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
			=> _cache.GetMappingAttributes<MappingAttribute>(type, memberInfo);

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;

		public string GetObjectID() => $".{nameof(AttributeReader)}.";
	}
}
