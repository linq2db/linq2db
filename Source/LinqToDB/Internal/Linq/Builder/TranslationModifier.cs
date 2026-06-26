using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	[DebuggerDisplay("InlineParameters = {InlineParameters}, IgnoreQueryFilterScopes = {IgnoreQueryFilterScopes}")]
	sealed class TranslationModifier : IEquatable<TranslationModifier>
	{
		public static readonly TranslationModifier Default = new();

		public TranslationModifier(
			bool    inlineParameters   = false,
			Type[]? ignoreQueryFilters = null)
			: this(inlineParameters, ignoreQueryFilters == null ? null : [new FilterIgnoreScope(null, ignoreQueryFilters)])
		{
		}

		TranslationModifier(bool inlineParameters, FilterIgnoreScope[]? scopes)
		{
			InlineParameters        = inlineParameters;
			IgnoreQueryFilterScopes = scopes;
		}

		public bool                 InlineParameters        { get; }
		public FilterIgnoreScope[]? IgnoreQueryFilterScopes { get; }

		/// <summary>
		/// Whole-entity disable check — returns <see langword="true"/> when at least one registered scope disables
		/// every filter on <paramref name="entityType"/> (i.e. the scope is unconstrained on the key dimension and
		/// matches the type). Preserves the legacy single-arg <c>IsFilterDisabled(Type)</c> shape that the DML / DDL
		/// builders rely on as a fast-out.
		/// </summary>
		public bool IsFilterDisabled(Type entityType)
		{
			var scopes = IgnoreQueryFilterScopes;
			if (scopes == null)
				return false;

			foreach (var s in scopes)
				if (s.MatchesAnyKey() && s.MatchesType(entityType))
					return true;

			return false;
		}

		/// <summary>
		/// Per-key disable check used when iterating an entity's named filters. A filter is disabled when at least
		/// one registered scope matches both the key and the entity-type dimensions.
		/// </summary>
		public bool IsFilterDisabled(Type entityType, string filterKey)
		{
			var scopes = IgnoreQueryFilterScopes;
			if (scopes == null)
				return false;

			foreach (var s in scopes)
				if (s.MatchesKey(filterKey) && s.MatchesType(entityType))
					return true;

			return false;
		}

		public bool Equals([NotNullWhen(true)] TranslationModifier? other)
		{
			if (other is null)
				return false;

			if (ReferenceEquals(this, other))
				return true;

			if (InlineParameters != other.InlineParameters)
				return false;

			if (ReferenceEquals(IgnoreQueryFilterScopes, other.IgnoreQueryFilterScopes))
				return true;

			if (IgnoreQueryFilterScopes == null || other.IgnoreQueryFilterScopes == null)
				return false;

			if (IgnoreQueryFilterScopes.Length != other.IgnoreQueryFilterScopes.Length)
				return false;

			for (var i = 0; i < IgnoreQueryFilterScopes.Length; i++)
				if (!IgnoreQueryFilterScopes[i].Equals(other.IgnoreQueryFilterScopes[i]))
					return false;

			return true;
		}

		public TranslationModifier WithInlineParameters(bool inlineParameters)
		{
			if (InlineParameters == inlineParameters)
				return this;

			return new TranslationModifier(inlineParameters, IgnoreQueryFilterScopes);
		}

		/// <summary>
		/// Back-compat helper. Pushes a scope <c>{Keys = null, Types = ignoreQueryFilters}</c> on the stack.
		/// </summary>
		public TranslationModifier WithIgnoreQueryFilters(Type[]? ignoreQueryFilters)
		{
			if (ignoreQueryFilters == null)
				return this;

			return WithIgnoreQueryFilterScope(new FilterIgnoreScope(null, ignoreQueryFilters));
		}

		public TranslationModifier WithIgnoreQueryFilterScope(FilterIgnoreScope scope)
		{
			ArgumentNullException.ThrowIfNull(scope);

			var existing = IgnoreQueryFilterScopes;
			if (existing == null)
				return new TranslationModifier(InlineParameters, [scope]);

			foreach (var s in existing)
				if (s.Equals(scope))
					return this;

			var newArr = new FilterIgnoreScope[existing.Length + 1];
			Array.Copy(existing, newArr, existing.Length);
			newArr[existing.Length] = scope;

			return new TranslationModifier(InlineParameters, newArr);
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			return obj is TranslationModifier other && Equals(other);
		}

		public override int GetHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(InlineParameters);

			if (IgnoreQueryFilterScopes != null)
			{
				foreach (var s in IgnoreQueryFilterScopes)
					hashCode.Add(s);
			}

			return hashCode.ToHashCode();
		}
	}
}
