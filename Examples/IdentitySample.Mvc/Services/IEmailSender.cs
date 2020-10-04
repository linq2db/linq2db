using System.Threading.Tasks;

namespace IdentitySample.Services
{
	public interface IEmailSender
	{
		Task SendEmailAsync(string email, string subject, string message);
	}
}