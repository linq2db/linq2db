namespace LinqToDB.Tools.DataProvider.SqlServer.Schemas
{
	public interface ISystemSchemaData : IDataContext
	{
		SystemSchemaModel System { get; }
	}
}
