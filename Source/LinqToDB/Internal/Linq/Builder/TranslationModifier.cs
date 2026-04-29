using System;
using System.Diagnostics;
using System.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	[DebuggerDisplay("InlineParameters = {InlineParameters}, IgnoreQueryFilters = {IgnoreQueryFilters}, EagerLoadingStrategy = {EagerLoadingStrategy}")]
	sealed class TranslationModifier : IEquatable<TranslationModifier>
	{
		public static readonly TranslationModifier Default = new();

		public TranslationModifier(
			bool                 inlineParameters    = false,
			Type[]?              ignoreQueryFilters  = null,
			EagerLoadingStrategy? eagerLoadingStrategy = null)
		{
			InlineParameters      = inlineParameters;
			IgnoreQueryFilters    = ignoreQueryFilters;
			EagerLoadingStrategy  = eagerLoadingStrategy;
		}

		public bool                  InlineParameters      { get; }
		public Type[]?               IgnoreQueryFilters    { get; }
		public EagerLoadingStrategy? EagerLoadingStrategy  { get; }

		public bool IsFilterDisabled(Type entityType)
		{
			if (IgnoreQueryFilters == null)
				return false;

			if (IgnoreQueryFilters.Length == 0)
				return true;

			return Array.IndexOf(IgnoreQueryFilters, entityType) >= 0;
		}

		public bool Equals(TranslationModifier? other)
		{
			if (other is null)
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return InlineParameters == other.InlineParameters &&
				EagerLoadingStrategy == other.EagerLoadingStrategy &&
				((IgnoreQueryFilters == null && other.IgnoreQueryFilters == null) ||
				(IgnoreQueryFilters != null && other.IgnoreQueryFilters != null && IgnoreQueryFilters.SequenceEqual(other.IgnoreQueryFilters)));
		}

		public TranslationModifier WithInlineParameters(bool inlineParameters)
		{
			if (InlineParameters == inlineParameters)
			{
				return this;
			}

			return new TranslationModifier(inlineParameters, IgnoreQueryFilters, EagerLoadingStrategy);
		}

		public TranslationModifier WithIgnoreQueryFilters(Type[]? ignoreQueryFilters)
		{
			if (IgnoreQueryFilters == ignoreQueryFilters || (IgnoreQueryFilters != null && ignoreQueryFilters != null && IgnoreQueryFilters.SequenceEqual(ignoreQueryFilters)))
			{
				return this;
			}

			var newFilters = (IgnoreQueryFilters, ignoreQueryFilters) switch
			{
				(null or [], _) => ignoreQueryFilters,
				(_, null or []) => IgnoreQueryFilters,
				_ => IgnoreQueryFilters.Union(ignoreQueryFilters).ToArray(),
			};

			return new TranslationModifier(InlineParameters, newFilters, EagerLoadingStrategy);
		}

		public TranslationModifier WithEagerLoadingStrategy(EagerLoadingStrategy strategy)
		{
			if (EagerLoadingStrategy == strategy)
			{
				return this;
			}

			return new TranslationModifier(InlineParameters, IgnoreQueryFilters, strategy);
		}

		public override bool Equals(object? obj)
		{
			return obj is TranslationModifier other && Equals(other);
		}

		public override int GetHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(InlineParameters);
			hashCode.Add(EagerLoadingStrategy);

			if (IgnoreQueryFilters != null)
			{
				foreach (var t in IgnoreQueryFilters)
					hashCode.Add(t);
			}

			return hashCode.ToHashCode();
		}
	}
}
