using System;

namespace LinqToDB.Mapping
{
	public interface IMappingSchemaProvider
	{
		MappingSchema MappingSchema { get; }
	}
}
