using LinqToDB.Mapping;

namespace Tests.Remote
{
	public interface ITestLinqService
	{
		MappingSchema? MappingSchema { get; set; }
	}
}
