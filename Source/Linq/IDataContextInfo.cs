using System;

namespace LinqToDB.Linq
{
	using Mapping;
	using SqlProvider;

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
