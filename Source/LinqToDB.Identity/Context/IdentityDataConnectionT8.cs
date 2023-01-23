﻿using System;
using LinqToDB.Configuration;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Base class for the LinqToDB database context used for identity.
	/// </summary>
	/// <typeparam name="TUser">The type of user objects.</typeparam>
	/// <typeparam name="TRole">The type of role objects.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for users and roles.</typeparam>
	/// <typeparam name="TUserClaim">The type of the user claim object.</typeparam>
	/// <typeparam name="TUserRole">The type of the user role object.</typeparam>
	/// <typeparam name="TUserLogin">The type of the user login object.</typeparam>
	/// <typeparam name="TRoleClaim">The type of the role claim object.</typeparam>
	/// <typeparam name="TUserToken">The type of the user token object.</typeparam>
	public class IdentityDataConnection<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> :
		IdentityDataConnection<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
		where TUser      : IdentityUser<TKey>
		where TRole      : IdentityRole<TKey>
		where TKey       : IEquatable<TKey>
		where TUserClaim : IdentityUserClaim<TKey>
		where TUserRole  : IdentityUserRole<TKey>
		where TUserLogin : IdentityUserLogin<TKey>
		where TRoleClaim : IdentityRoleClaim<TKey>
		where TUserToken : IdentityUserToken<TKey>
	{
		/// <summary>
		/// Constructor with options.
		/// </summary>
		/// <param name="options">Connection options.</param>
		//public IdentityDataConnection(DataOptions options)
		public IdentityDataConnection(LinqToDBConnectionOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataConnection()
		{
		}

		protected override void ConfigureMappings(MappingSchema mappingSchema)
		{
			base.ConfigureMappings(mappingSchema);

			var builder = mappingSchema.GetFluentMappingBuilder();

			DefaultMappings.SetupIdentityRole     <TKey, TRole     >(builder);
			DefaultMappings.SetupIdentityUserRole <TKey, TUserRole >(builder);
			DefaultMappings.SetupIdentityRoleClaim<TKey, TRoleClaim>(builder);
		}

		/// <summary>
		/// Gets the <see cref="ITable{TEntity}" /> of User roles.
		/// </summary>
		public ITable<TUserRole> UserRoles => this.GetTable<TUserRole>();

		/// <summary>
		/// Gets the <see cref="ITable{TEntity}" /> of roles.
		/// </summary>
		public ITable<TRole> Roles => this.GetTable<TRole>();

		/// <summary>
		/// Gets the <see cref="ITable{TEntity}" /> of role claims.
		/// </summary>
		public ITable<TRoleClaim> RoleClaims => this.GetTable<TRoleClaim>();
	}
}
