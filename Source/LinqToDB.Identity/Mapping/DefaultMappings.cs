using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NET10_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
#endif
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

		// EF Core maps integral keys (int/long) as store-generated identity columns; string/Guid keys are client-assigned.
		private static bool IsAutoIncrementKey<TKey>() => typeof(TKey) == typeof(int) || typeof(TKey) == typeof(long);

		// The string-key width has to be pinned here rather than only in the string-specific overloads below: a
		// consumer registering IdentityDataConnection<..., string, ...> never reaches those, and an unpinned key
		// widens to the provider default - which diverges from the EF Core schema everywhere, and overruns
		// Firebird 2.5's index limit on the AspNetUserRoles composite key.
		private static bool IsStringKey<TKey>() => typeof(TKey) == typeof(string);

		// LockoutEnd is DateTimeOffset?, which these providers cannot store as one: most cannot render the type in
		// DDL at all - CreateTable emits the literal token "DateTimeOffset" - and Firebird, which does render it
		// from 4.0 on, cannot write the value (https://github.com/linq2db/linq2db/issues/5915). DB2 z/OS renders
		// it, hence the LUW-only pin. Each value is that provider's widest datetime type.
		private static readonly (string Configuration, DataType DataType)[] _lockoutEndDataTypes =
		[
			(ProviderName.Access,   DataType.DateTime ),
			(ProviderName.SqlCe,    DataType.DateTime2),
			(ProviderName.Sybase,   DataType.DateTime2),
			(ProviderName.SapHana,  DataType.DateTime2),
			(ProviderName.Informix, DataType.DateTime2),
			(ProviderName.DB2LUW,   DataType.DateTime2),
			(ProviderName.Firebird, DataType.DateTime2),
		];

		// The DataType pin above only fixes the DDL; the value needs converting too. A column that cannot hold an
		// offset otherwise round-trips the value through the machine's *local* offset (Firebird returns
		// 13:52+02:00 for a 13:52Z write), and Sybase and SAP HANA reject the DateTimeOffset parameter outright
		// ("Specified cast is not valid. From: DateTimeOffset to: DateTime"). Store UTC and read back at zero
		// offset - the only answer that holds outside a UTC machine.
		private static ValueConverterAttribute LockoutEndAsUtc(string configuration) => new ()
		{
			Configuration  = configuration,
			ValueConverter = new ValueConverter<DateTimeOffset?, DateTime?>(
				v => v == null ? null : v.Value.UtcDateTime,
				v => v == null ? null : new DateTimeOffset(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)),
				handlesNulls: true),
		};

		// Access caps a text column at 255 characters, so the 256-char name/email columns fail CREATE TABLE
		// outright ("Size of field 'UserName' is too long"). DbType rather than a scoped ColumnAttribute for
		// the same reason as the LockoutEnd pins - the latter would replace the unscoped one wholesale.
		private static DataTypeAttribute AccessTextLimit() => new (DataType.NVarChar, "NVarChar(255)") { Configuration = ProviderName.Access };

		public static void SetupIdentityUserClaim<TKey, TUserClaim>(FluentMappingBuilder mappings)
			where TKey       : IEquatable<TKey>
			where TUserClaim : IdentityUserClaim<TKey>
		{
			mappings.Entity<TUserClaim>().HasTableName("AspNetUserClaims")
				.Property(e => e.Id)
					.IsPrimaryKey()
					.IsIdentity()
				.Property(e => e.UserId)
					.IsNullable(false)
				.Property(e => e.ClaimType)
				.Property(e => e.ClaimValue)
				;

			// add length
			if (IsStringKey<TKey>())
				mappings.Entity<TUserClaim>()
					.Property(e => e.UserId)
						.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityUserClaim<TUserClaim>(FluentMappingBuilder mappings)
			where TUserClaim : IdentityUserClaim<string>
			=> SetupIdentityUserClaim<string, TUserClaim>(mappings);

		public static void SetupIdentityRoleClaim<TKey, TRoleClaim>(FluentMappingBuilder mappings)
			where TKey       : IEquatable<TKey>
			where TRoleClaim : IdentityRoleClaim<TKey>
		{
			mappings.Entity<TRoleClaim>().HasTableName("AspNetRoleClaims")
				.Property(e => e.Id)
					.IsPrimaryKey()
					.IsIdentity()
				.Property(e => e.RoleId)
					.IsNullable(false)
				.Property(e => e.ClaimType)
				.Property(e => e.ClaimValue)
				;

			// add length
			if (IsStringKey<TKey>())
				mappings.Entity<TRoleClaim>()
					.Property(e => e.RoleId)
						.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityRoleClaim<TRoleClaim>(FluentMappingBuilder mappings)
			where TRoleClaim : IdentityRoleClaim<string>
			=> SetupIdentityRoleClaim<string, TRoleClaim>(mappings);

		public static void SetupIdentityUserRole<TKey, TUserRole>(FluentMappingBuilder mappings)
			where TKey      : IEquatable<TKey>
			where TUserRole : IdentityUserRole<TKey>
		{
			mappings.Entity<TUserRole>().HasTableName("AspNetUserRoles")
				.Property(e => e.UserId)
					.IsPrimaryKey()
					.IsNullable(false)
				.Property(e => e.RoleId)
					.IsPrimaryKey()
					.IsNullable(false)
				;

			// add length
			if (IsStringKey<TKey>())
				mappings.Entity<TUserRole>()
					.Property(e => e.UserId)
						.HasLength(STRING_KEY_LENGTH)
					.Property(e => e.RoleId)
						.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityUserRole<TUserRole>(FluentMappingBuilder mappings)
			where TUserRole : IdentityUserRole<string>
			=> SetupIdentityUserRole<string, TUserRole>(mappings);

		public static void SetupIdentityRole<TKey, TRole>(FluentMappingBuilder mappings)
			where TKey  : IEquatable<TKey>
			where TRole : IdentityRole<TKey>
		{
			var id = mappings.Entity<TRole>().HasTableName("AspNetRoles")
				.Property(e => e.Id)
					.IsPrimaryKey()
					.IsNullable(false);

			if (IsAutoIncrementKey<TKey>())
				id = id.IsIdentity();

			id
				.Property(e => e.Name)
					.HasLength(NAME_LENGTH)
					.HasAttribute(AccessTextLimit())
				.Property(e => e.NormalizedName)
					.HasLength(NAME_LENGTH)
					.HasAttribute(AccessTextLimit())
				.Property(e => e.ConcurrencyStamp)
					.HasLength(STAMP_LENGTH)
					.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Guid))
				;

			// add length
			if (IsStringKey<TKey>())
				mappings.Entity<TRole>()
					.Property(e => e.Id)
						.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityRole<TRole>(FluentMappingBuilder mappings)
			where TRole : IdentityRole<string>
			=> SetupIdentityRole<string, TRole>(mappings);

		public static void SetupIdentityUser<TKey, TUser>(FluentMappingBuilder mappings)
			where TKey  : IEquatable<TKey>
			where TUser : IdentityUser<TKey>
		{
			var id = mappings.Entity<TUser>().HasTableName("AspNetUsers")
				.Property(e => e.Id)
					.IsPrimaryKey()
					.IsNullable(false);

			if (IsAutoIncrementKey<TKey>())
				id = id.IsIdentity();

			id
				.Property(e => e.UserName)
					.HasLength(NAME_LENGTH)
					.HasAttribute(AccessTextLimit())
				.Property(e => e.NormalizedUserName)
					.HasLength(NAME_LENGTH)
					.HasAttribute(AccessTextLimit())
				.Property(e => e.Email)
					.HasLength(EMAIL_LENGTH)
					.HasAttribute(AccessTextLimit())
				.Property(e => e.NormalizedEmail)
					.HasLength(EMAIL_LENGTH)
					.HasAttribute(AccessTextLimit())
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
					.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Guid))
				.Property(e => e.PhoneNumberConfirmed)
				.Property(e => e.TwoFactorEnabled)
				.Property(e => e.LockoutEnd)
				.Property(e => e.LockoutEnabled)
				.Property(e => e.AccessFailedCount)
				;

			// add length
			if (IsStringKey<TKey>())
				mappings.Entity<TUser>()
					.Property(e => e.Id)
						.HasLength(STRING_KEY_LENGTH);

			// DataTypeAttribute rather than a configuration-scoped ColumnAttribute: the latter replaces the unscoped one wholesale.
			foreach (var (configuration, dataType) in _lockoutEndDataTypes)
				mappings.Entity<TUser>()
					.Property(e => e.LockoutEnd)
						.HasAttribute(new DataTypeAttribute(dataType) { Configuration = configuration })
						.HasAttribute(LockoutEndAsUtc(configuration));
		}

		public static void SetupIdentityUser<TUser>(FluentMappingBuilder mappings)
			where TUser : IdentityUser<string>
			=> SetupIdentityUser<string, TUser>(mappings);

		public static void SetupIdentityUserLogin<TKey, TUserLogin>(FluentMappingBuilder mappings)
			where TKey       : IEquatable<TKey>
			where TUserLogin : IdentityUserLogin<TKey>
		{
			mappings.Entity<TUserLogin>().HasTableName("AspNetUserLogins")
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

			// add length
			if (IsStringKey<TKey>())
				mappings.Entity<TUserLogin>()
					.Property(e => e.UserId)
						.HasLength(STRING_KEY_LENGTH);
		}

		public static void SetupIdentityUserLogin<TUserLogin>(FluentMappingBuilder mappings)
			where TUserLogin : IdentityUserLogin<string>
			=> SetupIdentityUserLogin<string, TUserLogin>(mappings);

		public static void SetupIdentityUserToken<TKey, TUserToken>(FluentMappingBuilder mappings)
			where TKey       : IEquatable<TKey>
			where TUserToken : IdentityUserToken<TKey>
		{
			var userIdLength = IsStringKey<TKey>() ? STRING_KEY_LENGTH : (int?)null;

			var userId = mappings.Entity<TUserToken>().HasTableName("AspNetUserTokens")
				.Property(e => e.UserId)
					.IsPrimaryKey()
					.IsNullable(false);

			if (userIdLength != null)
				userId = userId.HasLength(userIdLength.Value);

			userId
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

			// Firebird 2.5's index key is narrower than this three-column PK (36 + 128 + 128 characters, rendered
			// CHARACTER SET UNICODE_FSS at 3 bytes each), so CREATE TABLE fails outright with "cannot create index
			// PK_AspNetUserTokens". A table without the constraint beats a table that cannot be created: a
			// configuration-scoped ColumnAttribute replaces the unscoped one wholesale, which is what drops the key.
			// Consequence on that one dialect: no uniqueness and no index on (UserId, LoginProvider, Name) - the
			// engine cannot index them at any width that fits. SetTokenAsync's find-then-insert still serialises
			// normal use, and every token read is a predicate query rather than a key lookup.
			var noKey = mappings.Entity<TUserToken>();

			// IsPrimaryKey = false rather than merely omitted: fluent .IsPrimaryKey() registers a separate
			// PrimaryKeyAttribute, which ColumnDescriptor consults whenever the column attribute left the flag
			// unset - so the scoped attribute has to answer the question, not decline it.
			var userIdColumn = new ColumnAttribute { Configuration = ProviderName.Firebird25, CanBeNull = false, IsPrimaryKey = false };

			if (userIdLength != null)
				userIdColumn.Length = userIdLength.Value;

			noKey.Property(e => e.UserId)       .HasAttribute(userIdColumn)
				 .Property(e => e.LoginProvider).HasAttribute(new ColumnAttribute { Configuration = ProviderName.Firebird25, CanBeNull = false, Length = KEYS_LENGTH, IsPrimaryKey = false })
				 .Property(e => e.Name)         .HasAttribute(new ColumnAttribute { Configuration = ProviderName.Firebird25, CanBeNull = false, Length = KEYS_LENGTH, IsPrimaryKey = false })
				 ;
		}

		public static void SetupIdentityUserToken<TUserToken>(FluentMappingBuilder mappings)
			where TUserToken : IdentityUserToken<string>
			=> SetupIdentityUserToken<string, TUserToken>(mappings);

