using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using IdentityRole = LinqToDB.Identity.IdentityRole;
using IdentityUser = LinqToDB.Identity.IdentityUser;

namespace Tests.Identity
{
	public static class TestIdentityFactory
	{
		public static IServiceCollection CreateTestServices()
		{
			var services = new ServiceCollection();
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddLogging();
			services.AddIdentity<IdentityUser, IdentityRole>();
			return services;
		}

		public static RoleManager<IdentityRole> CreateRoleManager(IConnectionFactory factory)
		{
			var services = CreateTestServices();
			services.AddSingleton<IRoleStore<IdentityRole>>(new RoleStore<IdentityRole>(factory));
			return services.BuildServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
		}

		public static UserManager<IdentityUser> CreateUserManager(IConnectionFactory factory)
		{
			var services = CreateTestServices();
			services.AddSingleton<IUserStore<IdentityUser>>(new UserStore<IdentityUser>(factory));
			return services.BuildServiceProvider().GetRequiredService<UserManager<IdentityUser>>();
		}

	}
}
