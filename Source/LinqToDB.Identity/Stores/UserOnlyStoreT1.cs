using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{

	/// <summary>
	/// Creates a new instance of a persistence store for the specified user type.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	public class UserOnlyStore<TUser> : UserOnlyStore<TUser, IDataContext, string> where TUser : IdentityUser<string>, new()
	{
		/// <summary>
		/// Constructs a new instance of <see cref="UserOnlyStore{TUser}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public UserOnlyStore(IDataContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}
}