#if NET10_0_OR_GREATER
		// WebAuthn credential ids are variable-length (spec: no longer than 1023 bytes). 1024 matches EF Core's
		// own AspNetUserPasskeys mapping (b.Property(p => p.CredentialId).HasMaxLength(1024)).
		// Note: as a SQL Server clustered PK this exceeds the 900-byte index-key limit, so SQL Server warns at
		// CREATE TABLE and rejects only an inserted key > 900 bytes - identical to EF Core, and real credential
		// ids are well under 900 bytes.
		private const int CREDENTIAL_ID_LENGTH = 1024;

		// Access needs the type changed too: VarBinary caps at 255 bytes there, LongBinary is the unbounded one.
		// Informix (BYTE) and Firebird 2.5 (VARCHAR(1024) CHARACTER SET OCTETS) both hold the value fine - for
		// them only the index was ever the problem. Firebird 3+ indexes a 1024-byte key, so it keeps its PK.
		private static readonly (string Configuration, string? DbType)[] _passkeyIdWithoutPrimaryKey =
		[
			(ProviderName.Access,     "LongBinary"),
			(ProviderName.Informix,   null),
			(ProviderName.Firebird25, null),
		];

		public static void SetupIdentityUserPasskey<TKey>(FluentMappingBuilder mappings)
			where TKey : IEquatable<TKey>
		{
			// IdentityPasskeyData is stored as a single JSON column, matching the EF Core provider's
			// complex-property mapping so the AspNetUserPasskeys schema stays interchangeable.
			mappings.Entity<IdentityUserPasskey<TKey>>().HasTableName("AspNetUserPasskeys")
				.Property(e => e.CredentialId)
					.IsPrimaryKey()
					.IsNullable(false)
					.HasLength(CREDENTIAL_ID_LENGTH)
				.Property(e => e.UserId)
					.IsNullable(false)
				.Property(e => e.Data)
					.HasConversionFunc(SerializePasskeyData, DeserializePasskeyData)
					// The converter targets string; the column's DB type must be stated explicitly for DDL/schema.
					// Unbounded NVarChar -> nvarchar(max) on SQL Server, matching EF Core's ComplexProperty(Data).ToJson().
					// Providers with a bounded NVARCHAR (e.g. Oracle NVARCHAR2, 4000 bytes) need a CLOB/NText override
					// for large passkey JSON - a provider-specific follow-up (passkeys are .NET 10 only).
					.HasDataType(DataType.NVarChar)
				;

			// add length
			if (IsStringKey<TKey>())
				mappings.Entity<IdentityUserPasskey<TKey>>()
					.Property(e => e.UserId)
						.HasLength(STRING_KEY_LENGTH);

			// Access caps a binary column at 255 bytes, so it rejects the column itself ("Size of field
			// 'CredentialId' is too long"); Informix accepts BYTE but cannot index a blob ("-103 illegal key
			// descriptor"). Neither can carry a 1024-byte key, so on those two the table is emitted without the
			// constraint - which beats not being able to create it at all. Uniqueness then rests on
			// AddOrUpdatePasskeyAsync's find-then-insert rather than on the database.
			foreach (var (configuration, dbType) in _passkeyIdWithoutPrimaryKey)
				mappings.Entity<IdentityUserPasskey<TKey>>()
					.Property(e => e.CredentialId)
						.HasAttribute(new ColumnAttribute { Configuration = configuration, CanBeNull = false, DbType = dbType, Length = CREDENTIAL_ID_LENGTH, IsPrimaryKey = false });
		}

		public static void SetupIdentityUserPasskey(FluentMappingBuilder mappings)
			=> SetupIdentityUserPasskey<string>(mappings);

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Passkey data is a flat DTO serialized with reflection-based JSON; passkey storage is not supported in trimmed/AOT apps.")]
		[UnconditionalSuppressMessage("AOT",      "IL3050", Justification = "See IL2026.")]
		private static string SerializePasskeyData(IdentityPasskeyData data)
			=> JsonSerializer.Serialize(data, (JsonSerializerOptions?)null);

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Passkey data is a flat DTO serialized with reflection-based JSON; passkey storage is not supported in trimmed/AOT apps.")]
		[UnconditionalSuppressMessage("AOT",      "IL3050", Justification = "See IL2026.")]
		private static IdentityPasskeyData DeserializePasskeyData(string json)
			=> JsonSerializer.Deserialize<IdentityPasskeyData>(json, (JsonSerializerOptions?)null)!;
#endif
	}
}
