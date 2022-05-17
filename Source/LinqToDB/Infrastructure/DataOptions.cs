using System;
using System.Collections.Generic;

namespace LinqToDB.Infrastructure
{
	class DataOptions : OptionsBase<DataOptions>
	{
		public override DataOptions WithOptions(IOptionSet options)
		{
			switch (options)
			{
				case LinqOptionSet lo : _linqOptions = lo; break;
				default               : return base.WithOptions(options);
			}

			return this;
		}

		LinqOptionSet? _linqOptions;

		public LinqOptionSet LinqOptions => _linqOptions ?? Common.Configuration.Linq.Options;

		public override IEnumerable<IOptionSet> OptionSets
		{
			get
			{
				yield return LinqOptions;

				foreach (var item in base.OptionSets)
					yield return item;
			}
		}

		public override TSet? Find<TSet>()
			where TSet : class
		{
			var type = typeof(TSet);

			if (type == typeof(LinqOptionSet)) return (TSet)(IOptionSet)LinqOptions;

			return base.Find<TSet>();
		}
	}
}
