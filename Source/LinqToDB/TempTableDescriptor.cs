using LinqToDB.Mapping;
using LinqToDB.Model;

namespace LinqToDB
{
	sealed record TempTableDescriptor(EntityDescriptor EntityDescriptor, MappingSchema PrevMappingSchema);
}
