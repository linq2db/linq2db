using System;
using System.Collections.Generic;

namespace LinqToDB.Infrastructure
{
	public abstract class OptionsBase<T> : IOptions
		where T : OptionsBase<T>
	{
		protected OptionsBase()
		{
		}

		protected OptionsBase(OptionsBase<T> options)
		{
			if (options._sets != null)
				_sets = new(options._sets);
		}

		protected abstract T Clone();

		public virtual T WithOptions(IOptionSet options)
		{
			var o = Clone();

			if (o._sets == null)
				o._sets = new() { { options.GetType(), options } };
			else
				o._sets[options.GetType()] = options;

			return o;
		}

		public T WithOptions<TSet>(Func<TSet,TSet> optionSetter)
			where TSet : class, IOptionSet, new()
		{
			var original = Get<TSet>();
			var options  = optionSetter(original);

			return ReferenceEquals(original, options) ? (T)this : WithOptions(options);
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

		public virtual TSet Get<TSet>()
			where TSet : class, IOptionSet, new()
		{
			if (Find<TSet>() is {} set)
				return set;

			(_sets ??= new())[typeof(TSet)] = set = new();

			return set;
		}

		protected void Apply<TA>(TA obj)
		{
			if (_sets != null)
				foreach (var item in _sets.Values)
					if (item is IApplicable<TA> a)
						a.Apply(obj);
		}
	}
}
