// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Creates a new instance of a persistence store for the specified user type.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	public class UserStore<TUser> : UserStore<string, TUser, IdentityRole>
		where TUser : IdentityUser<string>, new()
	{
		/// <summary>
		///     Constructs a new instance of <see cref="UserStore{TUser, TContext, TConnection}" />.
		/// </summary>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber" />.</param>
		public UserStore(IConnectionFactory factory, IdentityErrorDescriber? describer = null)
			: base(factory, describer)
		{
		}
	}

	/// <summary>
	///     Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TRole">The type representing a role.</typeparam>
	public class UserStore<TUser, TRole> : UserStore<string, TUser, TRole>
		where TUser : IdentityUser<string>
		where TRole : IdentityRole<string>
	{
		/// <summary>
		///     Constructs a new instance of <see cref="LinqToDB.Identity.UserStore{TConnection,TUser,TRole}" />.
		/// </summary>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber" />.</param>
		public UserStore(IConnectionFactory factory, IdentityErrorDescriber? describer = null)
			: base(factory, describer)
		{
		}
	}

	/// <summary>
	///     Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TRole">The type representing a role.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	public class UserStore<TKey, TUser, TRole> :
		UserStore<TKey, TUser, TRole,IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>,
			IdentityUserToken<TKey>>
		where TUser : IdentityUser<TKey>
		where TRole : IdentityRole<TKey>
		where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     Constructs a new instance of <see cref="LinqToDB.Identity.UserStore{TConnection,TUser,TRole}" />.
		/// </summary>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber" />.</param>
		public UserStore(IConnectionFactory factory, IdentityErrorDescriber? describer = null)
			: base(factory, describer)
		{
		}

		/// <summary>
		///     Called to create a new instance of a <see cref="IdentityUserRole{TKey}" />.
		/// </summary>
		/// <param name="user">The associated user.</param>
		/// <param name="role">The associated role.</param>
		/// <returns></returns>
		protected override IdentityUserRole<TKey> CreateUserRole(TUser user, TRole role)
		{
			return new IdentityUserRole<TKey>
			{
				UserId = user.Id,
				RoleId = role.Id
			};
		}

		/// <summary>
		///     Called to create a new instance of a <see cref="IdentityUserClaim{TKey}" />.
		/// </summary>
		/// <param name="user">The associated user.</param>
		/// <param name="claim">The associated claim.</param>
		/// <returns></returns>
		protected override IdentityUserClaim<TKey> CreateUserClaim(TUser user, Claim claim)
		{
			var userClaim = new IdentityUserClaim<TKey> {UserId = user.Id};
			userClaim.InitializeFromClaim(claim);
			return userClaim;
		}

		/// <summary>
		///     Called to create a new instance of a <see cref="IdentityUserLogin{TKey}" />.
		/// </summary>
		/// <param name="user">The associated user.</param>
		/// <param name="login">The sasociated login.</param>
		/// <returns></returns>
		protected override IdentityUserLogin<TKey> CreateUserLogin(TUser user, UserLoginInfo login)
		{
			return new IdentityUserLogin<TKey>
			{
				UserId = user.Id,
				ProviderKey = login.ProviderKey,
				LoginProvider = login.LoginProvider,
				ProviderDisplayName = login.ProviderDisplayName
			};
		}

		/// <summary>
		///     Called to create a new instance of a <see cref="IdentityUserToken{TKey}" />.
		/// </summary>
		/// <param name="user">The associated user.</param>
		/// <param name="loginProvider">The associated login provider.</param>
		/// <param name="name">The name of the user token.</param>
		/// <param name="value">The value of the user token.</param>
		/// <returns></returns>
		protected override IdentityUserToken<TKey> CreateUserToken(TUser user, string loginProvider, string name,
			string value)
		{
			return new IdentityUserToken<TKey>
			{
				UserId = user.Id,
				LoginProvider = loginProvider,
				Name = name,
				Value = value
			};
		}
	}


	/// <summary>
	///     Represents a new instance of a persistence store for the specified user and role types.
	/// </summary>
	/// <typeparam name="TUser">The type representing a user.</typeparam>
	/// <typeparam name="TRole">The type representing a role.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	/// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
	/// <typeparam name="TUserRole">The type representing a user role.</typeparam>
	/// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
	/// <typeparam name="TUserToken">The type representing a user token.</typeparam>
	public class UserStore<TKey, TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken> :
		IUserLoginStore<TUser>,
		IUserRoleStore<TUser>,
		IUserClaimStore<TUser>,
		IUserPasswordStore<TUser>,
		IUserSecurityStampStore<TUser>,
		IUserEmailStore<TUser>,
		IUserLockoutStore<TUser>,
		IUserPhoneNumberStore<TUser>,
		IQueryableUserStore<TUser>,
		IUserTwoFactorStore<TUser>,
		IUserAuthenticationTokenStore<TUser>
#if NETSTANDARD2_0
		,IUserAuthenticatorKeyStore<TUser>
