using System;

namespace LinqToDB.Expressions
{
	using Mapping;

	public interface IGenericInfoProvider
	{
		void SetInfo(MappingSchema mappingSchema);
	}
}
