using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Async;
using LinqToDB.Concurrency;
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

	/// <summary>
	/// Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	public class UserOnlyStore<TUser, TContext> : UserOnlyStore<TUser, TContext, string>
		where TUser    : IdentityUser<string>
		where TContext : IDataContext
	{
		/// <summary>
		/// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public UserOnlyStore(TContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}

	/// <summary>
	/// Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	public class UserOnlyStore<TUser, TContext, TKey> : UserOnlyStore<TUser, TContext, TKey, IdentityUserClaim<TKey>, IdentityUserLogin<TKey>, IdentityUserToken<TKey>>
		where TUser    : IdentityUser<TKey>
		where TContext : IDataContext
		where TKey     : IEquatable<TKey>
	{
		/// <summary>
		/// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext, TKey}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public UserOnlyStore(TContext context, IdentityErrorDescriber? describer = null) : base(context, describer) { }
	}

	/// <summary>
	/// Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	/// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
	/// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
	/// <typeparam name="TUserToken">The type representing a user token.</typeparam>
	public class UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken> :
		UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>,
		IUserLoginStore<TUser>,
		IUserClaimStore<TUser>,
		IUserPasswordStore<TUser>,
		IUserSecurityStampStore<TUser>,
		IUserEmailStore<TUser>,
		IUserLockoutStore<TUser>,
		IUserPhoneNumberStore<TUser>,
		IQueryableUserStore<TUser>,
		IUserTwoFactorStore<TUser>,
		IUserAuthenticationTokenStore<TUser>,
		IUserAuthenticatorKeyStore<TUser>,
		IUserTwoFactorRecoveryCodeStore<TUser>,
		IProtectedUserStore<TUser>
#if NET10_0_OR_GREATER
		,
		IUserPasskeyStore<TUser>
#endif
		where TUser      : IdentityUser<TKey>
		where TContext   : IDataContext
		where TKey       : IEquatable<TKey>
		where TUserClaim : IdentityUserClaim<TKey>, new()
		where TUserLogin : IdentityUserLogin<TKey>, new()
		where TUserToken : IdentityUserToken<TKey>, new()
	{
		/// <summary>
		/// Creates a new instance of the store.
		/// </summary>
		/// <param name="context">The context used to access the store.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
		public UserOnlyStore(TContext context, IdentityErrorDescriber? describer = null) : base(describer ?? new IdentityErrorDescriber())
		{
			Context = context ?? throw new ArgumentNullException(nameof(context));
		}

		/// <summary>
		/// Gets the database context for this store.
		/// </summary>
		public virtual TContext Context { get; }

		/// <inheritdoc cref="Users"/>
		public override IQueryable<TUser>        Users      => Context.GetTable<TUser>();
		/// <summary>
		/// A navigation property for the user claims the store contains.
		/// </summary>
		protected virtual IQueryable<TUserClaim> UserClaims => Context.GetTable<TUserClaim>();
		/// <summary>
		/// A navigation property for the user logins the store contains.
		/// </summary>
		protected virtual IQueryable<TUserLogin> UserLogins => Context.GetTable<TUserLogin>();
		/// <summary>
		/// A navigation property for the user tokens the store contains.
		/// </summary>
		protected virtual IQueryable<TUserToken> UserTokens => Context.GetTable<TUserToken>();

		/// <inheritdoc cref="CreateAsync(TUser, CancellationToken)"/>
		public override async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			await Context.InsertAndSetIdentity(user, cancellationToken).ConfigureAwait(false);

			return IdentityResult.Success;
		}

		/// <inheritdoc cref="UpdateAsync(TUser, CancellationToken)"/>
		public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			var result = await Context
				.UpdateOptimisticAsync(user, cancellationToken)
				.ConfigureAwait(false);

			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <inheritdoc cref="DeleteAsync(TUser, CancellationToken)"/>
		public override async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			var result = await Context.DeleteOptimisticAsync(user, cancellationToken).ConfigureAwait(false);

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

		/// <inheritdoc cref="GetClaimsAsync(TUser, CancellationToken)"/>
		public override async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			return await UserClaims
				.Where(uc => uc.UserId.Equals(user.Id))
				.Select(c => c.ToClaim())
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);
		}

		/// <inheritdoc cref="UserStoreBase{TUser, TKey, TUserClaim, TUserLogin, TUserToken}.AddClaimsAsync(TUser, IEnumerable{Claim}, CancellationToken)"/>
		public override async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(claims);

			var data = claims.Select(_ => CreateUserClaim(user, _));

			await Context.BulkCopyAsync(data, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="ReplaceClaimAsync(TUser, Claim, Claim, CancellationToken)"/>
		public override async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(claim);
			ArgumentNullException.ThrowIfNull(newClaim);

			await UserClaims
				.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type)
				.Set(_ => _.ClaimValue, newClaim.Value)
				.Set(_ => _.ClaimType, newClaim.Type)
				.UpdateAsync(cancellationToken)
				.ConfigureAwait(false);
		}

		/// <inheritdoc cref="RemoveClaimsAsync(TUser, IEnumerable{Claim}, CancellationToken)"/>
		public override async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(claims);

			var userId   = Expression.PropertyOrField(Expression.Constant(user, typeof(TUser)), nameof(user.Id));
			var equals   = typeof(TKey).GetMethod(nameof(IEquatable<>.Equals), new[] {typeof(TKey)})
				?? throw new InvalidOperationException($"Cannot find method Equals on type {typeof(TKey)}");
			var uc       = Expression.Parameter(typeof(TUserClaim));
			var cv       = Expression.PropertyOrField(uc, nameof(IdentityUserClaim<>.ClaimValue));
			var ct       = Expression.PropertyOrField(uc, nameof(IdentityUserClaim<>.ClaimType));
			var ucUserId = Expression.PropertyOrField(uc, nameof(IdentityUserClaim<>.UserId));

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

				await UserClaims
					.Where(predicate)
					.DeleteAsync(cancellationToken)
					.ConfigureAwait(false);
			}
		}

		/// <inheritdoc cref="AddLoginAsync(TUser, UserLoginInfo, CancellationToken)"/>
		public override async Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(login);

			await Context.InsertAndSetIdentity(CreateUserLogin(user, login), cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="RemoveLoginAsync(TUser, string, string, CancellationToken)"/>
		public override async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			await UserLogins
				.DeleteAsync(userLogin => userLogin.UserId.Equals(user.Id) && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken)
				.ConfigureAwait(false);
		}

		/// <inheritdoc cref="GetLoginsAsync(TUser, CancellationToken)"/>
		public override async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			return await UserLogins
				.Where(l => l.UserId.Equals(user.Id))
				.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);
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

			ArgumentNullException.ThrowIfNull(claim);

			var query = from userclaims in UserClaims
						join user in Users on userclaims.UserId equals user.Id
						where userclaims.ClaimValue == claim.Value && userclaims.ClaimType == claim.Type
						select user;

			return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="FindByLoginAsync(string, string, CancellationToken)"/>
		public override Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			var q = from ul in UserLogins
					join u in Users on ul.UserId equals u.Id
					where ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey
					select u;

			return q.FirstOrDefaultAsync(cancellationToken);
		}

		/// <inheritdoc cref="RemoveTokenAsync(TUser, string, string, CancellationToken)"/>
		public override async Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			await UserTokens
				.DeleteAsync(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name, cancellationToken)
				.ConfigureAwait(false);
		}

		/// <inheritdoc cref="GetTokenAsync(TUser, string, string, CancellationToken)"/>
		public override Task<string?> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			return UserTokens
				.Where(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name)
				.Select(_ => _.Value)
				.FirstOrDefaultAsync(cancellationToken);
		}

		/// <inheritdoc cref="FindTokenAsync(TUser, string, string, CancellationToken)"/>
		protected override Task<TUserToken?> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
		{
			return UserTokens.FirstOrDefaultAsync(t => t.UserId.Equals(user.Id) && t.LoginProvider == loginProvider && t.Name == name, cancellationToken);
		}

		/// <inheritdoc cref="AddUserTokenAsync(TUserToken)"/>
		protected override async Task AddUserTokenAsync(TUserToken token)
		{
			// wut? no cancellation token parameter?
			await Context.InsertAndSetIdentity(token, default)
				.ConfigureAwait(false);
		}

		/// <inheritdoc cref="AddUserTokenAsync(TUserToken)"/>
		protected override async Task RemoveUserTokenAsync(TUserToken token)
		{
			// wut? no cancellation token parameter?
			await Context.DeleteAsync(token, default).ConfigureAwait(false);
		}

		/// <inheritdoc cref="FindUserAsync(TKey, CancellationToken)"/>
		protected override Task<TUser?> FindUserAsync(TKey userId, CancellationToken cancellationToken)
		{
			return Users.SingleOrDefaultAsync(u => u.Id.Equals(userId), cancellationToken);
		}

		/// <inheritdoc cref="FindUserLoginAsync(TKey, string, string, CancellationToken)"/>
		protected override Task<TUserLogin?> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
		{
			return UserLogins.SingleOrDefaultAsync(userLogin => userLogin.UserId.Equals(userId) && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken);
		}

		/// <inheritdoc cref="FindUserLoginAsync(string, string, CancellationToken)"/>
		protected override Task<TUserLogin?> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
		{
			return UserLogins.SingleOrDefaultAsync(userLogin => userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken);
		}

