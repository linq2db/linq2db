using System;

namespace LinqToDB.Data
{
	using Common.Internal;
	using DataProvider;
	using Infrastructure;

	/// <param name="ConfigurationString">
	/// Gets configuration string name to use with <see cref="DataConnection"/> instance.
	/// </param>
	public sealed record DataConnectionOptions(
		string? ConfigurationString,
		string? ConnectionString,
		IDataProvider? DataProvider
	) : IOptionSet, IApplicable<DataConnection>
	{
		public DataConnectionOptions() : this(
			ConfigurationString : null,
			ConnectionString    : null,
			DataProvider        : null)
		{
		}

		int? _configurationID;
		int IOptionSet.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(ConfigurationString)
			.Add(ConnectionString)
			//.Add(DataProvider?.ID)
			.CreateID();

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
		}

		#region IEquatable implementation

		public bool Equals(DataConnectionOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IOptionSet)this).ConfigurationID == ((IOptionSet)other).ConfigurationID;
		}

		public override int GetHashCode()
		{
			return ((IOptionSet)this).ConfigurationID;
		}

		#endregion
	}
}
