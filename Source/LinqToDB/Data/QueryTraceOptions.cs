using System;
using System.Diagnostics;

using LinqToDB.Common;
using LinqToDB.Common.Internal;

namespace LinqToDB.Data
{
	/// <param name="TraceLevel">
	/// Gets custom trace level to use with <see cref="DataConnection"/> instance. Value is ignored when <paramref name="TraceSwitch"/> specified.
	/// </param>
	/// <param name="OnTrace">
	/// Gets custom trace method to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="WriteTrace">
	/// Gets custom trace writer to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="TraceSwitch">
	/// Gets custom trace switcher to use with <see cref="DataConnection"/> instance.
	/// </param>
	public sealed record QueryTraceOptions
	(
		TraceLevel?                       TraceLevel  = default,
		Action<TraceInfo>?                OnTrace     = default,
		Action<string,string,TraceLevel>? WriteTrace  = default,
		TraceSwitch?                      TraceSwitch = default
		// If you add another parameter here, don't forget to update
		// QueryTraceOptions copy constructor and IConfigurationID.ConfigurationID.
	)
		: IOptionSet, IApplicable<DataConnection>
	{
		public QueryTraceOptions() : this((TraceLevel?)null)
		{
		}

		QueryTraceOptions(QueryTraceOptions original)
		{
			TraceLevel  = original.TraceLevel;
			OnTrace     = original.OnTrace;
			WriteTrace  = original.WriteTrace;
			TraceSwitch = original.TraceSwitch;
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
						.Add(TraceLevel)
						.Add(OnTrace)
						.Add(WriteTrace)
						.Add(TraceSwitch?.DisplayName)
						.Add(TraceSwitch?.Description)
						.Add(TraceSwitch?.DefaultValue)
						.Add(TraceSwitch?.Level)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		public static readonly QueryTraceOptions Empty = new();

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
			DataConnection.ConfigurationApplier.Apply(obj, this);
		}

		#region IEquatable implementation

		public bool Equals(QueryTraceOptions? other)
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
