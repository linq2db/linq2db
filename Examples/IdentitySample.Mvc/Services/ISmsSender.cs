using System.Threading.Tasks;

namespace IdentitySample.Services
{
	public interface ISmsSender
	{
		Task SendSmsAsync(string number, string message);
	}
}