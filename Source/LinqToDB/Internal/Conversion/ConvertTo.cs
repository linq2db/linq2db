using JetBrains.Annotations;

namespace LinqToDB.Internal.Conversion
{
	/// <summary>
	/// Value converter to <typeparamref name="TTo"/> type.
	/// </summary>
	/// <typeparam name="TTo">Target conversion type.</typeparam>
	public static class ConvertTo<TTo>
	{
		/// <summary>
		/// Converts value from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source conversion type.</typeparam>
		/// <param name="o">Value to convert.</param>
		/// <returns>Converted value.</returns>
		/// <example>
		/// ConvertTo&lt;int&gt;.From("123");
		/// </example>
		public static TTo From<TFrom>(TFrom o)
		{
			return Convert<TFrom,TTo>.From(o);
		}
	}
}
