using System;

using LinqToDB.SqlProvider;

namespace LinqToDB.Data.Linq
{
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
