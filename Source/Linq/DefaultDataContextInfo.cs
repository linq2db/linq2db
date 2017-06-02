using System;

namespace LinqToDB.Linq
{
	using Data;
	using DataProvider;
	using SqlProvider;
	using Mapping;

	class DefaultDataContextInfo : IDataContextInfo
	{
		private IDataContext    _dataContext;
		public  IDataContext     DataContext      { get { return _dataContext ?? (_dataContext = new DataConnection()); } }

		public bool              DisposeContext   { get { return true; } }

		public IDataContextInfo Clone(bool forNestedQuery)
		{
			return new DataContextInfo(DataContext.Clone(forNestedQuery), true);
		}
	}
}
