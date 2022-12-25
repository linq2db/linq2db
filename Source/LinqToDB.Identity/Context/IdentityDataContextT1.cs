using LinqToDB.Configuration;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
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
		//public IdentityDataContext(DataOptions options)
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

		protected override void ConfigureMappings(MappingSchema mappingSchema)
		{
			var builder = mappingSchema.GetFluentMappingBuilder();

			DefaultMappings.SetupIdentityUser     <TUser                    >(builder);
			DefaultMappings.SetupIdentityUserClaim<IdentityUserClaim<string>>(builder);
			DefaultMappings.SetupIdentityUserLogin<IdentityUserLogin<string>>(builder);
			DefaultMappings.SetupIdentityUserToken<IdentityUserToken<string>>(builder);
			DefaultMappings.SetupIdentityRole     <IdentityRole             >(builder);
			DefaultMappings.SetupIdentityUserRole <IdentityUserRole<string >>(builder);
			DefaultMappings.SetupIdentityRoleClaim<IdentityRoleClaim<string>>(builder);
		}
	}
}
