using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Options;

namespace LinqToDB.Identity
{
	/// <summary>
	/// <see cref="DataOptions"/> settings for the linq2db Identity stores. Set them with
	/// <see cref="IdentityDataOptionsExtensions.UseIdentityDatabaseCascadeDelete(DataOptions, bool)"/>.
	/// </summary>
	/// <param name="DatabaseCascadeDelete">
	/// <see langword="true"/> when the database deletes a user's claims, logins, tokens, passkeys and role memberships,
	/// and a role's memberships and claims, through <c>ON DELETE CASCADE</c> foreign keys. The stores' <c>DeleteAsync</c>
	/// then deletes only the user or role row. Default: <see langword="false"/>, the stores delete the dependent rows,
	/// as the schema <c>CreateTable</c> emits has no foreign keys.
	/// </param>
	public sealed record LinqToDBIdentityOptions
	(
		bool DatabaseCascadeDelete = default
		// If you add another parameter here, don't forget to update IConfigurationID.ConfigurationID.
	)
		: IOptionSet
	{
		/// <summary>
		/// Creates the default options.
		/// </summary>
		public LinqToDBIdentityOptions() : this(DatabaseCascadeDelete: default)
		{
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID
		{
			get
			{
				if (_configurationID == null)
				{
					using var idBuilder = new IdentifierBuilder();
					_configurationID = idBuilder
						.Add(DatabaseCascadeDelete)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		/// <summary>
		/// Gets the default <see cref="LinqToDBIdentityOptions"/> instance.
		/// </summary>
		public static LinqToDBIdentityOptions Default { get; } = new();

		/// <inheritdoc />
		IOptionSet IOptionSet.Default => Default;

		/// <inheritdoc />
		public bool Equals([NotNullWhen(true)] LinqToDBIdentityOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return DatabaseCascadeDelete == other.DatabaseCascadeDelete;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return ((IConfigurationID)this).ConfigurationID;
		}
	}
}
