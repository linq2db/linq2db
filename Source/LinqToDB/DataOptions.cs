using System;

namespace LinqToDB
{
	using Common;
	using Common.Internal;
	using Data;
	using Data.RetryPolicy;

	/// <summary>
	/// Immutable context configuration object.
	/// </summary>
	public sealed class DataOptions : OptionsContainer<DataOptions>, IConfigurationID, IEquatable<DataOptions>, ICloneable
	{
		public DataOptions()
		{
		}

		public DataOptions(ConnectionOptions connectionOptions)
		{
			_connectionOptions = connectionOptions;
		}

		DataOptions(DataOptions options) : base(options)
		{
			_linqOptions        = options._linqOptions;
			_retryPolicyOptions = options._retryPolicyOptions;
			_connectionOptions  = options._connectionOptions;
			_dataContextOptions = options._dataContextOptions;
			_bulkCopyOptions    = options._bulkCopyOptions;
		}

		protected override DataOptions Clone()
		{
			return new(this);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public override DataOptions WithOptions(IOptionSet options)
		{
			switch (options)
			{
				case LinqOptions        lo  : return ReferenceEquals(_linqOptions,        lo)  ? this : new(this) { _linqOptions        = lo  };
				case RetryPolicyOptions rp  : return ReferenceEquals(_retryPolicyOptions, rp)  ? this : new(this) { _retryPolicyOptions = rp  };
				case ConnectionOptions  co  : return ReferenceEquals(_connectionOptions,  co)  ? this : new(this) { _connectionOptions  = co  };
				case DataContextOptions dco : return ReferenceEquals(_dataContextOptions, dco) ? this : new(this) { _dataContextOptions = dco };
				case BulkCopyOptions    bco : return ReferenceEquals(_bulkCopyOptions,    bco) ? this : new(this) { _bulkCopyOptions    = bco };
				default                     : return base.WithOptions(options);
			}
		}

		LinqOptions?        _linqOptions;
		RetryPolicyOptions? _retryPolicyOptions;
		ConnectionOptions?  _connectionOptions;
		DataContextOptions? _dataContextOptions;
		BulkCopyOptions?    _bulkCopyOptions;

		public LinqOptions        LinqOptions        => _linqOptions        ??= Common.Configuration.Linq.Options;
		public RetryPolicyOptions RetryPolicyOptions => _retryPolicyOptions ??= Common.Configuration.RetryPolicy.Options;
		public ConnectionOptions  ConnectionOptions  => _connectionOptions  ??= DataConnection.DefaultDataOptions.ConnectionOptions;
		public DataContextOptions DataContextOptions => _dataContextOptions ??= DataContextOptions.Empty;
		public BulkCopyOptions    BulkCopyOptions    => _bulkCopyOptions    ??= BulkCopyOptions.Empty;

		public override IEnumerable<IOptionSet> OptionSets
		{
			get
			{
				yield return LinqOptions;
				yield return RetryPolicyOptions;
				yield return ConnectionOptions;

				if (_dataContextOptions != null)
					yield return _dataContextOptions;

				if (_bulkCopyOptions != null)
					yield return _bulkCopyOptions;

				foreach (var item in base.OptionSets)
					yield return item;
			}
		}

		public override TSet? Find<TSet>()
			where TSet : class
		{
			var type = typeof(TSet);

			if (type == typeof(LinqOptions))        return (TSet?)(IOptionSet?)LinqOptions;
			if (type == typeof(RetryPolicyOptions)) return (TSet?)(IOptionSet?)RetryPolicyOptions;
			if (type == typeof(ConnectionOptions))  return (TSet?)(IOptionSet?)ConnectionOptions;
			if (type == typeof(DataContextOptions)) return (TSet?)(IOptionSet?)_dataContextOptions;
			if (type == typeof(BulkCopyOptions))    return (TSet?)(IOptionSet?)_bulkCopyOptions;

			return base.Find<TSet>();
		}

		public void Apply(DataConnection dataConnection)
		{
			((IApplicable<DataConnection>)ConnectionOptions). Apply(dataConnection);
			((IApplicable<DataConnection>)RetryPolicyOptions).Apply(dataConnection);

			if (_dataContextOptions is IApplicable<DataConnection> a)
				a.Apply(dataConnection);

			base.Apply(dataConnection);
		}

		public void Apply(DataContext dataContext)
		{
			((IApplicable<DataContext>)ConnectionOptions).Apply(dataContext);

			if (_dataContextOptions is IApplicable<DataContext> a)
				a.Apply(dataContext);

			base.Apply(dataContext);
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
						.Add(LinqOptions)
						.Add(RetryPolicyOptions)
						.Add(ConnectionOptions)
						.Add(DataContextOptions)
						.Add(BulkCopyOptions)
						.AddRange(base.OptionSets)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		public bool Equals(DataOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID)other).ConfigurationID;
		}

		public override bool Equals(object? obj)
		{
			return obj is DataOptions o && Equals(o);
		}

		public override int GetHashCode()
		{
			return ((IConfigurationID)this).ConfigurationID;
		}

		public static bool operator ==(DataOptions? t1, DataOptions? t2)
		{
			if (ReferenceEquals(t1, t2))
				return true;
			if (t1 is null || t2 is null)
				return false;

			return t1.Equals(t2);
		}

		public static bool operator !=(DataOptions? t1, DataOptions? t2) => !(t1 == t2);
	}
}
