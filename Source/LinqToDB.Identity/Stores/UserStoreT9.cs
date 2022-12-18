using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
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
	/// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
	/// <typeparam name="TUserRole">The type representing a user role.</typeparam>
	/// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
	/// <typeparam name="TUserToken">The type representing a user token.</typeparam>
	/// <typeparam name="TRoleClaim">The type representing a role claim.</typeparam>
	public class UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> :
		UserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>,
		IProtectedUserStore<TUser>
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
		public UserStore(TContext context, IdentityErrorDescriber? describer = null) : base(describer ?? new IdentityErrorDescriber())
		{
			Context = context ?? throw new ArgumentNullException(nameof(context));
		}

		/// <summary>
		/// Gets the database context for this store.
		/// </summary>
		public virtual TContext Context { get; }

		/// <inheritdoc cref="Users"/>
		public override IQueryable<TUser> Users => Context.GetTable<TUser>();

		/// <inheritdoc cref="CreateAsync(TUser, CancellationToken)"/>
		public override async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			await Context.InsertAndSetIdentity(user, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return IdentityResult.Success;
		}

		/// <inheritdoc cref="UpdateAsync(TUser, CancellationToken)"/>
		public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			var result = await Context
				.UpdateConcurrent(user, cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <inheritdoc cref="DeleteAsync(TUser, CancellationToken)"/>
		public override async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			var result = await Users
					.Where(u => u.Id.Equals(user.Id) && u.ConcurrencyStamp == user.ConcurrencyStamp)
					.DeleteAsync(cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <inheritdoc cref="FindByIdAsync(string, CancellationToken)"/>
		public override Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			var id = ConvertIdFromString(userId);

			return Users.FirstOrDefaultAsync(u => u.Id.Equals(id!), cancellationToken);
		}

		/// <inheritdoc cref="FindByNameAsync(string, CancellationToken)"/>
		public override Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			return Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
		}

		/// <inheritdoc cref="AddToRoleAsync(TUser, string, CancellationToken)"/>
		public override async Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null)                                  throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(normalizedRoleName)) throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			var roleEntity = await FindRoleAsync(normalizedRoleName, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext)
				?? throw new InvalidOperationException(Resources.RoleNotFound(normalizedRoleName));

			await Context.InsertAndSetIdentity(CreateUserRole(user, roleEntity), cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="RemoveFromRoleAsync(TUser, string, CancellationToken)"/>
		public override async Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null)                                  throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(normalizedRoleName)) throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			var q =
				from ur in Context.GetTable<TUserRole>()
				join r in Context.GetTable<TRole>() on ur.RoleId equals r.Id
				where r.NormalizedName == normalizedRoleName && ur.UserId.Equals(user.Id)
				select ur;

			await q.DeleteAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="GetRolesAsync(TUser, CancellationToken)"/>
		public override async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			var userId = user.Id;
			var query = from userRole in Context.GetTable<TUserRole>()
						join role in Context.GetTable<TRole>() on userRole.RoleId equals role.Id
						where userRole.UserId.Equals(userId)
						select role.Name;

			return await query.ToListAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="IsInRoleAsync(TUser, string, CancellationToken)"/>
		public override Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null)                                  throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(normalizedRoleName)) throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			var q = from ur in Context.GetTable<TUserRole>()
					join r in Context.GetTable<TRole>() on ur.RoleId equals r.Id
					where r.NormalizedName == normalizedRoleName && ur.UserId.Equals(user.Id)
					select ur;

			return q.AnyAsync(cancellationToken);
		}

		/// <inheritdoc cref="GetClaimsAsync(TUser, CancellationToken)"/>
		public override async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			return await Context
				.GetTable<TUserClaim>()
				.Where(uc => uc.UserId.Equals(user.Id))
				.Select(c => c.ToClaim())
				.ToListAsync(cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="UserStoreBase{TUser, TKey, TUserClaim, TUserLogin, TUserToken}.AddClaimsAsync(TUser, IEnumerable{Claim}, CancellationToken)"/>
		public override async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user   == null) throw new ArgumentNullException(nameof(user));
			if (claims == null) throw new ArgumentNullException(nameof(claims));

			var data = claims.Select(_ => CreateUserClaim(user, _));

			await Context.GetTable<TUserClaim>().BulkCopyAsync(data, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="ReplaceClaimAsync(TUser, Claim, Claim, CancellationToken)"/>
		public override async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user     == null) throw new ArgumentNullException(nameof(user));
			if (claim    == null) throw new ArgumentNullException(nameof(claim));
			if (newClaim == null) throw new ArgumentNullException(nameof(newClaim));

			await Context.GetTable<TUserClaim>()
				.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type)
				.Set(_ => _.ClaimValue, newClaim.Value)
				.Set(_ => _.ClaimType, newClaim.Type)
				.UpdateAsync(cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="RemoveClaimsAsync(TUser, IEnumerable{Claim}, CancellationToken)"/>
		public override async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user   == null) throw new ArgumentNullException(nameof(user));
			if (claims == null) throw new ArgumentNullException(nameof(claims));

			var userId = Expression.PropertyOrField(Expression.Constant(user, typeof(TUser)), nameof(user.Id));
			var equals = typeof(TKey).GetMethod(nameof(IEquatable<TKey>.Equals), new[] {typeof(TKey)})
				?? throw new InvalidOperationException($"Cannot find method Equals on type {typeof(TKey)}");
			var uc     = Expression.Parameter(typeof(TUserClaim));
			var cv     = Expression.PropertyOrField(uc, nameof(IdentityUserClaim<TKey>.ClaimValue));
			var ct     = Expression.PropertyOrField(uc, nameof(IdentityUserClaim<TKey>.ClaimType));

			var ucUserId = Expression.PropertyOrField(uc, nameof(IdentityUserClaim<TKey>.UserId));
			Expression? body = null;

			foreach (var claim in claims)
			{
				var cl = Expression.Constant(claim);

				var claimValueEquals = Expression.Equal(cv, Expression.PropertyOrField(cl, nameof(Claim.Value)));
				var claimTypeEquals  = Expression.Equal(ct, Expression.PropertyOrField(cl, nameof(Claim.Type)));
				var claimPredicate   = Expression.AndAlso(claimValueEquals, claimTypeEquals);

				body = body == null ? claimPredicate : Expression.OrElse(body, claimPredicate);
			}

			if (body != null)
			{
				// uc => uc.UserId.Equals(user.Id) && claims_predicates
				var predicate = Expression.Lambda<Func<TUserClaim, bool>>(Expression.AndAlso(Expression.Call(ucUserId, @equals, userId), body), uc);

				await Context
					.GetTable<TUserClaim>()
					.Where(predicate)
					.DeleteAsync(cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}

		/// <inheritdoc cref="AddLoginAsync(TUser, UserLoginInfo, CancellationToken)"/>
		public override async Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user  == null) throw new ArgumentNullException(nameof(user));
			if (login == null) throw new ArgumentNullException(nameof(login));

			await Context.InsertAndSetIdentity(CreateUserLogin(user, login), cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="RemoveLoginAsync(TUser, string, string, CancellationToken)"/>
		public override async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			await Context
				.GetTable<TUserLogin>()
				.DeleteAsync(userLogin => userLogin.UserId.Equals(user.Id) && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="GetLoginsAsync(TUser, CancellationToken)"/>
		public override async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			return await Context
				.GetTable<TUserLogin>()
				.Where(l => l.UserId.Equals(user.Id))
				.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
				.ToListAsync(cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="FindByEmailAsync(string, CancellationToken)"/>
		public override Task<TUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			return Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
		}

		/// <inheritdoc cref="GetUsersForClaimAsync(Claim, CancellationToken)"/>
		public override async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (claim == null) throw new ArgumentNullException(nameof(claim));

			var query = from userclaims in Context.GetTable<TUserClaim>()
						join user in Users on userclaims.UserId equals user.Id
						where userclaims.ClaimValue == claim.Value && userclaims.ClaimType == claim.Type
						select user;

			return await query.ToListAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="GetUsersInRoleAsync(string, CancellationToken)"/>
		public override async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (string.IsNullOrEmpty(normalizedRoleName)) throw new ArgumentNullException(nameof(normalizedRoleName));

			var query = from userrole in Context.GetTable<TUserRole>()
						join user in Users on userrole.UserId equals user.Id
						join role in Context.GetTable<TRole>() on userrole.RoleId equals role.Id
						where role.NormalizedName == normalizedRoleName
						select user;

			return await query.ToListAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="FindByLoginAsync(string, string, CancellationToken)"/>
		public override Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			var q = from ul in Context.GetTable<TUserLogin>()
					join u in Users on ul.UserId equals u.Id
					where ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey
					select u;

			return q.FirstOrDefaultAsync(cancellationToken);
		}

		/// <inheritdoc cref="RemoveTokenAsync(TUser, string, string, CancellationToken)"/>
		public override async Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			await Context
				.GetTable<TUserToken>()
				.DeleteAsync(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name, cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="GetTokenAsync(TUser, string, string, CancellationToken)"/>
		public override Task<string?> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			if (user == null) throw new ArgumentNullException(nameof(user));

			return Context
				.GetTable<TUserToken>()
				.Where(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name)
				.Select(_ => _.Value)
				.FirstOrDefaultAsync(cancellationToken);
		}

		/// <inheritdoc cref="FindTokenAsync(TUser, string, string, CancellationToken)"/>
		protected override Task<TUserToken?> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
		{
			return Context.GetTable<TUserToken>().FirstOrDefaultAsync(t => t.UserId.Equals(user.Id) && t.LoginProvider == loginProvider && t.Name == name, cancellationToken);
		}

		/// <inheritdoc cref="AddUserTokenAsync(TUserToken)"/>
		protected override async Task AddUserTokenAsync(TUserToken token)
		{
			// wut? no cancellation token parameter?
			await Context.InsertAndSetIdentity(token, default)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="AddUserTokenAsync(TUserToken)"/>
		protected override async Task RemoveUserTokenAsync(TUserToken token)
		{
			// wut? no cancellation token parameter?
			await Context.DeleteAsync(token, default).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="FindRoleAsync(string, CancellationToken)"/>
		protected override Task<TRole?> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
		{
			return Context.GetTable<TRole>().SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
		}

		/// <inheritdoc cref="FindUserRoleAsync(TKey, TKey, CancellationToken)"/>
		protected override Task<TUserRole?> FindUserRoleAsync(TKey userId, TKey roleId, CancellationToken cancellationToken)
		{
			return Context.GetTable<TUserRole>().FirstOrDefaultAsync(r => r.UserId.Equals(userId) && r.RoleId.Equals(roleId), cancellationToken);
		}

		/// <inheritdoc cref="FindUserAsync(TKey, CancellationToken)"/>
		protected override Task<TUser?> FindUserAsync(TKey userId, CancellationToken cancellationToken)
		{
			return Users.SingleOrDefaultAsync(u => u.Id.Equals(userId), cancellationToken);
		}

		/// <inheritdoc cref="FindUserLoginAsync(TKey, string, string, CancellationToken)"/>
		protected override Task<TUserLogin?> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
		{
			return Context.GetTable<TUserLogin>().SingleOrDefaultAsync(userLogin => userLogin.UserId.Equals(userId) && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken);
		}

		/// <inheritdoc cref="FindUserLoginAsync(string, string, CancellationToken)"/>
		protected override Task<TUserLogin?> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
		{
			return Context.GetTable<TUserLogin>().SingleOrDefaultAsync(userLogin => userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken);
		}
	}
}
