using LinqToDB.Configuration;
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

			DefaultMappings.SetupIdentityUser     <IdentityUser             >(builder);
			DefaultMappings.SetupIdentityUserClaim<IdentityUserClaim<string>>(builder);
			DefaultMappings.SetupIdentityUserLogin<IdentityUserLogin<string>>(builder);
			DefaultMappings.SetupIdentityUserToken<IdentityUserToken<string>>(builder);
			DefaultMappings.SetupIdentityRole     <IdentityRole             >(builder);
			DefaultMappings.SetupIdentityUserRole <IdentityUserRole<string >>(builder);
			DefaultMappings.SetupIdentityRoleClaim<IdentityRoleClaim<string>>(builder);
		}
	}
}
