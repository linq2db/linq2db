using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Identity;

namespace IdentitySample.Models.ManageViewModels
{
	public class ManageLoginsViewModel
	{
		public IList<UserLoginInfo> CurrentLogins { get; set; }

		public IList<AuthenticationDescription> OtherLogins { get; set; }
	}
}