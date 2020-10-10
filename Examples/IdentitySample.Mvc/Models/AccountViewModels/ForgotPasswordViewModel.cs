using System.ComponentModel.DataAnnotations;

namespace IdentitySample.Models.AccountViewModels
{
	public class ExternalLoginConfirmationViewModel
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}

	public class ForgotPasswordViewModel
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}
}