using System;
using ASP = Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity.Model
{
	/// <summary>
	/// Represents a user in the identity system with navigation properties.
	/// </summary>
	/// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
	public class IdentityUser<TKey> :
		IdentityUser<TKey, ASP.IdentityUserClaim<TKey>, ASP.IdentityUserRole<TKey>, ASP.IdentityUserLogin<TKey>>
		where TKey : IEquatable<TKey>
	{
		/// <summary>
		/// Initializes a new instance of <see cref="IdentityUser" />.
		/// </summary>
		/// <remarks>
		/// The Id property is initialized to from a new GUID string value.
		/// </remarks>
		public IdentityUser()
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="IdentityUser" />.
		/// </summary>
		/// <param name="userName">The user name.</param>
		/// <remarks>
		/// The Id property is initialized to from a new GUID string value.
		/// </remarks>
		public IdentityUser(string userName) : base(userName)
		{
		}
	}
}
