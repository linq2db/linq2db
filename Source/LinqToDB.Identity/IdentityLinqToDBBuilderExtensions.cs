using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using LinqToDB;
using LinqToDB.Identity;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Contains extension methods to <see cref="IdentityBuilder" /> for adding LinqToDB stores.
	/// </summary>
	public static class IdentityLinqToDBBuilderExtensions
	{
		/// <summary>
		/// Adds an LinqToDB implementation of identity information stores.
		/// </summary>
		/// <typeparam name="TContext">The LinqToDB database context to use.</typeparam>
		/// <param name="builder">The <see cref="IdentityBuilder"/> instance this method extends.</param>
		/// <returns>The <see cref="IdentityBuilder"/> instance this method extends.</returns>
		public static IdentityBuilder AddLinqToDBStores<TContext>(this IdentityBuilder builder)
			where TContext : IDataContext
		{
			AddStores(builder.Services, builder.UserType, builder.RoleType, typeof(TContext));
			return builder;
		}

		private static void AddStores(IServiceCollection services, Type userType, Type? roleType, Type contextType)
		{
			var identityUserType = FindGenericBaseType(userType, typeof(IdentityUser<>))
				?? throw new InvalidOperationException(Resources.NotIdentityUser);

			var keyType = identityUserType.GenericTypeArguments[0];

			if (roleType != null)
			{
				var identityRoleType = FindGenericBaseType(roleType, typeof(IdentityRole<>))
					?? throw new InvalidOperationException(Resources.NotIdentityRole);

				Type userStoreType;
				Type roleStoreType;

				var identityContext = FindGenericBaseType(contextType, typeof(IdentityDataContext<,,,,,,,>))
					?? FindGenericBaseType(contextType, typeof(IdentityDataConnection<,,,,,,,>));

				if (identityContext == null)
				{
					userStoreType = typeof(UserStore<,,,>).MakeGenericType(userType, roleType, contextType, keyType);
					roleStoreType = typeof(RoleStore<,,>).MakeGenericType(roleType, contextType, keyType);
				}
				else
				{
					userStoreType = typeof(UserStore<,,,,,,,,>).MakeGenericType(userType, roleType, contextType,
						identityContext.GenericTypeArguments[2], // TKey
						identityContext.GenericTypeArguments[3], // TUserClaim
						identityContext.GenericTypeArguments[4], // TUserRole
						identityContext.GenericTypeArguments[5], // TUserLogin
						identityContext.GenericTypeArguments[7], // TUserToken
						identityContext.GenericTypeArguments[6]); // TRoleClaim
					roleStoreType = typeof(RoleStore<,,,,>).MakeGenericType(roleType, contextType,
						identityContext.GenericTypeArguments[2], // TKey
						identityContext.GenericTypeArguments[4], // TUserRole
						identityContext.GenericTypeArguments[6]); // TRoleClaim
				}

				services.TryAddScoped(typeof(IUserStore<>).MakeGenericType(userType), userStoreType);
				services.TryAddScoped(typeof(IRoleStore<>).MakeGenericType(roleType), roleStoreType);
			}
			else
			{
				Type userStoreType;

				var identityContext = FindGenericBaseType(contextType, typeof(IdentityDataContext<,,,,>))
					?? FindGenericBaseType(contextType, typeof(IdentityDataConnection<,,,,>));

				if (identityContext == null)
				{
					userStoreType = typeof(UserOnlyStore<,,>).MakeGenericType(userType, contextType, keyType);
				}
				else
				{
					userStoreType = typeof(UserOnlyStore<,,,,,>).MakeGenericType(userType, contextType,
						identityContext.GenericTypeArguments[1], // TKey
						identityContext.GenericTypeArguments[2], // TUserClaim
						identityContext.GenericTypeArguments[3], // TUserLogin
						identityContext.GenericTypeArguments[4]); // TUserToken
				}
				services.TryAddScoped(typeof(IUserStore<>).MakeGenericType(userType), userStoreType);
			}
		}

		private static Type? FindGenericBaseType(Type currentType, Type genericBaseType)
		{
			Type? type = currentType;
			while (type != null)
			{
				var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
				if (genericType != null && genericType == genericBaseType)
					return type;

				type = type.BaseType;
			}

			return null;
		}
	}
}
