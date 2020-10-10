using System.ComponentModel.DataAnnotations;

namespace IdentitySample.Models.ManageViewModels
{
	public class VerifyPhoneNumberViewModel
	{
		[Required]
		public string Code { get; set; }

		[Required]
		[Phone]
		[Display(Name = "Phone number")]
		public string PhoneNumber { get; set; }
	}
}