#if NET10_0_OR_GREATER
		#region IUserPasskeyStore

		/// <summary>
		/// A navigation property for the user passkeys the store contains.
		/// </summary>
		protected virtual IQueryable<IdentityUserPasskey<TKey>> UserPasskeys => Context.GetTable<IdentityUserPasskey<TKey>>();

		/// <inheritdoc cref="AddOrUpdatePasskeyAsync(TUser, UserPasskeyInfo, CancellationToken)"/>
		public virtual async Task AddOrUpdatePasskeyAsync(TUser user, UserPasskeyInfo passkey, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(passkey);

			var existing = await FindUserPasskeyByIdAsync(passkey.CredentialId, cancellationToken).ConfigureAwait(false);

			if (existing != null)
			{
				UpdateFromUserPasskeyInfo(existing, passkey);
				await Context.UpdateAsync(existing, token: cancellationToken).ConfigureAwait(false);
			}
			else
			{
				await Context.InsertAndSetIdentity(CreateUserPasskey(user, passkey), cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc cref="GetPasskeysAsync(TUser, CancellationToken)"/>
		public virtual async Task<IList<UserPasskeyInfo>> GetPasskeysAsync(TUser user, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);

			var passkeys = await UserPasskeys
				.Where(p => p.UserId.Equals(user.Id))
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			return passkeys.Select(ToUserPasskeyInfo).ToList();
		}

		/// <inheritdoc cref="FindByPasskeyIdAsync(byte[], CancellationToken)"/>
		public virtual async Task<TUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(credentialId);

			var passkey = await FindUserPasskeyByIdAsync(credentialId, cancellationToken).ConfigureAwait(false);

			return passkey == null ? null : await FindUserAsync(passkey.UserId, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="FindPasskeyAsync(TUser, byte[], CancellationToken)"/>
		public virtual async Task<UserPasskeyInfo?> FindPasskeyAsync(TUser user, byte[] credentialId, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(credentialId);

			var passkey = await FindUserPasskeyAsync(user.Id, credentialId, cancellationToken).ConfigureAwait(false);

			return passkey == null ? null : ToUserPasskeyInfo(passkey);
		}

		/// <inheritdoc cref="RemovePasskeyAsync(TUser, byte[], CancellationToken)"/>
		public virtual async Task RemovePasskeyAsync(TUser user, byte[] credentialId, CancellationToken cancellationToken)
		{
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(credentialId);

			await UserPasskeys
				.Where(p => p.UserId.Equals(user.Id) && p.CredentialId == credentialId)
				.DeleteAsync(cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Finds a passkey for the specified user with the specified credential id.
		/// </summary>
		protected virtual Task<IdentityUserPasskey<TKey>?> FindUserPasskeyAsync(TKey userId, byte[] credentialId, CancellationToken cancellationToken)
			=> UserPasskeys.SingleOrDefaultAsync(p => p.UserId.Equals(userId) && p.CredentialId == credentialId, cancellationToken);

		/// <summary>
		/// Finds a passkey with the specified credential id.
		/// </summary>
		protected virtual Task<IdentityUserPasskey<TKey>?> FindUserPasskeyByIdAsync(byte[] credentialId, CancellationToken cancellationToken)
			=> UserPasskeys.SingleOrDefaultAsync(p => p.CredentialId == credentialId, cancellationToken);

		/// <summary>
		/// Called to create a new instance of <see cref="IdentityUserPasskey{TKey}"/>.
		/// </summary>
		protected virtual IdentityUserPasskey<TKey> CreateUserPasskey(TUser user, UserPasskeyInfo passkey)
			=> new()
			{
				UserId       = user.Id,
				CredentialId = passkey.CredentialId,
				Data         = new()
				{
					PublicKey         = passkey.PublicKey,
					Name              = passkey.Name,
					CreatedAt         = passkey.CreatedAt,
					Transports        = passkey.Transports,
					SignCount         = passkey.SignCount,
					IsUserVerified    = passkey.IsUserVerified,
					IsBackupEligible  = passkey.IsBackupEligible,
					IsBackedUp        = passkey.IsBackedUp,
					AttestationObject = passkey.AttestationObject,
					ClientDataJson    = passkey.ClientDataJson,
				},
			};

		private static UserPasskeyInfo ToUserPasskeyInfo(IdentityUserPasskey<TKey> passkey)
			=> new(
				passkey.CredentialId,
				passkey.Data.PublicKey,
				passkey.Data.CreatedAt,
				passkey.Data.SignCount,
				passkey.Data.Transports,
				passkey.Data.IsUserVerified,
				passkey.Data.IsBackupEligible,
				passkey.Data.IsBackedUp,
				passkey.Data.AttestationObject,
				passkey.Data.ClientDataJson)
			{
				Name = passkey.Data.Name,
			};

		// Only properties mutable after passkey creation are updated.
		// See https://www.w3.org/TR/webauthn-3/#authn-ceremony-update-credential-record
		private static void UpdateFromUserPasskeyInfo(IdentityUserPasskey<TKey> passkey, UserPasskeyInfo info)
		{
			passkey.Data.Name           = info.Name;
			passkey.Data.SignCount      = info.SignCount;
			passkey.Data.IsBackedUp     = info.IsBackedUp;
			passkey.Data.IsUserVerified = info.IsUserVerified;
		}

		#endregion
#endif
	}
}
