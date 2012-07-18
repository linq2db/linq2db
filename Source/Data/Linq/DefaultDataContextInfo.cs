using System;
using LinqToDB.DataProvider;
using LinqToDB.SqlProvider;

namespace LinqToDB.Data.Linq
{
	using Mapping;

	class DefaultDataContextInfo : IDataContextInfo
	{
		private IDataContext _dataContext;
		public  IDataContext  DataContext    { get { return _dataContext ?? (_dataContext = new DbManager()); } }

		public MappingSchemaOld  MappingSchema  { get { return Map.DefaultSchema; } }
		public bool           DisposeContext { get { return true; } }
		public string         ContextID      { get { return _dataProvider.Name; } }

		public ISqlProvider CreateSqlProvider()
		{
			return _dataProvider.CreateSqlProvider();
		}

		public IDataContextInfo Clone(bool forNestedQuery)
		{
			return new DataContextInfo(DataContext.Clone(forNestedQuery));
		}

		static readonly DataProviderBaseOld _dataProvider = DbManager.GetDataProvider(DbManager.DefaultConfiguration);
	}
}
