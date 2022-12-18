using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	public class UserOnlyStore<TUser, TContext> : UserOnlyStore<TUser, TContext, string>
		where TUser : IdentityUser<string>
		where TContext : IDataContext
	{
		/// <summary>
		/// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public UserOnlyStore(TContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}
}
