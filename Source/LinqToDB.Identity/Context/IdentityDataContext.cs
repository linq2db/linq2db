using System;
using System.Threading;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Base class for the LinqToDB database context used for identity.
	/// </summary>
	public class IdentityDataContext : IdentityDataContext<IdentityUser, IdentityRole, string>
	{
		/// <summary>
		/// Constructor with options.
		/// </summary>
		/// <param name="options">Connection options.</param>
		public IdentityDataContext(DataOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataContext()
		{
		}

		protected override void ConfigureMappings(MappingSchema mappingSchema)
		{
			var builder = new FluentMappingBuilder(mappingSchema);

			DefaultMappings.SetupIdentityUser     <IdentityUser             >(builder);
			DefaultMappings.SetupIdentityUserClaim<IdentityUserClaim<string>>(builder);
			DefaultMappings.SetupIdentityUserLogin<IdentityUserLogin<string>>(builder);
			DefaultMappings.SetupIdentityUserToken<IdentityUserToken<string>>(builder);
#if NET10_0_OR_GREATER
			DefaultMappings.SetupIdentityUserPasskey(builder);
#endif
			DefaultMappings.SetupIdentityRole     <IdentityRole             >(builder);
			DefaultMappings.SetupIdentityUserRole <IdentityUserRole<string >>(builder);
			DefaultMappings.SetupIdentityRoleClaim<IdentityRoleClaim<string>>(builder);

			builder.Build();
		}
	}

	/// <summary>
	/// Base class for the LinqToDB database context used for identity.
	/// </summary>
	/// <typeparam name="TUser">The type of the user objects.</typeparam>
	public class IdentityDataContext<TUser> : IdentityDataContext<TUser, IdentityRole, string>
		where TUser : IdentityUser
	{
		/// <summary>
		/// Constructor with options.
		/// </summary>
		/// <param name="options">Connection options.</param>
		public IdentityDataContext(DataOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataContext()
		{
		}

		protected override void ConfigureMappings(MappingSchema mappingSchema)
		{
			var builder = new FluentMappingBuilder(mappingSchema);

			DefaultMappings.SetupIdentityUser     <TUser                    >(builder);
			DefaultMappings.SetupIdentityUserClaim<IdentityUserClaim<string>>(builder);
			DefaultMappings.SetupIdentityUserLogin<IdentityUserLogin<string>>(builder);
			DefaultMappings.SetupIdentityUserToken<IdentityUserToken<string>>(builder);
#if NET10_0_OR_GREATER
			DefaultMappings.SetupIdentityUserPasskey(builder);
#endif
			DefaultMappings.SetupIdentityRole     <IdentityRole             >(builder);
			DefaultMappings.SetupIdentityUserRole <IdentityUserRole<string >>(builder);
			DefaultMappings.SetupIdentityRoleClaim<IdentityRoleClaim<string>>(builder);

			builder.Build();
		}
	}

	/// <summary>
	/// Base class for the LinqToDB database context used for identity.
	/// </summary>
	/// <typeparam name="TUser">The type of user objects.</typeparam>
	/// <typeparam name="TRole">The type of role objects.</typeparam>
	/// <typeparam name="TKey">The type of the primary key for users and roles.</typeparam>
	public class IdentityDataContext<TUser, TRole, TKey> :
		IdentityDataContext<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>>
		where TUser : IdentityUser<TKey>
		where TRole : IdentityRole<TKey>
		where TKey  : IEquatable<TKey>
	{
		/// <summary>
		/// Constructor with options.
		/// </summary>
		/// <param name="options">Connection options.</param>
		public IdentityDataContext(DataOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataContext()
		{
		}
	}

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
		private static readonly Lock _syncRoot = new ();
		private static MappingSchema?  _mappingSchema;

		/// <summary>
		/// Constructor with options.
		/// </summary>
		/// <param name="options">Connection options.</param>
		public IdentityDataContext(DataOptions options)
			: base(options)
		{
			AddMappingSchema(GetMappingSchema());
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataContext()
		{
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
			var builder = new FluentMappingBuilder(mappingSchema);

			DefaultMappings.SetupIdentityUser     <TKey, TUser     >(builder);
			DefaultMappings.SetupIdentityUserClaim<TKey, TUserClaim>(builder);
			DefaultMappings.SetupIdentityUserLogin<TKey, TUserLogin>(builder);
			DefaultMappings.SetupIdentityUserToken<TKey, TUserToken>(builder);
#if NET10_0_OR_GREATER
			DefaultMappings.SetupIdentityUserPasskey<TKey>(builder);
#endif

			builder.Build();
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
	public class IdentityDataContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> :
		IdentityDataContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
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
		public IdentityDataContext(DataOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public IdentityDataContext()
		{
		}

		protected override void ConfigureMappings(MappingSchema mappingSchema)
		{
			base.ConfigureMappings(mappingSchema);

			var builder = new FluentMappingBuilder(mappingSchema);

			DefaultMappings.SetupIdentityRole     <TKey, TRole     >(builder);
			DefaultMappings.SetupIdentityUserRole <TKey, TUserRole >(builder);
			DefaultMappings.SetupIdentityRoleClaim<TKey, TRoleClaim>(builder);

			builder.Build();
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
