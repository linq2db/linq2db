using System;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TRole">The type representing a role.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	public class UserStore<TUser, TRole, TContext, TKey>
		: UserStore<TUser, TRole, TContext, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityUserToken<TKey>, IdentityRoleClaim<TKey>>
		where TUser    : IdentityUser<TKey>
		where TRole    : IdentityRole<TKey>
		where TContext : IDataContext
		where TKey     : IEquatable<TKey>
	{
		/// <summary>
		/// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext, TKey}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public UserStore(TContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}
}
