using System;

using LinqToDB.SqlProvider;

namespace LinqToDB.Data.Linq
{
	using Mapping;

	public interface IDataContextInfo
	{
		IDataContext  DataContext    { get; }
		string        ContextID      { get; }
		MappingSchemaOld MappingSchema  { get; }
		bool          DisposeContext { get; }

		ISqlProvider     CreateSqlProvider();
		IDataContextInfo Clone(bool forNestedQuery);
	}
}
