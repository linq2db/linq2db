using System;

namespace LinqToDB.Data.Linq
{
	using Data.Sql.SqlProvider;
	using Mapping;

	public interface IDataContextInfo
	{
		IDataContext  DataContext    { get; }
		string        ContextID      { get; }
		MappingSchema MappingSchema  { get; }
		bool          DisposeContext { get; }

		ISqlProvider     CreateSqlProvider();
		IDataContextInfo Clone();
	}
}
