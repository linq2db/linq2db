namespace LinqToDB.ServiceModel
{
	public interface ILinqService
	{
		LinqServiceInfo GetInfo        (string? configuration);
		int             ExecuteNonQuery(string? configuration, string queryData);
		object?         ExecuteScalar  (string? configuration, string queryData);
		string          ExecuteReader  (string? configuration, string queryData);
		int             ExecuteBatch   (string? configuration, string queryData);
	}
}
