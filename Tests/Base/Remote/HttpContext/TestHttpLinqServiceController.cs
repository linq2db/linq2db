#if !NETFRAMEWORK
using LinqToDB.Mapping;
using LinqToDB.Remote;
using LinqToDB.Remote.HttpClient.Server;

namespace Tests.Remote
{
	public sealed class TestHttpLinqServiceController(ILinqService linqService) : LinqToDBController()
	{
		protected override ILinqService CreateLinqService() => linqService;
	}
}
#endif