#endif
		where TUser : class, IIdentityUser<TKey>
		where TRole : class, IIdentityRole<TKey>
		where TUserClaim : class, IIdentityUserClaim<TKey>, new()
		where TUserRole : class, IIdentityUserRole<TKey>, new()
		where TUserLogin : class, IIdentityUserLogin<TKey>, new()
		where TUserToken : class, IIdentityUserToken<TKey>, new ()
		where TKey : IEquatable<TKey>
	{
		private readonly IConnectionFactory _factory;

		/// <summary>
		/// Gets <see cref="DataConnection"/> from supplied <see cref="IConnectionFactory"/>
		/// </summary>
		/// <returns><see cref="DataConnection"/> </returns>
		protected DataConnection GetConnection() => _factory.GetConnection();
		/// <summary>
		/// Gets <see cref="IDataContext"/> from supplied <see cref="IConnectionFactory"/>
		/// </summary>
		/// <returns><see cref="IDataContext"/> </returns>
		protected IDataContext GetContext() => _factory.GetContext();

		private bool _disposed;

		/// <summary>
		///     Creates a new instance of
		///     <see
		///         cref="LinqToDB.Identity.UserStore{TUser,TRole,TKey,TUserClaim,TUserRole,TUserLogin,TUserToken}" />
		///     .
		/// </summary>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber" /> used to describe store errors.</param>
		public UserStore(IConnectionFactory factory, IdentityErrorDescriber? describer = null)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			_factory = factory;

			ErrorDescriber = describer ?? new IdentityErrorDescriber();
		}

		/// <summary>
		///     Gets or sets the <see cref="IdentityErrorDescriber" /> for any error that occurred with the current operation.
		/// </summary>
		public IdentityErrorDescriber ErrorDescriber { get; set; }

		/// <summary>
		///     A navigation property for the users the store contains.
		/// </summary>
		public virtual IQueryable<TUser> Users => GetContext().GetTable<TUser>();

		/// <summary>
		///     Sets the token value for a particular user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="loginProvider">The authentication provider for the token.</param>
		/// <param name="name">The name of the token.</param>
		/// <param name="value">The value of the token.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task SetTokenAsync(TUser user, string loginProvider, string name, string value,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();

			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (var db = GetConnection())
			{
				await SetTokenAsync(db, user, loginProvider, name, value, cancellationToken);
			}
		}

		/// <inheritdoc cref="SetTokenAsync(TUser, string, string, string, CancellationToken)"/>
		protected virtual async Task SetTokenAsync(DataConnection db, TUser user, string loginProvider, string name, string value,
			CancellationToken cancellationToken)
		{
			await Task.Run(() =>
			{
				var q = db.GetTable<TUserToken>()
					.Where(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name);

				var token = q.FirstOrDefault();

				if (token == null)
				{
					db.Insert(CreateUserToken(user, loginProvider, name, value));
				}
				else
				{
					token.Value = value;
					q.Set(_ => _.Value, value)
						.Update();
				}
			}, cancellationToken);
		}

		/// <summary>
		///     Deletes a token for a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="loginProvider">The authentication provider for the token.</param>
		/// <param name="name">The name of the token.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task RemoveTokenAsync(TUser user, string loginProvider, string name,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();

			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (var db = GetConnection())
			{
				await RemoveTokenAsync(db, user, loginProvider, name, cancellationToken);
			}
		}

		/// <inheritdoc cref="RemoveTokenAsync(TUser , string , string , CancellationToken )"/>
		protected virtual async Task RemoveTokenAsync(DataConnection db, TUser user, string loginProvider, string name,
			CancellationToken cancellationToken)
		{
			await Task.Run(() =>
					db.GetTable<TUserToken>()
						.Where(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name)
						.Delete(),
				cancellationToken);
		}

		/// <summary>
		///     Returns the token value.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="loginProvider">The authentication provider for the token.</param>
		/// <param name="name">The name of the token.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task<string?> GetTokenAsync(TUser user, string loginProvider, string name,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();

			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (var db = GetConnection())
			{
				return await GetTokenAsync(db, user, loginProvider, name, cancellationToken);
			}
		}

		///<inheritdoc cref="GetTokenAsync(TUser , string , string ,CancellationToken )"/>
		protected virtual async Task<string?> GetTokenAsync(DataConnection db, TUser user, string loginProvider, string name,
			CancellationToken cancellationToken)
		{
			var entry = await db
				.GetTable<TUserToken>()
				.Where(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name)
				.FirstOrDefaultAsync(cancellationToken);

			return entry?.Value;
		}

		/// <summary>
		///     Get the claims associated with the specified <paramref name="user" /> as an asynchronous operation.
		/// </summary>
		/// <param name="user">The user whose claims should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that contains the claims granted to a user.</returns>
		public async Task<IList<Claim>> GetClaimsAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));


			using (var db = GetConnection())
			{
				return await GetClaimsAsync(db, user, cancellationToken);
			}
		}

		/// <inheritdoc cref="GetClaimsAsync(TUser, CancellationToken)"/>
		protected virtual async Task<IList<Claim>> GetClaimsAsync(DataConnection db, TUser user, CancellationToken cancellationToken)
		{
			return await
				db
					.GetTable<TUserClaim>()
					.Where(uc => uc.UserId.Equals(user.Id))
					.Select(c => c.ToClaim())
					.ToListAsync(cancellationToken);
		}

		/// <summary>
		///     Adds the <paramref name="claims" /> given to the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user to add the claim to.</param>
		/// <param name="claims">The claim to add to the user.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (claims == null)
				throw new ArgumentNullException(nameof(claims));
			var data = claims.Select(_ => CreateUserClaim(user, _));

			using (var dc = GetConnection())
			{
				return AddClaimsAsync(dc, data, cancellationToken);
			}
		}

		/// <inheritdoc cref="AddClaimsAsync(TUser, IEnumerable{Claim}, CancellationToken)"/>
		protected virtual Task AddClaimsAsync(DataConnection dc, IEnumerable<TUserClaim> data, CancellationToken cancellationToken)
		{
			dc.BulkCopy(data);
			return Task.FromResult(true);
		}

		/// <summary>
		///     Replaces the <paramref name="claim" /> on the specified <paramref name="user" />, with the
		///     <paramref name="newClaim" />.
		/// </summary>
		/// <param name="user">The role to replace the claim on.</param>
		/// <param name="claim">The claim replace.</param>
		/// <param name="newClaim">The new claim replacing the <paramref name="claim" />.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (claim == null)
				throw new ArgumentNullException(nameof(claim));
			if (newClaim == null)
				throw new ArgumentNullException(nameof(newClaim));

			using (var db = GetConnection())
			{
				await ReplaceClaimAsync(user, claim, newClaim, cancellationToken, db);
			}
		}

		/// <inheritdoc cref="ReplaceClaimAsync(TUser, Claim, Claim, CancellationToken)"/>
		protected virtual async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken,
			DataConnection db)
		{
			await Task.Run(() =>
			{
				var q = db
					.GetTable<TUserClaim>()
					.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type);

				q.Set(_ => _.ClaimValue, newClaim.Value)
					.Set(_ => _.ClaimType, newClaim.Type)
					.Update();
			}, cancellationToken);
		}

		/// <summary>
		///     Removes the <paramref name="claims" /> given from the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user to remove the claims from.</param>
		/// <param name="claims">The claim to remove.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (claims == null)
				throw new ArgumentNullException(nameof(claims));

			using (var db = GetConnection())
			{
				await RemoveClaimsAsync(db, user, claims, cancellationToken);
			}
		}

		/// <inheritdoc cref="RemoveClaimsAsync(TUser, IEnumerable{Claim},CancellationToken)"/>
		protected virtual async Task RemoveClaimsAsync(DataConnection db, TUser user, IEnumerable<Claim> claims,
			CancellationToken cancellationToken)
		{
			await Task.Run(() =>
			{
				var q = db.GetTable<TUserClaim>();
				var userId = Expression.PropertyOrField(Expression.Constant(user, typeof(TUser)), nameof(user.Id));
				var equals = typeof(TKey).GetMethod(nameof(IEquatable<TKey>.Equals), new[] {typeof(TKey)});
				var uc = Expression.Parameter(typeof(TUserClaim));
				Expression? body = null;
				var ucUserId = Expression.PropertyOrField(uc, nameof(IIdentityUserClaim<TKey>.UserId));
				var userIdEquals = Expression.Call(ucUserId, @equals, userId);

				foreach (var claim in claims)
				{
					var cl = Expression.Constant(claim);

					var claimValueEquals = Expression.Equal(
						Expression.PropertyOrField(uc, nameof(IIdentityUserClaim<TKey>.ClaimValue)),
						Expression.PropertyOrField(cl, nameof(Claim.Value)));
					var claimTypeEquals =
						Expression.Equal(
							Expression.PropertyOrField(uc, nameof(IIdentityUserClaim<TKey>.ClaimType)),
							Expression.PropertyOrField(cl, nameof(Claim.Type)));

					var predicatePart = Expression.And(Expression.And(userIdEquals, claimValueEquals), claimTypeEquals);

					body = body == null ? predicatePart : Expression.Or(body, predicatePart);
				}

				if (body != null)
				{
					var predicate = Expression.Lambda<Func<TUserClaim, bool>>(body, uc);

					q.Where(predicate).Delete();
				}
			}, cancellationToken);
		}

		/// <summary>
		///     Retrieves all users with the specified claim.
		/// </summary>
		/// <param name="claim">The claim whose users should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> contains a list of users, if any, that contain the specified claim.
		/// </returns>
		public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (claim == null)
				throw new ArgumentNullException(nameof(claim));

			using (var db = GetConnection())
			{
				return await UsersForClaimAsync(db, claim, cancellationToken);
			}
		}

		/// <inheritdoc cref="GetUsersForClaimAsync(Claim, CancellationToken)"/>
		protected virtual async Task<IList<TUser>> UsersForClaimAsync(DataConnection db, Claim claim, CancellationToken cancellationToken)
		{
			var query = from userclaims in db.GetTable<TUserClaim>()
				join user in db.GetTable<TUser>() on userclaims.UserId equals user.Id
				where userclaims.ClaimValue == claim.Value
				      && userclaims.ClaimType == claim.Type
				select user;

			return await query.ToListAsync(cancellationToken);
		}

		/// <summary>
		///     Gets a flag indicating whether the email address for the specified <paramref name="user" /> has been verified, true
		///     if the email address is verified otherwise
		///     false.
		/// </summary>
		/// <param name="user">The user whose email confirmation status should be returned.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The task object containing the results of the asynchronous operation, a flag indicating whether the email address
		///     for the specified <paramref name="user" />
		///     has been confirmed or not.
		/// </returns>
		public virtual Task<bool> GetEmailConfirmedAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.EmailConfirmed);
		}

		/// <summary>
		///     Sets the flag indicating whether the specified <paramref name="user" />'s email address has been confirmed or not.
		/// </summary>
		/// <param name="user">The user whose email confirmation status should be set.</param>
		/// <param name="confirmed">
		///     A flag indicating if the email address has been confirmed, true if the address is confirmed
		///     otherwise false.
		/// </param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The task object representing the asynchronous operation.</returns>
		public virtual Task SetEmailConfirmedAsync(TUser user, bool confirmed,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.EmailConfirmed = confirmed;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Sets the <paramref name="email" /> address for a <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose email should be set.</param>
		/// <param name="email">The email to set.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The task object representing the asynchronous operation.</returns>
		public virtual Task SetEmailAsync(TUser user, string email,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.Email = email;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Gets the email address for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose email should be returned.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The task object containing the results of the asynchronous operation, the email address for the specified
		///     <paramref name="user" />.
		/// </returns>
		public virtual Task<string> GetEmailAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.Email);
		}

		/// <summary>
		///     Returns the normalized email for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose email address to retrieve.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The task object containing the results of the asynchronous lookup operation, the normalized email address if any
		///     associated with the specified user.
		/// </returns>
		public virtual Task<string> GetNormalizedEmailAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.NormalizedEmail);
		}

		/// <summary>
		///     Sets the normalized email for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose email address to set.</param>
		/// <param name="normalizedEmail">The normalized email to set for the specified <paramref name="user" />.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The task object representing the asynchronous operation.</returns>
		public virtual Task SetNormalizedEmailAsync(TUser user, string normalizedEmail,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.NormalizedEmail = normalizedEmail;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Gets the user, if any, associated with the specified, normalized email address.
		/// </summary>
		/// <param name="normalizedEmail">The normalized email address to return the user for.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The task object containing the results of the asynchronous lookup operation, the user if any associated with the
		///     specified normalized email address.
		/// </returns>
		public async Task<TUser> FindByEmailAsync(string normalizedEmail,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();

			using (var db = GetConnection())
			{
				return await FindByEmailAsync(db, normalizedEmail, cancellationToken);
			}
		}

		/// <inheritdoc cref="FindByEmailAsync(string, CancellationToken)"/>
		protected virtual async Task<TUser> FindByEmailAsync(DataConnection db, string normalizedEmail, CancellationToken cancellationToken)
		{
			return await db.GetTable<TUser>().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
		}

		/// <summary>
		///     Gets the last <see cref="DateTimeOffset" /> a user's last lockout expired, if any.
		///     Any time in the past should be indicates a user is not locked out.
		/// </summary>
		/// <param name="user">The user whose lockout date should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     A <see cref="Task{TResult}" /> that represents the result of the asynchronous query, a
		///     <see cref="DateTimeOffset" /> containing the last time
		///     a user's lockout expired, if any.
		/// </returns>
		public virtual Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.LockoutEnd);
		}

		/// <summary>
		///     Locks out a user until the specified end date has passed. Setting a end date in the past immediately unlocks a
		///     user.
		/// </summary>
		/// <param name="user">The user whose lockout date should be set.</param>
		/// <param name="lockoutEnd">
		///     The <see cref="DateTimeOffset" /> after which the <paramref name="user" />'s lockout should
		///     end.
		/// </param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.LockoutEnd = lockoutEnd;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Records that a failed access has occurred, incrementing the failed access count.
		/// </summary>
		/// <param name="user">The user whose cancellation count should be incremented.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the incremented failed access
		///     count.
		/// </returns>
		public virtual Task<int> IncrementAccessFailedCountAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.AccessFailedCount++;
			return Task.FromResult(user.AccessFailedCount);
		}

		/// <summary>
		///     Resets a user's failed access count.
		/// </summary>
		/// <param name="user">The user whose failed access count should be reset.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>This is typically called after the account is successfully accessed.</remarks>
		public virtual Task ResetAccessFailedCountAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.AccessFailedCount = 0;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Retrieves the current failed access count for the specified <paramref name="user" />..
		/// </summary>
		/// <param name="user">The user whose failed access count should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation, containing the failed access count.</returns>
		public virtual Task<int> GetAccessFailedCountAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.AccessFailedCount);
		}

		/// <summary>
		///     Retrieves a flag indicating whether user lockout can enabled for the specified user.
		/// </summary>
		/// <param name="user">The user whose ability to be locked out should be returned.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, true if a user can be locked out, otherwise
		///     false.
		/// </returns>
		public virtual Task<bool> GetLockoutEnabledAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.LockoutEnabled);
		}

		/// <summary>
		///     Set the flag indicating if the specified <paramref name="user" /> can be locked out..
		/// </summary>
		/// <param name="user">The user whose ability to be locked out should be set.</param>
		/// <param name="enabled">A flag indicating if lock out can be enabled for the specified <paramref name="user" />.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetLockoutEnabledAsync(TUser user, bool enabled,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.LockoutEnabled = enabled;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Gets the user identifier for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose identifier should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the identifier for the
		///     specified <paramref name="user" />.
		/// </returns>
		public virtual Task<string?> GetUserIdAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(ConvertIdToString(user.Id));
		}

		/// <summary>
		///     Gets the user name for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose name should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the name for the specified
		///     <paramref name="user" />.
		/// </returns>
		public virtual Task<string> GetUserNameAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.UserName);
		}

		/// <summary>
		///     Sets the given <paramref name="userName" /> for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose name should be set.</param>
		/// <param name="userName">The user name to set.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetUserNameAsync(TUser user, string userName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.UserName = userName;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Gets the normalized user name for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose normalized name should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the normalized user name for
		///     the specified <paramref name="user" />.
		/// </returns>
		public virtual Task<string> GetNormalizedUserNameAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.NormalizedUserName);
		}

		/// <summary>
		///     Sets the given normalized name for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose name should be set.</param>
		/// <param name="normalizedName">The normalized name to set.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetNormalizedUserNameAsync(TUser user, string normalizedName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.NormalizedUserName = normalizedName;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Creates the specified <paramref name="user" /> in the user store.
		/// </summary>
		/// <param name="user">The user to create.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the
		///     <see cref="IdentityResult" /> of the creation operation.
		/// </returns>
		public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (var db = GetConnection())
			{
				return await CreateAsync(db, user, cancellationToken);
			}
		}

		/// <inheritdoc cref="CreateAsync(TUser, CancellationToken)"/>
		protected virtual async Task<IdentityResult> CreateAsync(DataConnection db, TUser user, CancellationToken cancellationToken)
		{
			await Task.Run(() => db.TryInsertAndSetIdentity(user), cancellationToken);
			return IdentityResult.Success;
		}

		/// <summary>
		///     Updates the specified <paramref name="user" /> in the user store.
		/// </summary>
		/// <param name="user">The user to update.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the
		///     <see cref="IdentityResult" /> of the update operation.
		/// </returns>
		public async Task<IdentityResult> UpdateAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			using (var db = GetConnection())
			{
				return await UpdateAsync(db, user, cancellationToken);
			}
		}

		/// <inheritdoc cref="UpdateAsync(TUser, CancellationToken)"/>
		protected virtual async Task<IdentityResult> UpdateAsync(DataConnection db, TUser user, CancellationToken cancellationToken)
		{
			var result = await Task.Run(() => db.UpdateConcurrent<TUser, TKey>(user), cancellationToken);
			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <summary>
		///     Deletes the specified <paramref name="user" /> from the user store.
		/// </summary>
		/// <param name="user">The user to delete.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the
		///     <see cref="IdentityResult" /> of the update operation.
		/// </returns>
		public async Task<IdentityResult> DeleteAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (var db = GetConnection())
			{
				return await DeleteAsync(db, user, cancellationToken);
			}
		}

		/// <inheritdoc cref="DeleteAsync(TUser, CancellationToken)"/>
		protected virtual async Task<IdentityResult> DeleteAsync(DataConnection db, TUser user, CancellationToken cancellationToken)
		{
			var result = await Task.Run(() =>
				db.GetTable<TUser>()
					.Where(_ => _.Id.Equals(user.Id) && _.ConcurrencyStamp == user.ConcurrencyStamp)
					.Delete(), cancellationToken);
			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <summary>
		///     Finds and returns a user, if any, who has the specified <paramref name="userId" />.
		/// </summary>
		/// <param name="userId">The user ID to search for.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the user matching the specified
		///     <paramref name="userId" /> if it exists.
		/// </returns>
		public async Task<TUser> FindByIdAsync(string userId,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			var id = ConvertIdFromString(userId);

			using (var db = GetConnection())
			{
				return await FindByIdAsync(db, id, cancellationToken);
			}
		}

		/// <inheritdoc cref="FindByIdAsync(string, CancellationToken)"/>
		protected virtual async Task<TUser> FindByIdAsync(DataConnection db, TKey id, CancellationToken cancellationToken)
		{
			return await db.GetTable<TUser>().FirstOrDefaultAsync(_ => _.Id.Equals(id), cancellationToken);
		}

		/// <summary>
		///     Finds and returns a user, if any, who has the specified normalized user name.
		/// </summary>
		/// <param name="normalizedUserName">The normalized user name to search for.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the user matching the specified
		///     <paramref name="normalizedUserName" /> if it exists.
		/// </returns>
		public async Task<TUser> FindByNameAsync(string normalizedUserName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			using (var db = GetConnection())
			{
				return await FindByNameAsync(db, normalizedUserName, cancellationToken);
			}
		}

		/// <inheritdoc cref="FindByNameAsync(string, CancellationToken)"/>
		protected virtual async Task<TUser> FindByNameAsync(DataConnection db, string normalizedUserName,
			CancellationToken cancellationToken)
		{
			return await db.GetTable<TUser>()
				.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
		}

		/// <summary>
		///     Dispose the store
		/// </summary>
		public void Dispose()
		{
			_disposed = true;
		}

		/// <summary>
		///     Adds the <paramref name="login" /> given to the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user to add the login to.</param>
		/// <param name="login">The login to add to the user.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task AddLoginAsync(TUser user, UserLoginInfo login,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (login == null)
				throw new ArgumentNullException(nameof(login));

			using (var db = GetConnection())
			{
				await AddLoginAsync(db, user, login, cancellationToken);
			}
		}

		/// <inheritdoc cref="AddLoginAsync(TUser, UserLoginInfo, CancellationToken)"/>
		protected virtual async Task AddLoginAsync(DataConnection db, TUser user, UserLoginInfo login,
			CancellationToken cancellationToken)
		{
			await Task.Run(() => db.Insert(CreateUserLogin(user, login)), cancellationToken);
		}

		/// <summary>
		///     Removes the <paramref name="loginProvider" /> given from the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user to remove the login from.</param>
		/// <param name="loginProvider">The login to remove from the user.</param>
		/// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (var db = GetConnection())
			{
				await RemoveLoginAsync(db, user, loginProvider, providerKey, cancellationToken);
			}
		}

		/// <inheritdoc cref="RemoveLoginAsync(TUser, string, string, CancellationToken)"/>
		protected virtual async Task RemoveLoginAsync(DataConnection db, TUser user, string loginProvider, string providerKey,
			CancellationToken cancellationToken)
		{
			await Task.Run(() =>
					db
						.GetTable<TUserLogin>()
						.Delete(
							userLogin =>
								userLogin.UserId.Equals(user.Id) && userLogin.LoginProvider == loginProvider &&
								userLogin.ProviderKey == providerKey),
				cancellationToken);
		}

		/// <summary>
		///     Retrieves the associated logins for the specified
		///     <param ref="user" />
		///     .
		/// </summary>
		/// <param name="user">The user whose associated logins to retrieve.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> for the asynchronous operation, containing a list of <see cref="UserLoginInfo" /> for the
		///     specified <paramref name="user" />, if any.
		/// </returns>
		public async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (var db = GetConnection())
			{
				return await GetLoginsAsync(db, user, cancellationToken);
			}
		}

		/// <inheritdoc cref="GetLoginsAsync(TUser, CancellationToken)"/>
		protected virtual async Task<IList<UserLoginInfo>> GetLoginsAsync(DataConnection db, TUser user, CancellationToken cancellationToken)
		{
			var userId = user.Id;
			return await db
				.GetTable<TUserLogin>()
				.Where(l => l.UserId.Equals(userId))
				.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
				.ToListAsync(cancellationToken);
		}

		/// <summary>
		///     Retrieves the user associated with the specified login provider and login provider key..
		/// </summary>
		/// <param name="loginProvider">The login provider who provided the <paramref name="providerKey" />.</param>
		/// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> for the asynchronous operation, containing the user, if any which matched the specified
		///     login provider and key.
		/// </returns>
		public async Task<TUser> FindByLoginAsync(string loginProvider, string providerKey,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();

			using (var db = GetConnection())
			{
				return await FindByLoginAsync(db, loginProvider, providerKey, cancellationToken);
			}
		}

		/// <inheritdoc cref="FindByLoginAsync(string, string, CancellationToken)"/>
		protected virtual async Task<TUser> FindByLoginAsync(DataConnection db, string loginProvider, string providerKey,
			CancellationToken cancellationToken)
		{
			var q = from ul in db.GetTable<TUserLogin>()
				join u in db.GetTable<TUser>() on ul.UserId equals u.Id
				where ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey
				select u;

			return await q.FirstOrDefaultAsync(cancellationToken);
		}

		/// <summary>
		///     Sets the password hash for a user.
		/// </summary>
		/// <param name="user">The user to set the password hash for.</param>
		/// <param name="passwordHash">The password hash to set.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetPasswordHashAsync(TUser user, string passwordHash,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.PasswordHash = passwordHash;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Gets the password hash for a user.
		/// </summary>
		/// <param name="user">The user to retrieve the password hash for.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that contains the password hash for the user.</returns>
		public virtual Task<string> GetPasswordHashAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.PasswordHash);
		}

		/// <summary>
		///     Returns a flag indicating if the specified user has a password.
		/// </summary>
		/// <param name="user">The user to retrieve the password hash for.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     A <see cref="Task{TResult}" /> containing a flag indicating if the specified user has a password. If the
		///     user has a password the returned value with be true, otherwise it will be false.
		/// </returns>
		public virtual Task<bool> HasPasswordAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(user.PasswordHash != null);
		}

		/// <summary>
		///     Sets the telephone number for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose telephone number should be set.</param>
		/// <param name="phoneNumber">The telephone number to set.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetPhoneNumberAsync(TUser user, string phoneNumber,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.PhoneNumber = phoneNumber;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Gets the telephone number, if any, for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose telephone number should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the user's telephone number, if
		///     any.
		/// </returns>
		public virtual Task<string> GetPhoneNumberAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.PhoneNumber);
		}

		/// <summary>
		///     Gets a flag indicating whether the specified <paramref name="user" />'s telephone number has been confirmed.
		/// </summary>
		/// <param name="user">The user to return a flag for, indicating whether their telephone number is confirmed.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, returning true if the specified
		///     <paramref name="user" /> has a confirmed
		///     telephone number otherwise false.
		/// </returns>
		public virtual Task<bool> GetPhoneNumberConfirmedAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.PhoneNumberConfirmed);
		}

		/// <summary>
		///     Sets a flag indicating if the specified <paramref name="user" />'s phone number has been confirmed..
		/// </summary>
		/// <param name="user">The user whose telephone number confirmation status should be set.</param>
		/// <param name="confirmed">A flag indicating whether the user's telephone number has been confirmed.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.PhoneNumberConfirmed = confirmed;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Adds the given <paramref name="normalizedRoleName" /> to the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user to add the role to.</param>
		/// <param name="normalizedRoleName">The role to add.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task AddToRoleAsync(TUser user, string normalizedRoleName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(normalizedRoleName))
				throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			using (var db = GetConnection())
			{
				await AddToRoleAsync(db, user, normalizedRoleName, cancellationToken);
			}
		}

		/// <inheritdoc cref="AddToRoleAsync(TUser, string, CancellationToken)"/>
		protected virtual async Task AddToRoleAsync(DataConnection db, TUser user, string normalizedRoleName,
			CancellationToken cancellationToken)
		{
			await Task.Run(() =>
			{
				var roleEntity = db.GetTable<TRole>()
					.SingleOrDefault(r => r.NormalizedName == normalizedRoleName);
				if (roleEntity == null)
					throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.RoleNotFound,
						normalizedRoleName));
				db.TryInsertAndSetIdentity(CreateUserRole(user, roleEntity));
			}, cancellationToken);
		}

		/// <summary>
		///     Removes the given <paramref name="normalizedRoleName" /> from the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user to remove the role from.</param>
		/// <param name="normalizedRoleName">The role to remove.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task RemoveFromRoleAsync(TUser user, string normalizedRoleName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(normalizedRoleName))
				throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			using (var db = GetConnection())
			{
				await RemoveFromRoleAsync(db, user, normalizedRoleName, cancellationToken);
			}
		}

		/// <inheritdoc cref="RemoveFromRoleAsync(TUser, string, CancellationToken)"/>
		protected virtual async Task RemoveFromRoleAsync(DataConnection db, TUser user, string normalizedRoleName,
			CancellationToken cancellationToken)
		{
			await Task.Run(() =>
				{
					var q =
						from ur in db.GetTable<TUserRole>()
						join r in db.GetTable<TRole>() on ur.RoleId equals r.Id
						where r.NormalizedName == normalizedRoleName && ur.UserId.Equals(user.Id)
						select ur;

					q.Delete();
				},
				cancellationToken);
		}

		/// <summary>
		///     Retrieves the roles the specified <paramref name="user" /> is a member of.
		/// </summary>
		/// <param name="user">The user whose roles should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that contains the roles the user is a member of.</returns>
		public async Task<IList<string>> GetRolesAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			using (var db = GetConnection())
			{
				return await GetRolesAsync(db, user, cancellationToken);
			}
		}

		/// <inheritdoc cref="GetRolesAsync(TUser, CancellationToken)"/>
		protected virtual async Task<IList<string>> GetRolesAsync(DataConnection db, TUser user, CancellationToken cancellationToken)
		{
			var userId = user.Id;
			var query = from userRole in db.GetTable<TUserRole>()
				join role in db.GetTable<TRole>() on userRole.RoleId equals role.Id
				where userRole.UserId.Equals(userId)
				select role.Name;

			return await query.ToListAsync(cancellationToken);
		}

		/// <summary>
		///     Returns a flag indicating if the specified user is a member of the give <paramref name="normalizedRoleName" />.
		/// </summary>
		/// <param name="user">The user whose role membership should be checked.</param>
		/// <param name="normalizedRoleName">The role to check membership of</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     A <see cref="Task{TResult}" /> containing a flag indicating if the specified user is a member of the given group.
		///     If the
		///     user is a member of the group the returned value with be true, otherwise it will be false.
		/// </returns>
		public async Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(normalizedRoleName))
				throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));

			using (var db = GetConnection())
			{
				return await IsInRoleAsync(db, user, normalizedRoleName, cancellationToken);
			}
		}

		/// <inheritdoc cref="IsInRoleAsync(TUser, string,CancellationToken)"/>
		protected virtual async Task<bool> IsInRoleAsync(DataConnection db, TUser user, string normalizedRoleName,
			CancellationToken cancellationToken)
		{
			var q = from ur in db.GetTable<TUserRole>()
				join r in db.GetTable<TRole>() on ur.RoleId equals r.Id
				where r.NormalizedName == normalizedRoleName && ur.UserId.Equals(user.Id)
				select ur;

			return await q.AnyAsync(cancellationToken);
		}

		/// <summary>
		///     Retrieves all users in the specified role.
		/// </summary>
		/// <param name="normalizedRoleName">The role whose users should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> contains a list of users, if any, that are in the specified role.
		/// </returns>
		public async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (string.IsNullOrEmpty(normalizedRoleName))
				throw new ArgumentNullException(nameof(normalizedRoleName));

			using (var db = GetConnection())
			{
				return await GetUsersInRoleAsync(db, normalizedRoleName, cancellationToken);
			}
		}

		/// <inheritdoc cref="GetUsersInRoleAsync(string, CancellationToken)"/>
		protected virtual async Task<IList<TUser>> GetUsersInRoleAsync(DataConnection db, string normalizedRoleName,
			CancellationToken cancellationToken)
		{
			var query = from userrole in db.GetTable<TUserRole>()
				join user in db.GetTable<TUser>() on userrole.UserId equals user.Id
				join role in db.GetTable<TRole>() on userrole.RoleId equals role.Id
				where role.NormalizedName == normalizedRoleName
				select user;

			return await query.ToListAsync(cancellationToken);
		}

		/// <summary>
		///     Sets the provided security <paramref name="stamp" /> for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose security stamp should be set.</param>
		/// <param name="stamp">The security stamp to set.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetSecurityStampAsync(TUser user, string stamp,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.SecurityStamp = stamp;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Get the security stamp for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose security stamp should be set.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing the security stamp for the
		///     specified <paramref name="user" />.
		/// </returns>
		public virtual Task<string> GetSecurityStampAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.SecurityStamp);
		}

		/// <summary>
		///     Sets a flag indicating whether the specified <paramref name="user" /> has two factor authentication enabled or not,
		///     as an asynchronous operation.
		/// </summary>
		/// <param name="user">The user whose two factor authentication enabled status should be set.</param>
		/// <param name="enabled">
		///     A flag indicating whether the specified <paramref name="user" /> has two factor authentication
		///     enabled.
		/// </param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetTwoFactorEnabledAsync(TUser user, bool enabled,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			user.TwoFactorEnabled = enabled;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Returns a flag indicating whether the specified <paramref name="user" /> has two factor authentication enabled or
		///     not,
		///     as an asynchronous operation.
		/// </summary>
		/// <param name="user">The user whose two factor authentication enabled status should be set.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>
		///     The <see cref="Task" /> that represents the asynchronous operation, containing a flag indicating whether the
		///     specified
		///     <paramref name="user" /> has two factor authentication enabled or not.
		/// </returns>
		public virtual Task<bool> GetTwoFactorEnabledAsync(TUser user,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			return Task.FromResult(user.TwoFactorEnabled);
		}

		/// <summary>
		///     Creates a new entity to represent a user role.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		protected virtual TUserRole CreateUserRole(TUser user, TRole role)
		{
			return new TUserRole()
			{
				UserId = user.Id,
				RoleId = role.Id
			};
		}

		/// <summary>
		///     Create a new entity representing a user claim.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="claim"></param>
		/// <returns></returns>
		protected virtual TUserClaim CreateUserClaim(TUser user, Claim claim)
		{
			var res = new TUserClaim() {UserId = user.Id};
			res.InitializeFromClaim(claim);
			return res;
		}

		/// <summary>
		///     Create a new entity representing a user login.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="login"></param>
		/// <returns></returns>
		protected virtual TUserLogin CreateUserLogin(TUser user, UserLoginInfo login)
		{
			return new TUserLogin()
			{
				UserId = user.Id,
				LoginProvider = login.LoginProvider,
				ProviderDisplayName = login.ProviderDisplayName,
				ProviderKey = login.ProviderKey
			};
		}

		/// <summary>
		///     Create a new entity representing a user token.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="loginProvider"></param>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		protected virtual TUserToken CreateUserToken(TUser user, string loginProvider, string name, string value)
		{
			return new TUserToken()
			{
				UserId = user.Id,
				LoginProvider = loginProvider,
				Name = name,
				Value = value
			};
		}

		/// <summary>
		///     Converts the provided <paramref name="id" /> to a strongly typed key object.
		/// </summary>
		/// <param name="id">The id to convert.</param>
		/// <returns>An instance of <typeparamref name="TKey" /> representing the provided <paramref name="id" />.</returns>
		public virtual TKey ConvertIdFromString(string? id)
		{
			if (id == null)
				return default!;
			return (TKey) TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(id);
		}

		/// <summary>
		///     Converts the provided <paramref name="id" /> to its string representation.
		/// </summary>
		/// <param name="id">The id to convert.</param>
		/// <returns>An <see cref="string" /> representation of the provided <paramref name="id" />.</returns>
		public virtual string? ConvertIdToString(TKey id)
		{
			if (Equals(id, default(TKey)))
				return null;
			return id.ToString();
		}

		/// <summary>
		///     Throws if this class has been disposed.
		/// </summary>
		protected void ThrowIfDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		private const string InternalLoginProvider = "[AspNetUserStore]";
		private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
		private const string RecoveryCodeTokenName = "RecoveryCodes";

		/// <summary>
		/// Sets the authenticator key for the specified <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The user whose authenticator key should be set.</param>
		/// <param name="key">The authenticator key to set.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
		public virtual Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken)
			=> SetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);

		/// <summary>
		/// Get the authenticator key for the specified <paramref name="user" />.
		/// </summary>
		/// <param name="user">The user whose security stamp should be set.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
		/// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the security stamp for the specified <paramref name="user"/>.</returns>
		public virtual Task<string?> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken)
			=> GetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, cancellationToken);
	}
}
