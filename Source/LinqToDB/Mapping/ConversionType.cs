namespace LinqToDB.Mapping
{
	/// <summary>
	/// Defines conversion type such as to database / from database conversion direction.
	/// </summary>
	public enum ConversionType
	{
		/// <summary>
		/// Conversion is used for all directions.
		/// </summary>
		Common,
		/// <summary>
		/// Conversion is used to convert values from object to database.
		/// </summary>
		ToDatabase,
		/// <summary>
		/// Conversion is used to convert values from database to object.
		/// </summary>
		FromDatabase,
	}
}
