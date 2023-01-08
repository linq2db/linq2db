using System;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Common;
	using Mapping;

	public class AttributeReader : IMetadataReader
	{
		readonly static MappingAttributesCache _cache = new (
			static source =>
			{
				var res = source.GetCustomAttributes(typeof(MappingAttribute), inherit: false);
				// API returns object[] for old frameworks and typed array for new
				return res.Length == 0 ? Array<MappingAttribute>.Empty : res is MappingAttribute[] attrRes ? attrRes : res.Cast<MappingAttribute>().ToArray();
			});

		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(type);

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(memberInfo);

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => Array<MemberInfo>.Empty;

		public string GetObjectID() => $".{nameof(AttributeReader)}.";
	}
}
