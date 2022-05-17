using System;
using System.Collections.Generic;

namespace LinqToDB.Infrastructure
{
	abstract class OptionsBase<T> : IOptions
		where T : OptionsBase<T>
	{
		public virtual T WithOptions(IOptionSet options)
		{
			(_sets ??= new())[options.GetType()] = options;
			return (T)this;
		}

		public T WithOptions<TSet>(Func<TSet,TSet> optionSetter)
			where TSet : class, IOptionSet, new()
		{
			return WithOptions(optionSetter(Find<TSet>() ?? new TSet()));
		}

		Dictionary<Type,IOptionSet>? _sets;

		public virtual IEnumerable<IOptionSet> OptionSets
		{
			get
			{
				if (_sets != null)
					foreach (var item in _sets.Values)
						yield return item;
			}
		}

		public virtual TSet? Find<TSet>()
			where TSet : class, IOptionSet
		{
			if (_sets?.TryGetValue(typeof(TSet), out var set) == true)
				return (TSet?)set;

			return null;
		}
	}
}
