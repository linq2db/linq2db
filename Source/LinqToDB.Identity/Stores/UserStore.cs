using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Async;
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

	/// <summary>
	/// Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TRole">The type representing a role.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	public class UserStore<TUser, TRole, TContext> : UserStore<TUser, TRole, TContext, string>
		where TUser    : IdentityUser<string>
		where TRole    : IdentityRole<string>
		where TContext : IDataContext
	{
		/// <summary>
		/// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public UserStore(TContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}

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

	/// <summary>
	/// Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TRole">The type representing a role.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	/// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
	/// <typeparam name="TUserRole">The type representing a user role.</typeparam>
	/// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
	/// <typeparam name="TUserToken">The type representing a user token.</typeparam>
	/// <typeparam name="TRoleClaim">The type representing a role claim.</typeparam>
	public class UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> :
		UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken>,
		IUserRoleStore<TUser>
		where TUser      : IdentityUser<TKey>
		where TRole      : IdentityRole<TKey>
		where TContext   : IDataContext
		where TKey       : IEquatable<TKey>
		where TUserClaim : IdentityUserClaim<TKey>, new()
		where TUserRole  : IdentityUserRole<TKey>, new()
		where TUserLogin : IdentityUserLogin<TKey>, new()
		where TUserToken : IdentityUserToken<TKey>, new()
		where TRoleClaim : IdentityRoleClaim<TKey>, new()
	{
		/// <summary>
		/// Creates a new instance of the store.
		/// </summary>
		/// <param name="context">The context used to access the store.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
		public UserStore(TContext context, IdentityErrorDescriber? describer = null) : base(context, describer)
		{
		}

		/// <summary>
		/// A navigation property for the user roles the store contains.
		/// </summary>
		protected virtual IQueryable<TUserRole>  UserRoles  => Context.GetTable<TUserRole>();
		/// <summary>
		/// A navigation property for the user roles the store contains.
		/// </summary>
		protected virtual IQueryable<TRole>      Roles      => Context.GetTable<TRole>();

		/// <summary>
		/// Called to create a new instance of a <see cref="IdentityUserRole{TKey}"/>.
		/// </summary>
		/// <param name="user">The associated user.</param>
		/// <param name="role">The associated role.</param>
		/// <returns></returns>
		protected virtual TUserRole CreateUserRole(TUser user, TRole role)
		{
			return new TUserRole()
			{
				UserId = user.Id,
				RoleId = role.Id,
			};
		}

		// These IUserRoleStore methods deliberately mirror Microsoft's UserStoreBase signatures - the
		// 'normalizedRoleName' parameter name and the optional CancellationToken - which also keeps the API
		// source-compatible with the standalone linq2db.Identity package. MA0061 (default value) and CA1725
		// (parameter name) fire only because the interface is implemented directly here rather than through an
		// intermediate base (as EF Core does); the interface declares 'roleName' with no default.
#pragma warning disable MA0061, CA1725
		/// <summary>
		/// Retrieves all users in the specified role.
		/// </summary>
		/// <param name="normalizedRoleName">The role whose users should be retrieved.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>
		/// The <see cref="Task"/> contains a list of users, if any, that are in the specified role.
		/// </returns>
		public virtual async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (string.IsNullOrEmpty(normalizedRoleName)) throw new ArgumentNullException(nameof(normalizedRoleName));

			var query = from userrole in UserRoles
						join user in Users on userrole.UserId equals user.Id
						join role in Roles on userrole.RoleId equals role.Id
						where role.NormalizedName == normalizedRoleName
						select user;

			return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Adds the given <paramref name="normalizedRoleName"/> to the specified <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The user to add the role to.</param>
		/// <param name="normalizedRoleName">The role to add.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
		public virtual async Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			if (string.IsNullOrWhiteSpace(normalizedRoleName)) throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			var roleEntity = await FindRoleAsync(normalizedRoleName, cancellationToken).ConfigureAwait(false)
				?? throw new InvalidOperationException(Resources.RoleNotFound(normalizedRoleName));

			await Context.InsertAndSetIdentity(CreateUserRole(user, roleEntity), cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Removes the given <paramref name="normalizedRoleName"/> from the specified <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The user to remove the role from.</param>
		/// <param name="normalizedRoleName">The role to remove.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
		public virtual async Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			if (string.IsNullOrWhiteSpace(normalizedRoleName)) throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			var q =
				from ur in UserRoles
				join r in Roles on ur.RoleId equals r.Id
				where r.NormalizedName == normalizedRoleName && ur.UserId.Equals(user.Id)
				select ur;

			await q.DeleteAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Retrieves the roles the specified <paramref name="user"/> is a member of.
		/// </summary>
		/// <param name="user">The user whose roles should be retrieved.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>A <see cref="Task{TResult}"/> that contains the roles the user is a member of.</returns>
		public virtual async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			var userId = user.Id;
			var query = from userRole in UserRoles
						join role in Roles on userRole.RoleId equals role.Id
						where userRole.UserId.Equals(userId)
						select role.Name;

			return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Returns a flag indicating if the specified user is a member of the give <paramref name="normalizedRoleName"/>.
		/// </summary>
		/// <param name="user">The user whose role membership should be checked.</param>
		/// <param name="normalizedRoleName">The role to check membership of</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>A <see cref="Task{TResult}"/> containing a flag indicating if the specified user is a member of the given group. If the
		/// user is a member of the group the returned value with be true, otherwise it will be false.</returns>
		public virtual Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			if (string.IsNullOrWhiteSpace(normalizedRoleName)) throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			var q = from ur in UserRoles
					join r in Roles on ur.RoleId equals r.Id
					where r.NormalizedName == normalizedRoleName && ur.UserId.Equals(user.Id)
					select ur;

			return q.AnyAsync(cancellationToken);
		}
#pragma warning restore MA0061, CA1725

		/// <summary>
		/// Return a role with the normalized name if it exists.
		/// </summary>
		/// <param name="normalizedRoleName">The normalized role name.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>The role if it exists.</returns>
		protected virtual Task<TRole?> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
		{
			return Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
		}

		/// <summary>
		/// Return a user role for the userId and roleId if it exists.
		/// </summary>
		/// <param name="userId">The user's id.</param>
		/// <param name="roleId">The role's id.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>The user role if it exists.</returns>
		protected virtual Task<TUserRole?> FindUserRoleAsync(TKey userId, TKey roleId, CancellationToken cancellationToken)
		{
			return UserRoles.FirstOrDefaultAsync(r => r.UserId.Equals(userId) && r.RoleId.Equals(roleId), cancellationToken);
		}
	}
}
