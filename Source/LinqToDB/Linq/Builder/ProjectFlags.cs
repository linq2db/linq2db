namespace LinqToDB.Linq.Builder
{
	[Flags]
	enum ProjectFlags
	{
		SQL             = 0x01,
		Expression      = 0x02,
		Root            = 0x04,
		/// <summary>
		/// Forces expanding associations and GroupJoin into query expression
		/// </summary>
		Expand          = 0x08,
		AggregationRoot = 0x10,		
		/// <summary>
		/// Specify that from whole context we need just key fields.
		/// </summary>
		Keys            = 0x20,
		/// <summary>
		/// Validates that expression can be converted to the SQL. Returned value cannot be used.
		/// </summary>
		Test            = 0x40,
		AssociationRoot = 0x80,	
	}
}
