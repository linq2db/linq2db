// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	///     Contains extension methods to <see cref="IdentityBuilder" /> for adding linq2db stores.
	/// </summary>
	public static class IdentityLinqToDbBuilderExtensions
	{
		/// <summary>
		///     Adds an linq2db plementation of identity information stores.
		/// </summary>
		/// <param name="builder">The <see cref="IdentityBuilder" /> instance this method extends.</param>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <returns>The <see cref="IdentityBuilder" /> instance this method extends.</returns>
		// ReSharper disable once InconsistentNaming
		public static IdentityBuilder AddLinqToDBStores(this IdentityBuilder builder, IConnectionFactory factory)
		{
			return AddLinqToDBStores(builder, factory,
				typeof(string), 
				typeof(LinqToDB.Identity.IdentityUserClaim<string>), 
				typeof(LinqToDB.Identity.IdentityUserRole<string>), 
				typeof(LinqToDB.Identity.IdentityUserLogin<string>), 
				typeof(LinqToDB.Identity.IdentityUserToken<string>), 
				typeof(LinqToDB.Identity.IdentityRoleClaim<string>));
		}

		/// <summary>
		///     Adds an linq2db implementation of identity information stores.
		/// </summary>
		/// <typeparam name="TKey">The type of the primary key used for the users and roles.</typeparam>
		/// <param name="builder">The <see cref="IdentityBuilder" /> instance this method extends.</param>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <returns>The <see cref="IdentityBuilder" /> instance this method extends.</returns>
		// ReSharper disable once InconsistentNaming
		public static IdentityBuilder AddLinqToDBStores<TKey>(this IdentityBuilder builder, IConnectionFactory factory)
			where TKey : IEquatable<TKey>
		{
			return AddLinqToDBStores(builder, factory,
				typeof(TKey),
				typeof(LinqToDB.Identity.IdentityUserClaim<TKey>),
				typeof(LinqToDB.Identity.IdentityUserRole<TKey>),
				typeof(LinqToDB.Identity.IdentityUserLogin<TKey>),
				typeof(LinqToDB.Identity.IdentityUserToken<TKey>),
				typeof(LinqToDB.Identity.IdentityRoleClaim<TKey>));
		}

		/// <summary>
		///     Adds an linq2db implementation of identity information stores.
		/// </summary>
		/// <typeparam name="TKey">The type of the primary key used for the users and roles.</typeparam>
		/// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
		/// <typeparam name="TUserRole">The type representing a user role.</typeparam>
		/// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
		/// <typeparam name="TUserToken">The type representing a user token.</typeparam>
		/// <typeparam name="TRoleClaim">The type of the class representing a role claim.</typeparam>
		/// <param name="builder">The <see cref="IdentityBuilder" /> instance this method extends.</param>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <returns>The <see cref="IdentityBuilder" /> instance this method extends.</returns>
		// ReSharper disable once InconsistentNaming
		public static IdentityBuilder AddLinqToDBStores<
			TKey, 
			TUserClaim, 
			TUserRole, 
			TUserLogin, 
			TUserToken, 
			TRoleClaim>(this IdentityBuilder builder, IConnectionFactory factory)
			where TUserClaim : class, IIdentityUserClaim<TKey>
			where TUserRole : class, IIdentityUserRole<TKey>
			where TUserLogin : class, IIdentityUserLogin<TKey>
			where TUserToken : class, IIdentityUserToken<TKey>
			where TKey : IEquatable<TKey>
			where TRoleClaim : class, IIdentityRoleClaim<TKey>
		{

			return AddLinqToDBStores(builder, factory,
				typeof(TKey), 
				typeof(TUserClaim), 
				typeof(TUserRole), 
				typeof(TUserLogin), 
				typeof(TUserToken), 
				typeof(TRoleClaim));
		}

		/// <summary>
		///     Adds an linq2db implementation of identity information stores.
		/// </summary>
		/// <param name="builder">The <see cref="IdentityBuilder" /> instance this method extends.</param>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <param name="keyType">The type of the primary key used for the users and roles.</param>
		/// <param name="userClaimType">The type representing a claim.</param>
		/// <param name="userRoleType">The type representing a user role.</param>
		/// <param name="userLoginType">The type representing a user external login.</param>
		/// <param name="userTokenType">The type representing a user token.</param>
		/// <param name="roleClaimType">The type of the class representing a role claim.</param>
		/// <returns>The <see cref="IdentityBuilder" /> instance this method extends.</returns>
		// ReSharper disable once InconsistentNaming
		public static IdentityBuilder AddLinqToDBStores(this IdentityBuilder builder, IConnectionFactory factory,
			Type keyType, Type userClaimType, Type userRoleType, Type userLoginType, Type userTokenType, Type roleClaimType)
		{
			builder.Services.AddSingleton(factory);

			builder.Services.TryAdd(GetDefaultServices(
				keyType, 
				builder.UserType, 
				userClaimType, 
				userRoleType, 
				userLoginType, 
				userTokenType, 
				builder.RoleType, 
				roleClaimType));

			return builder;
		}

		private static IServiceCollection GetDefaultServices(Type keyType, Type userType, Type userClaimType, Type userRoleType, Type userLoginType, Type userTokenType, Type roleType, Type roleClaimType)
		{
			//UserStore<TKey, TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken>
			var userStoreType = typeof(UserStore<,,,,,,>).MakeGenericType(keyType, userType, roleType, userClaimType, userRoleType, userLoginType, userTokenType);
			// RoleStore<TKey, TRole, TRoleClaim>
			var roleStoreType = typeof(RoleStore<,,>).MakeGenericType(keyType, roleType, roleClaimType);

			var services = new ServiceCollection();
			services.AddScoped(
				typeof(IUserStore<>).MakeGenericType(userType),
				userStoreType);
			services.AddScoped(
				typeof(IRoleStore<>).MakeGenericType(roleType),
				roleStoreType);
			return services;
		}
	}
}