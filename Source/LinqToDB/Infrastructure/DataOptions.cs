using System;
using System.Collections.Generic;

namespace LinqToDB.Infrastructure
{
	using Data;

	public class DataOptions : OptionsBase<DataOptions>
	{
		public override DataOptions WithOptions(IOptionSet options)
		{
			switch (options)
			{
				case LinqOptions           lo  : _linqOptions           = lo;  break;
				case DataConnectionOptions dco : _dataConnectionOptions = dco; break;
				default                        : return base.WithOptions(options);
			}

			return this;
		}

		LinqOptions?           _linqOptions;
		DataConnectionOptions? _dataConnectionOptions;

		public LinqOptions           LinqOptions           => _linqOptions           ??= Common.Configuration.Linq.Options;
		public DataConnectionOptions DataConnectionOptions => _dataConnectionOptions ??= DataConnection.DefaultDataConnectionOptions;

		public override IEnumerable<IOptionSet> OptionSets
		{
			get
			{
				yield return LinqOptions;
				yield return DataConnectionOptions;

				foreach (var item in base.OptionSets)
					yield return item;
			}
		}

		public override TSet? Find<TSet>()
			where TSet : class
		{
			var type = typeof(TSet);

			if (type == typeof(LinqOptions)) return (TSet)(IOptionSet)LinqOptions;

			return base.Find<TSet>();
		}

		public void Apply(DataConnection dataConnection)
		{
			((IApplicable<DataConnection>)LinqOptions).          Apply(dataConnection);
			((IApplicable<DataConnection>)DataConnectionOptions).Apply(dataConnection);

			base.Apply(dataConnection);
		}
	}
}
