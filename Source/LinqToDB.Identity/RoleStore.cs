// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Creates a new instance of a persistence store for roles.
	/// </summary>
	/// <typeparam name="TRole">The type of the class representing a role.</typeparam>
	public class RoleStore<TRole> : RoleStore<string, TRole>
		where TRole : IdentityRole<string>
	{
		/// <summary>
		///     Constructs a new instance of <see cref="LinqToDB.Identity.RoleStore{TConnection,TRole}" />.
		/// </summary>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber" />.</param>
		public RoleStore(IConnectionFactory factory, IdentityErrorDescriber? describer = null)
			: base(factory, describer)
		{
		}
	}

	/// <summary>
	///     Creates a new instance of a persistence store for roles.
	/// </summary>
	/// <typeparam name="TRole">The type of the class representing a role.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	public class RoleStore<TKey, TRole> :
		RoleStore<TKey, TRole, IdentityRoleClaim<TKey>>
		where TRole : IdentityRole<TKey>
		where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     Constructs a new instance of <see cref="RoleStore{TRole,TKey,TRoleClaim}" />.
		/// </summary>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber" />.</param>
		public RoleStore(IConnectionFactory factory, IdentityErrorDescriber? describer = null)
			: base(factory, describer)
		{
		}

	}

	/// <summary>
	///     Creates a new instance of a persistence store for roles.
	/// </summary>
	/// <typeparam name="TRole">The type of the class representing a role.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
	/// <typeparam name="TRoleClaim">The type of the class representing a role claim.</typeparam>
	public class RoleStore<TKey, TRole, TRoleClaim> :
		IQueryableRoleStore<TRole>,
		IRoleClaimStore<TRole>
		where TRole : class, IIdentityRole<TKey>
		where TKey : IEquatable<TKey>
		where TRoleClaim : class, IIdentityRoleClaim<TKey>, new()
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
		///     Constructs a new instance of <see cref="RoleStore{TRole,TKey,TRoleClaim}" />.
		/// </summary>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber" />.</param>
		public RoleStore(IConnectionFactory factory, IdentityErrorDescriber? describer = null)
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
		///     Creates a new role in a store as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role to create in the store.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that represents the <see cref="IdentityResult" /> of the asynchronous query.</returns>
		public async Task<IdentityResult> CreateAsync(TRole role,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));

			using (var db = GetConnection())
				return await CreateAsync(db, role, cancellationToken);
		}

		/// <inheritdoc cref="CreateAsync(TRole,CancellationToken)"/>
		protected virtual async Task<IdentityResult> CreateAsync(DataConnection db, TRole role, CancellationToken cancellationToken)
		{
			await Task.Run(() => db.TryInsertAndSetIdentity(role), cancellationToken);
			return IdentityResult.Success;
			
		}



		/// <summary>
		///     Updates a role in a store as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role to update in the store.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that represents the <see cref="IdentityResult" /> of the asynchronous query.</returns>
		public async Task<IdentityResult> UpdateAsync(TRole role,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));

			using (var db = GetConnection())
				return await UpdateAsync(db, role, cancellationToken);
		}

		/// <inheritdoc cref="UpdateAsync(TRole, CancellationToken)"/>
		protected virtual async Task<IdentityResult> UpdateAsync(DataConnection db, TRole role, CancellationToken cancellationToken)
		{
			var result = await Task.Run(() => db.UpdateConcurrent<TRole, TKey>(role), cancellationToken);
			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <summary>
		///     Deletes a role from the store as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role to delete from the store.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that represents the <see cref="IdentityResult" /> of the asynchronous query.</returns>
		public async Task<IdentityResult> DeleteAsync(TRole role,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));

			using (var db = GetConnection())
				return await DeleteAsync(db, role, cancellationToken);
		}

		/// <inheritdoc cref="DeleteAsync(TRole, CancellationToken)"/>
		private async Task<IdentityResult> DeleteAsync(DataConnection db, TRole role, CancellationToken cancellationToken)
		{
			var result = await Task.Run(() =>
				db.GetTable<TRole>()
					.Where(_ => _.Id.Equals(role.Id) && _.ConcurrencyStamp == role.ConcurrencyStamp)
					.Delete(), cancellationToken);

			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <summary>
		///     Gets the ID for a role from the store as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role whose ID should be returned.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that contains the ID of the role.</returns>
		public Task<string?> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));
			return Task.FromResult(ConvertIdToString(role.Id));
		}

		/// <summary>
		///     Gets the name of a role from the store as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role whose name should be returned.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that contains the name of the role.</returns>
		public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));
			return Task.FromResult(role.Name);
		}

		/// <summary>
		///     Sets the name of a role in the store as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role whose name should be set.</param>
		/// <param name="roleName">The name of the role.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public Task SetRoleNameAsync(TRole role, string roleName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));
			role.Name = roleName;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Finds the role who has the specified ID as an asynchronous operation.
		/// </summary>
		/// <param name="id">The role ID to look for.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that result of the look up.</returns>
		public async Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			var roleId = ConvertIdFromString(id);

			using (var db = GetConnection())
				return await FindByIdAsync(db, roleId, cancellationToken);
		}

		/// <inheritdoc cref="FindByIdAsync(string, CancellationToken)"/>
		protected virtual async Task<TRole> FindByIdAsync(DataConnection db, TKey roleId, CancellationToken cancellationToken)
		{
			return await db.GetTable<TRole>().FirstOrDefaultAsync(u => u.Id.Equals(roleId), cancellationToken);
		}

		/// <summary>
		///     Finds the role who has the specified normalized name as an asynchronous operation.
		/// </summary>
		/// <param name="normalizedName">The normalized role name to look for.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that result of the look up.</returns>
		public async Task<TRole> FindByNameAsync(string normalizedName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			using (var db = GetConnection())
			{
				return await FindByNameAsync(db, normalizedName, cancellationToken);
			}

		}

		/// <inheritdoc cref="FindByNameAsync(string, CancellationToken)"/>
		protected virtual async Task<TRole> FindByNameAsync(DataConnection db, string normalizedName, CancellationToken cancellationToken)
		{
			return await db.GetTable<TRole>().FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
		}

		/// <summary>
		///     Get a role's normalized name as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role whose normalized name should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that contains the name of the role.</returns>
		public virtual Task<string> GetNormalizedRoleNameAsync(TRole role,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));
			return Task.FromResult(role.NormalizedName);
		}

		/// <summary>
		///     Set a role's normalized name as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role whose normalized name should be set.</param>
		/// <param name="normalizedName">The normalized name to set</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public virtual Task SetNormalizedRoleNameAsync(TRole role, string normalizedName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));
			role.NormalizedName = normalizedName;
			return TaskCache.CompletedTask;
		}

		/// <summary>
		///     Dispose the stores
		/// </summary>
		public void Dispose()
		{
			_disposed = true;
		}

		/// <summary>
		///     A navigation property for the roles the store contains.
		/// </summary>
		public virtual IQueryable<TRole> Roles => GetContext().GetTable<TRole>();

		/// <summary>
		///     Get the claims associated with the specified <paramref name="role" /> as an asynchronous operation.
		/// </summary>
		/// <param name="role">The role whose claims should be retrieved.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>A <see cref="Task{TResult}" /> that contains the claims granted to a role.</returns>
		public async Task<IList<Claim>> GetClaimsAsync(TRole role,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));

			using (var db = GetConnection())
			{
				return await GetClaimsAsync(db, role, cancellationToken);
			}
		}

		/// <inheritdoc cref="GetClaimsAsync(TRole, CancellationToken)"/>
		protected virtual async Task<IList<Claim>> GetClaimsAsync(DataConnection db, TRole role, CancellationToken cancellationToken)
		{
			return await db.GetTable<TRoleClaim>()
				.Where(rc => rc.RoleId.Equals(role.Id))
				.Select(c => c.ToClaim())
				.ToListAsync(cancellationToken);
		}

		/// <summary>
		///     Adds the <paramref name="claim" /> given to the specified <paramref name="role" />.
		/// </summary>
		/// <param name="role">The role to add the claim to.</param>
		/// <param name="claim">The claim to add to the role.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
		{
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));
			if (claim == null)
				throw new ArgumentNullException(nameof(claim));

			using (var db = GetConnection())
				await AddClaimAsync(db, role, claim, cancellationToken);
		}

		/// <inheritdoc cref="AddClaimAsync(TRole, Claim, CancellationToken)"/>
		protected virtual async Task AddClaimAsync(DataConnection db, TRole role, Claim claim, CancellationToken cancellationToken)
		{
			await Task.Run(() => db.TryInsertAndSetIdentity(CreateRoleClaim(role, claim)),
				cancellationToken);
		}

		/// <summary>
		///     Removes the <paramref name="claim" /> given from the specified <paramref name="role" />.
		/// </summary>
		/// <param name="role">The role to remove the claim from.</param>
		/// <param name="claim">The claim to remove from the role.</param>
		/// <param name="cancellationToken">
		///     The <see cref="CancellationToken" /> used to propagate notifications that the operation
		///     should be canceled.
		/// </param>
		/// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
		public async Task RemoveClaimAsync(TRole role, Claim claim,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			ThrowIfDisposed();
			if (role == null)
				throw new ArgumentNullException(nameof(role));
			if (claim == null)
				throw new ArgumentNullException(nameof(claim));

			using (var db = GetConnection())
			{
				await RemoveClaimAsync(db, role, claim, cancellationToken);
			}
		}

		/// <inheritdoc cref="RemoveClaimAsync(TRole, Claim, CancellationToken)"/>
		protected virtual async Task RemoveClaimAsync(DataConnection db, TRole role, Claim claim, CancellationToken cancellationToken)
		{
			await Task.Run(() =>
					db.GetTable<TRoleClaim>()
						.Where(rc => rc.RoleId.Equals(role.Id) && rc.ClaimValue == claim.Value && rc.ClaimType == claim.Type)
						.Delete(),
				cancellationToken);
		}

		/// <summary>
		///     Converts the provided <paramref name="id" /> to a strongly typed key object.
		/// </summary>
		/// <param name="id">The id to convert.</param>
		/// <returns>An instance of <typeparamref name="TKey" /> representing the provided <paramref name="id" />.</returns>
		public virtual TKey ConvertIdFromString(string id)
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
			if (id.Equals(default(TKey)!))
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


		/// <summary>
		///     Creates a entity representing a role claim.
		/// </summary>
		/// <param name="role">The associated role.</param>
		/// <param name="claim">The associated claim.</param>
		/// <returns>The role claim entity.</returns>
		protected virtual TRoleClaim CreateRoleClaim(TRole role, Claim claim)
		{
			var roleClaim = new TRoleClaim(){ RoleId = role.Id };
			roleClaim.InitializeFromClaim(claim);
			return roleClaim;
		}
	}
}
