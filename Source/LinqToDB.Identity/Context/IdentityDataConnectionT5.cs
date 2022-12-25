using System;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Mapping;
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
	public class IdentityDataConnection<TUser, TKey, TUserClaim, TUserLogin, TUserToken> :
		DataConnection
		where TUser      : IdentityUser<TKey>
		where TKey       : IEquatable<TKey>
		where TUserClaim : IdentityUserClaim<TKey>
		where TUserLogin : IdentityUserLogin<TKey>
		where TUserToken : IdentityUserToken<TKey>
	{
		private static readonly object _syncRoot = new ();
		private static MappingSchema?  _mappingSchema;

		/// <summary>
		/// Constructor with options.
		/// </summary>
		/// <param name="options">Connection options.</param>
		//public IdentityDataConnection(DataOptions options)
		public IdentityDataConnection(LinqToDBConnectionOptions options)
			: base(options)
		{
			// TODO: rework after options merge
			AddMappingSchema(GetMappingSchema());
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataConnection()
		{
			// TODO: rework after options merge
			AddMappingSchema(GetMappingSchema());
		}

		private MappingSchema GetMappingSchema()
		{
			if (_mappingSchema == null)
			{
				lock (_syncRoot)
				{
					if (_mappingSchema == null)
					{
						var ms = new MappingSchema();

						ConfigureMappings(ms);

						_mappingSchema = ms;
					}
				}
			}

			return _mappingSchema;
		}

		protected virtual void ConfigureMappings(MappingSchema mappingSchema)
		{
			var builder = mappingSchema.GetFluentMappingBuilder();

			DefaultMappings.SetupIdentityUser     <TKey, TUser     >(builder);
			DefaultMappings.SetupIdentityUserClaim<TKey, TUserClaim>(builder);
			DefaultMappings.SetupIdentityUserLogin<TKey, TUserLogin>(builder);
			DefaultMappings.SetupIdentityUserToken<TKey, TUserToken>(builder);
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
