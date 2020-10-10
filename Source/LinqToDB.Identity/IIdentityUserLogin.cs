using System;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Represents a login and its associated provider for a user.
	/// </summary>
	/// <typeparam name="TKey">The type of the primary key of the user associated with this login.</typeparam>
	public interface IIdentityUserLogin<TKey> where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     Gets or sets the login provider for the login (e.g. facebook, google)
		/// </summary>
		string LoginProvider { get; set; }

		/// <summary>
		///     Gets or sets the unique provider identifier for this login.
		/// </summary>
		string ProviderKey { get; set; }

		/// <summary>
		///     Gets or sets the friendly name used in a UI for this login.
		/// </summary>
		string ProviderDisplayName { get; set; }

		/// <summary>
		///     Gets or sets the of the primary key of the user associated with this login.
		/// </summary>
		TKey UserId { get; set; }
	}
}