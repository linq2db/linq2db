using System;

namespace LinqToDB.Mapping
{
	public interface IMappingSchemaProvider
	{
		MappingSchemaOld MappingSchema { get; }
	}
}
