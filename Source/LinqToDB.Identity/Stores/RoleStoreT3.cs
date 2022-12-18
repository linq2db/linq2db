using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Creates a new instance of a persistence store for roles.
	/// </summary>
	/// <typeparam name="TRole">The type of the class representing a role.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	public class RoleStore<TRole, TContext, TKey> : RoleStore<TRole, TContext, TKey, IdentityUserRole<TKey>, IdentityRoleClaim<TKey>>,
		IQueryableRoleStore<TRole>,
		IRoleClaimStore<TRole>
		where TRole : IdentityRole<TKey>
		where TKey : IEquatable<TKey>
		where TContext : IDataContext
	{
		/// <summary>
		/// Constructs a new instance of <see cref="RoleStore{TRole, TContext, TKey}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public RoleStore(TContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}
}
