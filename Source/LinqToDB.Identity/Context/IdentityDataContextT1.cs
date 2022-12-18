using System.Data.Common;
using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Base class for the LinqToDB database context used for identity.
	/// </summary>
	/// <typeparam name="TUser">The type of the user objects.</typeparam>
	public class IdentityDataContext<TUser> : IdentityDataContext<TUser, IdentityRole, string>
		where TUser : IdentityUser
	{
		/// <summary>
		/// Constructor with options.
		/// </summary>
		/// <param name="options">Connection options.</param>
		public IdentityDataContext(LinqToDBConnectionOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataContext()
		{
		}
	}
}
