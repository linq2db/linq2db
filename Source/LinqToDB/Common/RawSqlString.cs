using System;

namespace LinqToDB.Common
{
	/// <summary>
	///     A string representing a raw SQL query. This type enables overload resolution between
	///     the regular and interpolated <see cref="DataExtensions.FromSql{TEntity}(IDataContext,RawSqlString,object[])" />.
	/// </summary>
	public readonly struct RawSqlString
	{
		/// <summary>
		///     Implicitly converts a <see cref="string" /> to a <see cref="RawSqlString" />
		/// </summary>
		/// <param name="s"> The string. </param>
		public static implicit operator RawSqlString(string s) => new RawSqlString(s);

		/// <summary>
		///     Implicitly converts a <see cref="FormattableString" /> to a <see cref="RawSqlString" />
		/// </summary>
		/// <param name="fs"> The string format. </param>
		public static implicit operator RawSqlString(FormattableString fs) => default;

		/// <summary>
		///     Constructs a <see cref="RawSqlString" /> from a <see cref="string" />
		/// </summary>
		/// <param name="s"> The string. </param>
		public RawSqlString(string s) => Format = s;

		/// <summary>
		///     The string format.
		/// </summary>
		public string Format { get; }
	}
}
