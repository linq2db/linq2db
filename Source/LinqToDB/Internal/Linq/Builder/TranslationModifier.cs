using System;
using System.Diagnostics;
using System.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	[DebuggerDisplay("InlineParameters = {InlineParameters}, IgnoreQueryFilters = {IgnoreQueryFilters}")]
	public class TranslationModifier : IEquatable<TranslationModifier>
	{
		public static readonly TranslationModifier Default = new();

		public TranslationModifier(
			bool    inlineParameters   = false,
			Type[]? ignoreQueryFilters = null)
		{
			InlineParameters   = inlineParameters;
			IgnoreQueryFilters = ignoreQueryFilters;
		}

		public bool    InlineParameters   { get; }
		public Type[]? IgnoreQueryFilters { get; }

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
			       ((IgnoreQueryFilters == null && other.IgnoreQueryFilters == null) ||
			        (IgnoreQueryFilters != null && other.IgnoreQueryFilters != null && IgnoreQueryFilters.SequenceEqual(other.IgnoreQueryFilters)));
		}

		public TranslationModifier WithInlineParameters(bool inlineParameters)
		{
			if (InlineParameters == inlineParameters)
			{
				return this;
			}

			return new TranslationModifier(inlineParameters, IgnoreQueryFilters);
		}

		public TranslationModifier WithIgnoreQueryFilters(Type[]? ignoreQueryFilters)
		{
			if (IgnoreQueryFilters == ignoreQueryFilters || (IgnoreQueryFilters != null && ignoreQueryFilters != null && IgnoreQueryFilters.SequenceEqual(ignoreQueryFilters)))
			{
				return this;
			}

			return new TranslationModifier(InlineParameters, ignoreQueryFilters);
		}

		public override bool Equals(object? obj)
		{
			return obj is TranslationModifier other && Equals(other);
		}

		public override int GetHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(InlineParameters);

			if (IgnoreQueryFilters != null)
			{
				foreach (var t in IgnoreQueryFilters)
					hashCode.Add(t);
			}

			return hashCode.ToHashCode();
		}
	}
}
