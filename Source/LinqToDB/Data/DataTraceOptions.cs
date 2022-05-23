using System;
using System.Diagnostics;

namespace LinqToDB.Data
{
	using Common.Internal;
	using Infrastructure;

	/// <param name="OnTrace">
	/// Gets custom trace method to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="TraceLevel">
	/// Gets custom trace level to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="WriteTrace">
	/// Gets custom trace writer to use with <see cref="DataConnection"/> instance.
	/// </param>
	public sealed record DataTraceOptions
	(
		TraceLevel?                         TraceLevel,
		Action<TraceInfo>?                  OnTrace    = default,
		Action<string?,string?,TraceLevel>? WriteTrace = default
	) : IOptionSet, IApplicable<DataConnection>
	{
		public DataTraceOptions() : this((TraceLevel?)null)
		{
		}

		public DataTraceOptions(DataTraceOptions original)
		{
			TraceLevel = original.TraceLevel;
			OnTrace    = original.OnTrace;
			WriteTrace = original.WriteTrace;
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(TraceLevel)
			.Add(OnTrace)
			.Add(WriteTrace)
			.CreateID();

		public static readonly DataTraceOptions Empty = new();

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
			DataConnection.ConfigurationApplier.Apply(obj, this);
		}

		#region IEquatable implementation

		public bool Equals(DataTraceOptions? other)
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
