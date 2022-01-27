using LinqToDB.Remote;
using LinqToDB.Remote.Soap;

namespace WebHost
{
	public class SoapRemoteLinqService : SoapLinqService
	{
		public SoapRemoteLinqService(
			): base(new LinqService() { AllowUpdates = true })
		{

		}
	}
}
