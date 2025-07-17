using LinqToDB.Mapping;

namespace LinqToDB
{
	sealed record TempTableDescriptor(EntityDescriptor EntityDescriptor, MappingSchema PrevMappingSchema);
}
