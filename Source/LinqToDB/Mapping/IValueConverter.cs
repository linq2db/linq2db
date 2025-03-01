using System.Linq.Expressions;

namespace LinqToDB.Mapping
{
	/// <summary>
	///     Defines conversions from an object of one type in a model to an object of the same or
	///     different type in the database.
	/// </summary>
	public interface IValueConverter
	{
		/// <summary>
		///     Identifies that convert expressions can handle null values.
		/// </summary>
		bool HandlesNulls                       { get; }

		/// <summary>
		///     Gets the expression to convert objects when reading data from the database.
		/// </summary>
		LambdaExpression FromProviderExpression { get; }

		/// <summary>
		///     Gets the expression to convert objects when writing data to the database.
		/// </summary>
		LambdaExpression ToProviderExpression   { get; }
	}
}
