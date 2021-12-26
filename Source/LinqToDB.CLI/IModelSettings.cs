namespace LinqToDB.CodeGen.Configuration
{
	public interface IModelSettings
	{
		string Provider { get; }
		string ConnectionString { get; }
	}
}
