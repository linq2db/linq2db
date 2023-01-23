using System;
using ASP = Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity.Model
{
	/// <summary>
	/// Implementation of <see cref="ASP.IdentityRole{TKey}" /> with navigation properties which uses a string as the primary key.
	/// </summary>
	public class IdentityRole<TUserRole, TRoleClaim> : IdentityRole<string, TUserRole, TRoleClaim>
		where TUserRole  : ASP.IdentityUserRole<string>
		where TRoleClaim : ASP.IdentityRoleClaim<string>
	{
		/// <summary>
		/// Initializes a new instance of <see cref="IdentityRole{TUserRole, TRoleClaim}"/>.
		/// </summary>
		public IdentityRole()
		{
			Id = Guid.NewGuid().ToString();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="IdentityRole{TUserRole, TRoleClaim}"/>.
		/// </summary>
		/// <param name="roleName">The role name.</param>
		public IdentityRole(string roleName) : base(roleName)
		{
			Id = Guid.NewGuid().ToString();
		}
	}
}
