using System;

namespace LinqToDB.Data
{
	/// <summary>
	/// Type of an operation being performed.
	/// </summary>
	public enum OperationType
	{
		OpenConnection,
		CloseConnection,
		BeginTransaction,
		CommitTransaction,
		RollbackTransaction,
		DisposeTransaction,
		DeriveParameters,
		PrepareCommand,
		ExecuteNonQuery,
		ExecuteReader,
		Fill,
		Update,
		Read
	}
}
