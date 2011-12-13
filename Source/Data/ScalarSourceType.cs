namespace LinqToDB.Data
{
	/// <summary>
	/// Defines the method how a scalar value is returned from the server.
	/// </summary>
	public enum ScalarSourceType
	{
		/// <summary>
		/// A call to <see cref="DbManager"/>.<see cref="DbManager.ExecuteReader()"/>
		/// then <see cref="System.Data.IDataReader"/>.<see cref="System.Data.IDataReader.GetValue(int)"/>.
		/// </summary>
		DataReader,

		/// <summary>
		/// A call to <see cref="DbManager"/>.<see cref="DbManager.ExecuteNonQuery()"/>.
		/// An output parameter <see cref="System.Data.IDbDataParameter.Value"/> is used.
		/// </summary>
		OutputParameter,

		/// <summary>
		/// A call to <see cref="DbManager"/>.<see cref="DbManager.ExecuteNonQuery()"/>.
		/// The return parameter <see cref="System.Data.IDbDataParameter.Value"/> is used.
		/// </summary>
		ReturnValue,

		/// <summary>
		/// Same as <see cref="DbManager"/>.<see cref="DbManager.ExecuteNonQuery()"/>.
		/// Useful for an abstract <see cref="LinqToDB.DataAccess.DataAccessor"/>.
		/// </summary>
		AffectedRows,
	}
}
