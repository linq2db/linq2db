namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Identity of one <c>InsertWithOutputQuery</c> call, carried as a constant in the query expression:
	/// references to the same call share it, separate calls do not, so each call inserts once.
	/// </summary>
	sealed class DataModificationIdentity;
}
