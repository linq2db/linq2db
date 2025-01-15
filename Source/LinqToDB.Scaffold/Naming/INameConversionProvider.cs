using System;

namespace LinqToDB.Naming
{
	/// <summary>
	/// Name conversion provider.
	/// </summary>
	public interface INameConversionProvider
	{
		/// <summary>
		/// Returns name converter for specific conversion type.
		/// </summary>
		/// <param name="conversion">Conversion type.</param>
		/// <returns>Name converter.</returns>
		Func<string, string> GetConverter(Pluralization conversion);
	}
}
