using System;

using LinqToDB.Mapping;

namespace LinqToDB
{
	record TempTableDescriptor(EntityDescriptor EntityDescriptor, MappingSchema PrevMappingSchema)
	{
	}
}
