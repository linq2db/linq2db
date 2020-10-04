using System;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Represents a user in the identity system
	/// </summary>
	/// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
	public interface IIdentityUser<TKey> : IConcurrency<TKey>
		where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     Gets or sets the user name for this user.
		/// </summary>
		string UserName { get; set; }

		/// <summary>
		///     Gets or sets the normalized user name for this user.
		/// </summary>
		string NormalizedUserName { get; set; }

		/// <summary>
		///     Gets or sets a salted and hashed representation of the password for this user.
		/// </summary>
		string PasswordHash { get; set; }

		/// <summary>
		///     Gets or sets a flag indicating if a user has confirmed their email address.
		/// </summary>
		/// <value>True if the email address has been confirmed, otherwise false.</value>
		bool EmailConfirmed { get; set; }

		/// <summary>
		///     Gets or sets the email address for this user.
		/// </summary>
		string Email { get; set; }

		/// <summary>
		///     Gets or sets the normalized email address for this user.
		/// </summary>
		string NormalizedEmail { get; set; }

		/// <summary>
		///     Gets or sets the date and time, in UTC, when any user lockout ends.
		/// </summary>
		/// <remarks>
		///     A value in the past means the user is not locked out.
		/// </remarks>
		DateTimeOffset? LockoutEnd { get; set; }

		/// <summary>
		///     Gets or sets the number of failed login attempts for the current user.
		/// </summary>
		int AccessFailedCount { get; set; }

		/// <summary>
		///     Gets or sets a flag indicating if the user could be locked out.
		/// </summary>
		/// <value>True if the user could be locked out, otherwise false.</value>
		bool LockoutEnabled { get; set; }

		/// <summary>
		///     Gets or sets a telephone number for the user.
		/// </summary>
		string PhoneNumber { get; set; }

		/// <summary>
		///     Gets or sets a flag indicating if a user has confirmed their telephone address.
		/// </summary>
		/// <value>True if the telephone number has been confirmed, otherwise false.</value>
		bool PhoneNumberConfirmed { get; set; }

		/// <summary>
		///     A random value that must change whenever a users credentials change (password changed, login removed)
		/// </summary>
		string SecurityStamp { get; set; }

		/// <summary>
		///     Gets or sets a flag indicating if two factor authentication is enabled for this user.
		/// </summary>
		/// <value>True if 2fa is enabled, otherwise false.</value>
		bool TwoFactorEnabled { get; set; }
	}
}
