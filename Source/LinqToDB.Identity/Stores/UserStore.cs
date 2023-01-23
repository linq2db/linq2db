using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Represents a new instance of a persistence store for users, using the default implementation
	/// of <see cref="IdentityUser{TKey}"/> with a string as a primary key.
	/// </summary>
	public class UserStore : UserStore<IdentityUser<string>>
	{
		/// <summary>
		/// Constructs a new instance of <see cref="UserStore"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public UserStore(IDataContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}
}
