using LinqToDB.Remote;
using LinqToDB.Remote.SignalR;

namespace Tests.Remote
{
	internal sealed class TestSignalRLinqService(ILinqService linqService) : LinqToDBHub
	{
		protected override ILinqService CreateLinqService() => linqService;
	}
}
