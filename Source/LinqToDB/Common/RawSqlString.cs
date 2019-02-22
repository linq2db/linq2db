using System;
using JetBrains.Annotations;

namespace LinqToDB.Common
{
	/// <summary>
	///     A string representing a raw SQL query. This type enables overload resolution between
	///     the regular and interpolated <see cref="DataExtensions.FromSql{TEntity}(IDataContext,RawSqlString,object[])" />.
	/// </summary>
	public struct RawSqlString
	{
		/// <summary>
		///     Implicitly converts a <see cref="string" /> to a <see cref="RawSqlString" />
		/// </summary>
		/// <param name="s"> The string. </param>
		public static implicit operator RawSqlString([NotNull] string s) => new RawSqlString(s);

#if !NET45
		/// <summary>
		///     Implicitly converts a <see cref="FormattableString" /> to a <see cref="RawSqlString" />
		/// </summary>
		/// <param name="fs"> The string format. </param>
		public static implicit operator RawSqlString([NotNull] FormattableString fs) => default;
#endif
		/// <summary>
		///     Constructs a <see cref="RawSqlString" /> from a <see cref="string" />
		/// </summary>
		/// <param name="s"> The string. </param>
		public RawSqlString([NotNull] string s) => Format = s;

		/// <summary>
		///     The string format.
		/// </summary>
		public string Format { get; }
	}
}
