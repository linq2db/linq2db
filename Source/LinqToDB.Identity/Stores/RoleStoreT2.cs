using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Creates a new instance of a persistence store for roles.
	/// </summary>
	/// <typeparam name="TRole">The type of the class representing a role.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	public class RoleStore<TRole, TContext> : RoleStore<TRole, TContext, string>
		where TRole    : IdentityRole<string>
		where TContext : IDataContext
	{
		/// <summary>
		/// Constructs a new instance of <see cref="RoleStore{TRole, TContext}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public RoleStore(TContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}
}
