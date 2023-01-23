using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	internal static class DefaultMappings
	{
		// Guid.NewGuid().ToString() as default key/stamp generator
		private const int STRING_KEY_LENGTH = 36;
		private const int STAMP_LENGTH      = 36;
		private const int NAME_LENGTH       = 256;
		private const int EMAIL_LENGTH      = 256;
		// see https://github.com/dotnet/aspnetcore/issues/7568
		private const int KEYS_LENGTH       = 128;

		public static void SetupIdentityUserClaim<TKey, TUserClaim>(FluentMappingBuilder mappings)
			where TKey       : IEquatable<TKey>
			where TUserClaim : IdentityUserClaim<TKey>
		{
			mappings.Entity<TUserClaim>("AspNetUserClaims")
				.Property(e => e.Id)
					.IsPrimaryKey()
				.Property(e => e.UserId)
					.IsNullable(false)
				.Property(e => e.ClaimType)
				.Property(e => e.ClaimValue)
				;
		}

		public static void SetupIdentityUserClaim<TUserClaim>(FluentMappingBuilder mappings)
			where TUserClaim : IdentityUserClaim<string>
		{
			SetupIdentityUserClaim<string, TUserClaim>(mappings);

			// add length
			mappings.Entity<TUserClaim>()
				.Property(e => e.UserId)
					.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityRoleClaim<TKey, TRoleClaim>(FluentMappingBuilder mappings)
			where TKey       : IEquatable<TKey>
			where TRoleClaim : IdentityRoleClaim<TKey>
		{
			mappings.Entity<TRoleClaim>("AspNetRoleClaims")
				.Property(e => e.Id)
					.IsPrimaryKey()
				.Property(e => e.RoleId)
					.IsNullable(false)
				.Property(e => e.ClaimType)
				.Property(e => e.ClaimValue)
				;
		}

		public static void SetupIdentityRoleClaim<TRoleClaim>(FluentMappingBuilder mappings)
			where TRoleClaim : IdentityRoleClaim<string>
		{
			SetupIdentityRoleClaim<string, TRoleClaim>(mappings);

			// add length
			mappings.Entity<TRoleClaim>()
				.Property(e => e.RoleId)
					.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityUserRole<TKey, TUserRole>(FluentMappingBuilder mappings)
			where TKey      : IEquatable<TKey>
			where TUserRole : IdentityUserRole<TKey>
		{
			mappings.Entity<TUserRole>("AspNetUserRoles")
				.Property(e => e.UserId)
					.IsPrimaryKey()
					.IsNullable(false)
				.Property(e => e.RoleId)
					.IsPrimaryKey()
					.IsNullable(false)
				;
		}

		public static void SetupIdentityUserRole<TUserRole>(FluentMappingBuilder mappings)
			where TUserRole : IdentityUserRole<string>
		{
			SetupIdentityUserRole<string, TUserRole>(mappings);

			// add length
			mappings.Entity<TUserRole>()
				.Property(e => e.UserId)
					.HasLength(STRING_KEY_LENGTH)
				.Property(e => e.RoleId)
					.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityRole<TKey, TRole>(FluentMappingBuilder mappings)
			where TKey  : IEquatable<TKey>
			where TRole : IdentityRole<TKey>
		{
			mappings.Entity<TRole>("AspNetRoles")
				.Property(e => e.Id)
					.IsPrimaryKey()
					.IsNullable(false)
				.Property(e => e.Name)
					.HasLength(NAME_LENGTH)
				.Property(e => e.NormalizedName)
					.HasLength(NAME_LENGTH)
				.Property(e => e.ConcurrencyStamp)
					.HasLength(STAMP_LENGTH)
					.HasAttribute(GuidConcurrencyPropertyAttribute.Instance)
				;
		}

		public static void SetupIdentityRole<TRole>(FluentMappingBuilder mappings)
			where TRole : IdentityRole<string>
		{
			SetupIdentityRole<string, TRole>(mappings);

			// add length
			mappings.Entity<TRole>()
				.Property(e => e.Id)
					.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityUser<TKey, TUser>(FluentMappingBuilder mappings)
			where TKey  : IEquatable<TKey>
			where TUser : IdentityUser<TKey>
		{
			mappings.Entity<TUser>("AspNetUsers")
				.Property(e => e.Id)
					.IsPrimaryKey()
					.IsNullable(false)
				.Property(e => e.UserName)
					.HasLength(NAME_LENGTH)
				.Property(e => e.NormalizedUserName)
					.HasLength(NAME_LENGTH)
				.Property(e => e.Email)
					.HasLength(EMAIL_LENGTH)
				.Property(e => e.NormalizedEmail)
					.HasLength(EMAIL_LENGTH)
				.Property(e => e.EmailConfirmed)

				// length for those fields not set by ef.core implementation
				// so we use information from here (with some extra added)
				// https://github.com/dotnet/aspnetcore/issues/5823
				.Property(e => e.PasswordHash)
					.HasLength(100) // 84 min
				.Property(e => e.SecurityStamp)
					.HasLength(40) // 36 min
				.Property(e => e.PhoneNumber)
					.HasLength(20) // 15 min

				.Property(e => e.ConcurrencyStamp)
					.HasLength(STAMP_LENGTH)
					.HasAttribute(GuidConcurrencyPropertyAttribute.Instance)
				.Property(e => e.PhoneNumberConfirmed)
				.Property(e => e.TwoFactorEnabled)
				.Property(e => e.LockoutEnd)
				.Property(e => e.LockoutEnabled)
				.Property(e => e.AccessFailedCount)
				;
		}

		public static void SetupIdentityUser<TUser>(FluentMappingBuilder mappings)
			where TUser : IdentityUser<string>
		{
			SetupIdentityUser<string, TUser>(mappings);

			// add length
			mappings.Entity<TUser>()
				.Property(e => e.Id)
					.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityUserLogin<TKey, TUserLogin>(FluentMappingBuilder mappings)
			where TKey       : IEquatable<TKey>
			where TUserLogin : IdentityUserLogin<TKey>
		{
			mappings.Entity<TUserLogin>("AspNetUserLogins")
				.Property(e => e.LoginProvider)
					.IsPrimaryKey()
					.IsNullable(false)
					.HasLength(KEYS_LENGTH)
				.Property(e => e.ProviderKey)
					.IsPrimaryKey()
					.IsNullable(false)
					.HasLength(KEYS_LENGTH)
				.Property(e => e.ProviderDisplayName)
					.HasLength(128) // (un)educated guess...
				.Property(e => e.UserId)
					.IsNullable(false)
				;
		}

		public static void SetupIdentityUserLogin<TUserLogin>(FluentMappingBuilder mappings)
			where TUserLogin : IdentityUserLogin<string>
		{
			SetupIdentityUserLogin<string, TUserLogin>(mappings);

			// add length
			mappings.Entity<TUserLogin>()
				.Property(e => e.UserId)
					.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityUserToken<TKey, TUserToken>(FluentMappingBuilder mappings)
			where TKey       : IEquatable<TKey>
			where TUserToken : IdentityUserToken<TKey>
		{
			mappings.Entity<TUserToken>("AspNetUserTokens")
				.Property(e => e.UserId)
					.IsPrimaryKey()
					.IsNullable(false)
				.Property(e => e.LoginProvider)
					.IsPrimaryKey()
					.IsNullable(false)
					.HasLength(KEYS_LENGTH)
				.Property(e => e.Name)
					.IsPrimaryKey()
					.IsNullable(false)
					.HasLength(KEYS_LENGTH)
				.Property(e => e.Value)
					//.HasLength(???)
				;
		}

		public static void SetupIdentityUserToken<TUserToken>(FluentMappingBuilder mappings)
			where TUserToken : IdentityUserToken<string>
		{
			SetupIdentityUserToken<string, TUserToken>(mappings);

			// add length
			mappings.Entity<TUserToken>()
				.Property(e => e.UserId)
					.HasLength(STRING_KEY_LENGTH);
		}
	}
}
