using System;

namespace LinqToDB.Linq
{
	public interface IDataContextInfo
	{
		IDataContext     DataContext    { get; }
		bool             DisposeContext { get; }

		IDataContextInfo Clone(bool forNestedQuery);
	}
}
