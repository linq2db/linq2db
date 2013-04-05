namespace LinqToDB.Linq
{
	using Mapping;
	using SqlProvider;

	public interface IDataContextInfo
	{
		IDataContext     DataContext      { get; }
		string           ContextID        { get; }
		MappingSchema    MappingSchema    { get; }
		bool             DisposeContext   { get; }
		SqlProviderFlags SqlProviderFlags { get; }

		ISqlProvider     CreateSqlProvider();
		IDataContextInfo Clone(bool forNestedQuery);
	}
}
