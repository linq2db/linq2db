using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Mapping;

	public class AttributeReader : IMetadataReader
	{
		readonly static MappingAttributesCache _cache = new (static source => (MappingAttribute[])source.GetCustomAttributes(typeof(MappingAttribute), inherit: false));

		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(type);

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(memberInfo);

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;
	}
}
