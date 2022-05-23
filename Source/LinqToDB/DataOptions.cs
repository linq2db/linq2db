using System;
using System.Collections.Generic;

namespace LinqToDB
{
	using Data;
	using Infrastructure;

	public class DataOptions : OptionsBase<DataOptions>
	{
		public DataOptions()
		{
		}

		public DataOptions(ConnectionOptions connectionOptions)
		{
			_connectionOptions = connectionOptions;
		}

		public override DataOptions WithOptions(IOptionSet options)
		{
			switch (options)
			{
				case LinqOptions        lo  : _linqOptions        = lo;  break;
				case ConnectionOptions  co  : _connectionOptions  = co;  break;
				case DataContextOptions dco : _dataContextOptions = dco; break;
				default                     :  return base.WithOptions(options);
			}

			return this;
		}

		LinqOptions?        _linqOptions;
		ConnectionOptions?  _connectionOptions;
		DataContextOptions? _dataContextOptions;

		public LinqOptions        LinqOptions        => _linqOptions        ??= Common.Configuration.Linq.Options;
		public ConnectionOptions  ConnectionOptions  => _connectionOptions  ??= DataConnection.DefaultConnectionOptions;
		public DataContextOptions DataContextOptions => _dataContextOptions ??= DataContextOptions.Empty;

		public override IEnumerable<IOptionSet> OptionSets
		{
			get
			{
				yield return LinqOptions;
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
			if (type == typeof(ConnectionOptions))  return (TSet?)(IOptionSet?)ConnectionOptions;
			if (type == typeof(DataContextOptions)) return (TSet?)(IOptionSet?)_dataContextOptions;

			return base.Find<TSet>();
		}

		public void Apply(DataConnection dataConnection)
		{
			((IApplicable<DataConnection>)ConnectionOptions).Apply(dataConnection);

			if (_dataContextOptions is IApplicable<DataConnection> a)
				a.Apply(dataConnection);

			base.Apply(dataConnection);
		}
	}
}
