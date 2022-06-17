using System;
using System.Collections.Generic;

namespace LinqToDB
{
	using Common;
	using Common.Internal;
	using Data;
	using Data.RetryPolicy;

	public class DataOptions : OptionsContainer<DataOptions>, IConfigurationID
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
		}

		protected override DataOptions Clone()
		{
			return new(this);
		}

		public override DataOptions WithOptions(IOptionSet options)
		{
			switch (options)
			{
				case LinqOptions        lo  : return ReferenceEquals(_linqOptions,        lo)  ? this : new(this) { _linqOptions        = lo  };
				case RetryPolicyOptions rp  : return ReferenceEquals(_retryPolicyOptions, rp)  ? this : new(this) { _retryPolicyOptions = rp  };
				case ConnectionOptions  co  : return ReferenceEquals(_connectionOptions,  co)  ? this : new(this) { _connectionOptions  = co  };
				case DataContextOptions dco : return ReferenceEquals(_dataContextOptions, dco) ? this : new(this) { _dataContextOptions = dco };
				default                     : return base.WithOptions(options);
			}
		}

		LinqOptions?        _linqOptions;
		RetryPolicyOptions? _retryPolicyOptions;
		ConnectionOptions?  _connectionOptions;
		DataContextOptions? _dataContextOptions;

		public LinqOptions        LinqOptions        => _linqOptions        ??= Common.Configuration.Linq.Options;
		public RetryPolicyOptions RetryPolicyOptions => _retryPolicyOptions ??= Common.Configuration.RetryPolicy.Options;
		public ConnectionOptions  ConnectionOptions  => _connectionOptions  ??= DataConnection.DefaultDataOptions.ConnectionOptions;
		public DataContextOptions DataContextOptions => _dataContextOptions ??= DataContextOptions.Empty;

		public override IEnumerable<IOptionSet> OptionSets
		{
			get
			{
				yield return LinqOptions;
				yield return RetryPolicyOptions;
				yield return ConnectionOptions;

				if (_dataContextOptions != null)
					yield return _dataContextOptions;

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
		int IConfigurationID.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(LinqOptions)
			.Add(RetryPolicyOptions)
			.Add(ConnectionOptions)
			.Add(DataContextOptions)
			.AddRange(base.OptionSets)
			.CreateID();
	}
}
