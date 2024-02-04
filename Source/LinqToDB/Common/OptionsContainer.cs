using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	/// <summary>
	/// Base class for options.
	/// </summary>
	/// <typeparam name="T">Derived type.</typeparam>
	public abstract class OptionsContainer<T> : IOptionsContainer
		where T : OptionsContainer<T>
	{
		protected OptionsContainer()
		{
		}

		protected OptionsContainer(OptionsContainer<T> options)
		{
			if (options._sets != null)
				_sets = new(options._sets);
		}

		protected abstract T Clone();

		/// <summary>
		/// Adds or replace <see cref="IOptionSet"/> instance based on concrete implementation type.
		/// </summary>
		/// <param name="options">Set of options.</param>
		/// <returns>New options object with <paramref name="options"/> applied.</returns>
		[Pure]
		public virtual T WithOptions(IOptionSet options)
		{
			var o = Clone();

			if (o._sets == null)
				o._sets = new() { { options.GetType(), options } };
			else
				o._sets[options.GetType()] = options;

			return o;
		}

		/// <summary>
		/// Adds or replace <see cref="IOptionSet"/> instance, returned by <paramref name="optionSetter"/> delegate.
		/// </summary>
		/// <typeparam name="TSet"><see cref="IOptionSet"/> concrete type.</typeparam>
		/// <param name="optionSetter">New option set creation delegate. Takes current options set as parameter.</param>
		/// <returns>New options object (if <paramref name="optionSetter"/> created new options set).</returns>
		[Pure]
		public T WithOptions<TSet>(Func<TSet,TSet> optionSetter)
			where TSet : class, IOptionSet, new()
		{
			var original = Get<TSet>();
			var options  = optionSetter(original);

			return ReferenceEquals(original, options) ? (T)this : WithOptions(options);
		}

		volatile Dictionary<Type,IOptionSet>? _sets;

		/// <summary>
		/// Provides access to option sets, stored in current options object.
		/// </summary>
		public virtual IEnumerable<IOptionSet> OptionSets
		{
			[Pure]
			get
			{
				if (_sets != null)
					foreach (var item in _sets.Values)
						yield return item;
			}
		}

		/// <summary>
		/// Search for options set by set type <typeparamref name="TSet"/>.
		/// </summary>
		/// <typeparam name="TSet">Options set type.</typeparam>
		/// <returns>Options set or <c>null</c> if set with type <typeparamref name="TSet"/> not found in options.</returns>
		[Pure]
		public virtual TSet? Find<TSet>()
			where TSet : class, IOptionSet
		{
			if (_sets?.TryGetValue(typeof(TSet), out var set) == true)
				return (TSet?)set;

			return null;
		}

		[Pure]
		public TSet FindOrDefault<TSet>(TSet defaultOptions)
			where TSet : class, IOptionSet
		{
			return Find<TSet>() ?? defaultOptions;
		}

		readonly object _sync = new ();

		/// <summary>
		/// Returns options set by set type <typeparamref name="TSet"/>. If options doesn't contain specific options set, it is created and added to options.
		/// </summary>
		/// <typeparam name="TSet">Options set type.</typeparam>
		/// <returns>
		/// Returns options set by set type <typeparamref name="TSet"/>. If options doesn't contain specific options set, it is created and added to options.
		/// </returns>
		[Pure]
		public virtual TSet Get<TSet>()
			where TSet : class, IOptionSet, new()
		{
			if (Find<TSet>() is { } set)
				return set;

			lock (_sync)
				_sets = new (_sets ?? []) { [typeof(TSet)] = set = new() };

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
