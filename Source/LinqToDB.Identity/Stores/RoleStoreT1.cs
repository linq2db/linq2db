using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{

	/// <summary>
	/// Creates a new instance of a persistence store for roles.
	/// </summary>
	/// <typeparam name="TRole">The type of the class representing a role</typeparam>
	public class RoleStore<TRole> : RoleStore<TRole, IDataContext, string>
		where TRole : IdentityRole<string>
	{
		/// <summary>
		/// Constructs a new instance of <see cref="RoleStore{TRole}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public RoleStore(IDataContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}
}
