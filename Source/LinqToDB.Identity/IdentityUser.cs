// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using LinqToDB.Mapping;

namespace LinqToDB.Identity
{
	/// <summary>
	///     The default implementation of <see cref="IdentityUser{TKey}" /> which uses a string as a primary key.
	/// </summary>
	public class IdentityUser : IdentityUser<string>
	{
		/// <summary>
		///     Initializes a new instance of <see cref="IdentityUser" />.
		/// </summary>
		/// <remarks>
		///     The Id property is initialized to from a new GUID string value.
		/// </remarks>
		public IdentityUser()
		{
			Id = Guid.NewGuid().ToString();
		}

		/// <summary>
		///     Initializes a new instance of <see cref="IdentityUser" />.
		/// </summary>
		/// <param name="userName">The user name.</param>
		/// <remarks>
		///     The Id property is initialized to from a new GUID string value.
		/// </remarks>
		public IdentityUser(string userName) : this()
		{
			UserName = userName;
		}
	}

	/// <summary>
	///     Represents a user in the identity system
	/// </summary>
	/// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
	public class IdentityUser<TKey> :
		IdentityUser<TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>>
		where TKey : IEquatable<TKey>
	{
	}

	/// <summary>
	///     Represents a user in the identity system
	/// </summary>
	/// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
	/// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
	/// <typeparam name="TUserRole">The type representing a user role.</typeparam>
	/// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
	public class IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin> : IIdentityUser<TKey> where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     <see cref="Claims" /> storage
		/// </summary>
		protected ICollection<TUserClaim> _claims = new List<TUserClaim>();

		/// <summary>
		///     <see cref="Logins" /> storage
		/// </summary>
		protected ICollection<TUserLogin> _logins = new List<TUserLogin>();

		/// <summary>
		///     <see cref="Roles" /> storage
		/// </summary>
		protected ICollection<TUserRole> _roles = new List<TUserRole>();

		/// <summary>
		///     Initializes a new instance of <see cref="IdentityUser{TKey}" />.
		/// </summary>
		public IdentityUser()
		{
		}

		/// <summary>
		///     Initializes a new instance of <see cref="IdentityUser{TKey}" />.
		/// </summary>
		/// <param name="userName">The user name.</param>
		public IdentityUser(string userName) : this()
		{
			UserName = userName;
		}

		/// <summary>
		///     Navigation property for the roles this user belongs to.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(IdentityUserRole<TKey>.UserId), Storage = nameof(_roles))]
		public virtual ICollection<TUserRole> Roles => _roles;

		/// <summary>
		///     Navigation property for the claims this user possesses.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(IdentityUserClaim<TKey>.UserId), Storage = nameof(_claims))]
		public virtual ICollection<TUserClaim> Claims => _claims;

		/// <summary>
		///     Navigation property for this users login accounts.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(IdentityUserLogin<TKey>.UserId), Storage = nameof(_logins))]
		public virtual ICollection<TUserLogin> Logins => _logins;

		/// <summary>
		///     Gets or sets the primary key for this user.
		/// </summary>
		[PrimaryKey]
		[Column(CanBeNull = false, IsPrimaryKey = true, Length = 255)]
		public virtual TKey Id { get; set; } = default!;

		/// <summary>
		///     Gets or sets the user name for this user.
		/// </summary>
		public virtual string UserName { get; set; } = default!;

		/// <summary>
		///     Gets or sets the normalized user name for this user.
		/// </summary>
		public virtual string NormalizedUserName { get; set; } = default!;

		/// <summary>
		///     Gets or sets the email address for this user.
		/// </summary>
		public virtual string Email { get; set; } = default!;

		/// <summary>
		///     Gets or sets the normalized email address for this user.
		/// </summary>
		public virtual string NormalizedEmail { get; set; } = default!;

		/// <summary>
		///     Gets or sets a flag indicating if a user has confirmed their email address.
		/// </summary>
		/// <value>True if the email address has been confirmed, otherwise false.</value>
		public virtual bool EmailConfirmed { get; set; } = default!;

		/// <summary>
		///     Gets or sets a salted and hashed representation of the password for this user.
		/// </summary>
		public virtual string PasswordHash { get; set; } = default!;

		/// <summary>
		///     A random value that must change whenever a users credentials change (password changed, login removed)
		/// </summary>
		public virtual string SecurityStamp { get; set; } = default!;

		/// <summary>
		///     A random value that must change whenever a user is persisted to the store
		/// </summary>
		public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		///     Gets or sets a telephone number for the user.
		/// </summary>
		public virtual string PhoneNumber { get; set; } = default!;

		/// <summary>
		///     Gets or sets a flag indicating if a user has confirmed their telephone address.
		/// </summary>
		/// <value>True if the telephone number has been confirmed, otherwise false.</value>
		public virtual bool PhoneNumberConfirmed { get; set; } = default!;

		/// <summary>
		///     Gets or sets a flag indicating if two factor authentication is enabled for this user.
		/// </summary>
		/// <value>True if 2fa is enabled, otherwise false.</value>
		public virtual bool TwoFactorEnabled { get; set; } = default!;

		/// <summary>
		///     Gets or sets the date and time, in UTC, when any user lockout ends.
		/// </summary>
		/// <remarks>
		///     A value in the past means the user is not locked out.
		/// </remarks>
		public virtual DateTimeOffset? LockoutEnd { get; set; } = default!;

		/// <summary>
		///     Gets or sets a flag indicating if the user could be locked out.
		/// </summary>
		/// <value>True if the user could be locked out, otherwise false.</value>
		public virtual bool LockoutEnabled { get; set; } = default!;

		/// <summary>
		///     Gets or sets the number of failed login attempts for the current user.
		/// </summary>
		public virtual int AccessFailedCount { get; set; } = default!;

		/// <summary>
		///     Returns the username for this user.
		/// </summary>
		public override string ToString()
		{
			return UserName;
		}
	}
}
