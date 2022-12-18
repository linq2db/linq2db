using System;
using LinqToDB.Configuration;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Base class for the LinqToDB database context used for identity.
	/// </summary>
	/// <typeparam name="TUser">The type of user objects.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for users and roles.</typeparam>
	/// <typeparam name="TUserClaim">The type of the user claim object.</typeparam>
	/// <typeparam name="TUserLogin">The type of the user login object.</typeparam>
	/// <typeparam name="TUserToken">The type of the user token object.</typeparam>
	public class IdentityDataContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken> :
		DataContext
		where TUser      : IdentityUser<TKey>
		where TKey       : IEquatable<TKey>
		where TUserClaim : IdentityUserClaim<TKey>
		where TUserLogin : IdentityUserLogin<TKey>
		where TUserToken : IdentityUserToken<TKey>
	{
		/// <summary>
		/// Constructor with options.
		/// </summary>
		/// <param name="options">Connection options.</param>
		public IdentityDataContext(LinqToDBConnectionOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataContext()
		{
		}

		/// <summary>
		/// Gets the <see cref="ITable{TEntity}" /> of Users.
		/// </summary>
		public ITable<TUser> Users => this.GetTable<TUser>();

		/// <summary>
		/// Gets the <see cref="ITable{TEntity}" /> of User claims.
		/// </summary>
		public ITable<TUserClaim> UserClaims => this.GetTable<TUserClaim>();

		/// <summary>
		/// Gets the <see cref="ITable{TEntity}" /> of User logins.
		/// </summary>
		public ITable<TUserLogin> UserLogins => this.GetTable<TUserLogin>();

		/// <summary>
		/// Gets the <see cref="ITable{TEntity}" /> of User tokens.
		/// </summary>
		public ITable<TUserToken> UserTokens => this.GetTable<TUserToken>();
	}
}
