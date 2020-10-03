// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data;
using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Base class for the LinqToDB database context used for identity.
	/// </summary>
	public class IdentityDataConnection : IdentityDataConnection<IdentityUser, IdentityRole, string>
	{
		#region Constructors 

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /></param>
		/// <param name="connection">Connection object <see cref="IDbConnection" /></param>
		public IdentityDataConnection(IDataProvider dataProvider, IDbConnection connection)
			: base(dataProvider, connection)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /></param>
		/// <param name="transaction">Transdaction object <see cref="IDbTransaction" /></param>
		public IdentityDataConnection(IDataProvider dataProvider, IDbTransaction transaction)
			: base(dataProvider, transaction)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /> </param>
		/// <param name="connectionString">Connection string</param>
		public IdentityDataConnection(IDataProvider dataProvider, string connectionString)
			: base(dataProvider, connectionString)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="providerName">Data provider name</param>
		/// <param name="connectionString">Connection string</param>
		public IdentityDataConnection(string providerName, string connectionString)
			: base(providerName, connectionString)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="configurationString">Connection string</param>
		public IdentityDataConnection(string configurationString)
			: base(configurationString)
		{
		}

		/// <summary>
		///     Default constructor
		/// </summary>
		public IdentityDataConnection()
		{
		}

		#endregion
	}

	/// <summary>
	///     Base class for the LinqToDB database context used for identity.
	/// </summary>
	/// <typeparam name="TUser">The type of the user objects.</typeparam>
	public class IdentityDataConnection<TUser> : IdentityDataConnection<TUser, IdentityRole, string>
		where TUser : IdentityUser
	{
		#region Constructors 

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /></param>
		/// <param name="connection">Connection object <see cref="IDbConnection" /></param>
		public IdentityDataConnection(IDataProvider dataProvider, IDbConnection connection)
			: base(dataProvider, connection)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /></param>
		/// <param name="transaction">Transdaction object <see cref="IDbTransaction" /></param>
		public IdentityDataConnection(IDataProvider dataProvider, IDbTransaction transaction)
			: base(dataProvider, transaction)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /> </param>
		/// <param name="connectionString">Connection string</param>
		public IdentityDataConnection(IDataProvider dataProvider, string connectionString)
			: base(dataProvider, connectionString)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="providerName">Data provider name</param>
		/// <param name="connectionString">Connection string</param>
		public IdentityDataConnection(string providerName, string connectionString)
			: base(providerName, connectionString)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="configurationString">Connection string</param>
		public IdentityDataConnection(string configurationString)
			: base(configurationString)
		{
		}

		/// <summary>
		///     Default constructor
		/// </summary>
		public IdentityDataConnection()
		{
		}

		#endregion
	}

	/// <summary>
	///     Base class for the LinqToDB database context used for identity.
	/// </summary>
	/// <typeparam name="TUser">The type of user objects.</typeparam>
	/// <typeparam name="TRole">The type of role objects.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for users and roles.</typeparam>
	public class IdentityDataConnection<TUser, TRole, TKey> :
		IdentityDataConnection
		<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>,
			IdentityRoleClaim<TKey>, IdentityUserToken<TKey>>
		where TUser : class, IIdentityUser<TKey>
		where TRole : class, IIdentityRole<TKey>
		where TKey : IEquatable<TKey>
	{
		#region Constructors 

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /></param>
		/// <param name="connection">Connection object <see cref="IDbConnection" /></param>
		public IdentityDataConnection(IDataProvider dataProvider, IDbConnection connection)
			: base(dataProvider, connection)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /></param>
		/// <param name="transaction">Transdaction object <see cref="IDbTransaction" /></param>
		public IdentityDataConnection(IDataProvider dataProvider, IDbTransaction transaction)
			: base(dataProvider, transaction)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /> </param>
		/// <param name="connectionString">Connection string</param>
		public IdentityDataConnection(IDataProvider dataProvider, string connectionString)
			: base(dataProvider, connectionString)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="providerName">Data provider name</param>
		/// <param name="connectionString">Connection string</param>
		public IdentityDataConnection(string providerName, string connectionString)
			: base(providerName, connectionString)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="configurationString">Connection string</param>
		public IdentityDataConnection(string configurationString)
			: base(configurationString)
		{
		}

		/// <summary>
		///     Default constructor
		/// </summary>
		public IdentityDataConnection()
		{
		}

		#endregion
	}

	/// <summary>
	///     Base class for the LinqToDB database context used for identity.
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
		DataConnection
		where TUser : class, IIdentityUser<TKey>
		where TRole : class, IIdentityRole<TKey>
		where TKey : IEquatable<TKey>
		where TUserClaim : class, IIdentityUserClaim<TKey>
		where TUserRole : class, IIdentityUserRole<TKey>
		where TUserLogin : class, IIdentityUserLogin<TKey>
		where TRoleClaim : class, IIdentityRoleClaim<TKey>
		where TUserToken : class, IIdentityUserToken<TKey>
	{
		#region Constructors 

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /></param>
		/// <param name="connection">Connection object <see cref="IDbConnection" /></param>
		public IdentityDataConnection(IDataProvider dataProvider, IDbConnection connection)
			: base(dataProvider, connection)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /></param>
		/// <param name="transaction">Transdaction object <see cref="IDbTransaction" /></param>
		public IdentityDataConnection(IDataProvider dataProvider, IDbTransaction transaction)
			: base(dataProvider, transaction)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="dataProvider">Data provider object, see <see cref="IDataProvider" /> </param>
		/// <param name="connectionString">Connection string</param>
		public IdentityDataConnection(IDataProvider dataProvider, string connectionString)
			: base(dataProvider, connectionString)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="providerName">Data provider name</param>
		/// <param name="connectionString">Connection string</param>
		public IdentityDataConnection(string providerName, string connectionString)
			: base(providerName, connectionString)
		{
		}

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="configurationString">Connection string</param>
		public IdentityDataConnection(string configurationString)
			: base(configurationString)
		{
		}

		/// <summary>
		///     Default constructor
		/// </summary>
		public IdentityDataConnection()
		{
		}

		#endregion

		#region Tables

		/// <summary>
		///     Gets the <see cref="ITable{TEntity}" /> of Users.
		/// </summary>
		public ITable<TUser> Users => GetTable<TUser>();

		/// <summary>
		///     Gets the <see cref="ITable{TEntity}" /> of User claims.
		/// </summary>
		public ITable<TUserClaim> UserClaims => GetTable<TUserClaim>();

		/// <summary>
		///     Gets the <see cref="ITable{TEntity}" /> of User logins.
		/// </summary>
		public ITable<TUserLogin> UserLogins => GetTable<TUserLogin>();

		/// <summary>
		///     Gets the <see cref="ITable{TEntity}" /> of User roles.
		/// </summary>
		public ITable<TUserRole> UserRoles => GetTable<TUserRole>();

		/// <summary>
		///     Gets the <see cref="ITable{TEntity}" /> of User tokens.
		/// </summary>
		public ITable<TUserToken> UserTokens => GetTable<TUserToken>();

		/// <summary>
		///     Gets the <see cref="ITable{TEntity}" /> of roles.
		/// </summary>
		public ITable<TRole> Roles => GetTable<TRole>();

		/// <summary>
		///     Gets the <see cref="ITable{TEntity}" /> of role claims.
		/// </summary>
		public ITable<TRoleClaim> RoleClaims => GetTable<TRoleClaim>();

		#endregion
	}
}