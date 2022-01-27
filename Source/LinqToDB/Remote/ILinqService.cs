namespace LinqToDB.Remote
{
	public interface ILinqService
	{
		LinqServiceInfo GetInfo        (string? configuration);
		int             ExecuteNonQuery(string? configuration, string queryData);
		string?         ExecuteScalar  (string? configuration, string queryData);
		string          ExecuteReader  (string? configuration, string queryData);
		int             ExecuteBatch   (string? configuration, string queryData);
	}
}
