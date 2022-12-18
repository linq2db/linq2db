using System;
using System.Collections.Generic;
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
	/// <typeparam name="TUserRole">The type of the class representing a user role.</typeparam>
	/// <typeparam name="TRoleClaim">The type of the class representing a role claim.</typeparam>
	public class RoleStore<TRole, TContext, TKey, TUserRole, TRoleClaim> :
		RoleStoreBase<TRole, TKey, TUserRole, TRoleClaim>
		where TRole      : IdentityRole<TKey>
		where TKey       : IEquatable<TKey>
		where TContext   : IDataContext
		where TUserRole  : IdentityUserRole<TKey>, new()
		where TRoleClaim : IdentityRoleClaim<TKey>, new()
	{
		/// <summary>
		/// Constructs a new instance of <see cref="RoleStore{TRole, TContext, TKey, TUserRole, TRoleClaim}"/>.
		/// </summary>
		/// <param name="context">The <see cref="IDataContext"/>.</param>
		/// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
		public RoleStore(TContext context, IdentityErrorDescriber? describer = null)
			: base(describer ?? new IdentityErrorDescriber())
		{
			Context = context ?? throw new ArgumentNullException(nameof(context));
		}

		/// <summary>
		/// Gets the database context for this store.
		/// </summary>
		public virtual TContext Context { get; }

		/// <inheritdoc cref="Roles"/>
		public    override IQueryable<TRole>      Roles      => Context.GetTable<TRole>();
		/// <summary>
		/// A navigation property for the role claims the store contains.
		/// </summary>
		protected virtual  IQueryable<TRoleClaim> RoleClaims => Context.GetTable<TRoleClaim>();

		/// <inheritdoc cref="CreateAsync(TRole, CancellationToken)"/>
		public override async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (role == null) throw new ArgumentNullException(nameof(role));

			await Context.InsertAndSetIdentity(role, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return IdentityResult.Success;
		}

		/// <inheritdoc cref="UpdateAsync(TRole, CancellationToken)"/>
		public override async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (role == null) throw new ArgumentNullException(nameof(role));

			var result = await Context.UpdateConcurrent(role, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <inheritdoc cref="DeleteAsync(TRole, CancellationToken)"/>
		public override async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (role == null) throw new ArgumentNullException(nameof(role));

			var result = await Roles
				.Where(_ => _.Id.Equals(role.Id) && _.ConcurrencyStamp == role.ConcurrencyStamp)
				.DeleteAsync(cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return result == 1 ? IdentityResult.Success : IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
		}

		/// <inheritdoc cref="DeleteAsync(TRole, CancellationToken)"/>
		public override Task<TRole?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			var roleId = ConvertIdFromString(id);

			return Roles.FirstOrDefaultAsync(u => u.Id.Equals(roleId!), cancellationToken);
		}

		/// <inheritdoc cref="FindByNameAsync(string, CancellationToken)"/>
		public override Task<TRole?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			return Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
		}

		/// <inheritdoc cref="GetClaimsAsync(TRole, CancellationToken)"/>
		public override async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (role == null) throw new ArgumentNullException(nameof(role));

			return await RoleClaims
				.Where(rc => rc.RoleId.Equals(role.Id))
				.Select(c => c.ToClaim())
				.ToListAsync(cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="AddClaimAsync(TRole, Claim, CancellationToken)"/>
		public override async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (role  == null) throw new ArgumentNullException(nameof(role));
			if (claim == null) throw new ArgumentNullException(nameof(claim));

			await Context.InsertAndSetIdentity(CreateRoleClaim(role, claim), cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		/// <inheritdoc cref="RemoveClaimAsync(TRole, Claim, CancellationToken)"/>
		public override async Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			if (role  == null) throw new ArgumentNullException(nameof(role));
			if (claim == null) throw new ArgumentNullException(nameof(claim));

			await RoleClaims
				.Where(rc => rc.RoleId.Equals(role.Id) && rc.ClaimValue == claim.Value && rc.ClaimType == claim.Type)
				.DeleteAsync(cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
	}
}
