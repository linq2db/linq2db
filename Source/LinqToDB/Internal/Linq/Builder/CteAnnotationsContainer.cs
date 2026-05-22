using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Immutable carrier for the state produced by the <see cref="ICteBuilder"/> callback.
	/// Placed into the query expression tree as a single constant so linq2db's query-cache
	/// equality comparer can discriminate distinct <c>AsCte(b =&gt; ...)</c> configurations:
	/// reference-equal delegate values would otherwise collapse to the same cache entry.
	/// The constructor snapshots the caller's annotation bag into an internally-owned store,
	/// so post-call mutations of the source <see cref="Annotatable"/> cannot affect translation
	/// or cache keying.
	/// </summary>
	public sealed class CteAnnotationsContainer : IExpressionCacheKey, IEquatable<CteAnnotationsContainer>
	{
		readonly Annotatable _annotations;
		readonly string      _cacheKey;

		public string?              Name        { get; }
		public IReadOnlyAnnotatable Annotations => _annotations;

		public CteAnnotationsContainer(string? name, IEnumerable<IAnnotation>? annotations)
		{
			Name         = name;
			_annotations = new Annotatable();

			if (annotations != null)
			{
				foreach (var ann in annotations)
				{
					_annotations.SetAnnotation(ann.Name, ann.Value);
				}
			}

			_cacheKey = BuildCacheKey(name, _annotations);
		}

		static string BuildCacheKey(string? name, IReadOnlyAnnotatable annotations)
		{
			var sb = new StringBuilder();
			sb.Append('[').Append(name ?? string.Empty).Append(']');

			// GetAnnotations returns items ordered by ordinal name — stable hashing.
			foreach (var ann in annotations.GetAnnotations())
			{
				sb.Append('|').Append(ann.Name).Append('=').Append(CultureInfo.InvariantCulture, $"{ann.Value}");
			}

			return sb.ToString();
		}

		public bool Equals(CteAnnotationsContainer? other)
			=> other != null && string.Equals(_cacheKey, other._cacheKey, StringComparison.Ordinal);

		public override bool Equals(object? obj) => obj is CteAnnotationsContainer c && Equals(c);

		public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_cacheKey);
	}
}
