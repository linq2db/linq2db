using System;
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
	/// Wraps an <see cref="Annotatable"/> so callers can reuse the generic annotation API
	/// for reading and writing provider-specific CTE metadata.
	/// </summary>
	public sealed class CteAnnotationsContainer : IExpressionCacheKey, IEquatable<CteAnnotationsContainer>
	{
		public static readonly CteAnnotationsContainer Empty = new(null, new Annotatable());

		public string?     Name        { get; }
		public Annotatable Annotations { get; }

		private readonly string _cacheKey;

		public CteAnnotationsContainer(string? name, Annotatable annotations)
		{
			ArgumentNullException.ThrowIfNull(annotations);

			Name        = name;
			Annotations = annotations;
			_cacheKey   = BuildCacheKey(name, annotations);
		}

		static string BuildCacheKey(string? name, Annotatable annotations)
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
