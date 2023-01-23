using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Creates a new instance of a persistence store for the specified user type.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	public class UserStore<TUser> : UserStore<TUser, IdentityRole, IDataContext, string>
		where TUser : IdentityUser<string>, new()
	{
		/// <summary>
		/// Constructs a new instance of <see cref="UserStore{TUser}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public UserStore(IDataContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}
}
