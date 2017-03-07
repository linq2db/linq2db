using System;

namespace LinqToDB.Linq
{
	using Mapping;
	using SqlProvider;

	class DataContextInfo : IDataContextInfo
	{
		//public DataContextInfo(IDataContext dataContext)
		//{
		//	DataContext    = dataContext;
		//	DisposeContext = false;
		//}

		public DataContextInfo(IDataContext dataContext, bool disposeContext)
		{
			DataContext    = dataContext;
			DisposeContext = disposeContext;
		}

		public IDataContext     DataContext      { get; private set; }
		public bool             DisposeContext   { get; private set; }
		public string           ContextID        { get { return DataContext.ContextID;        } }
		public MappingSchema    MappingSchema    { get { return DataContext.MappingSchema;    } }
		public SqlProviderFlags SqlProviderFlags { get { return DataContext.SqlProviderFlags; } }

		public ISqlBuilder CreateSqlBuilder()
		{
			return DataContext.CreateSqlProvider();
		}

		public ISqlOptimizer GetSqlOptimizer()
		{
			return DataContext.GetSqlOptimizer();
		}

		public IDataContextInfo Clone(bool forNestedQuery)
		{
			return new DataContextInfo(DataContext.Clone(forNestedQuery), true);
		}

		public static IDataContextInfo Create(IDataContext dataContext)
		{
#if SILVERLIGHT || NETFX_CORE
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return new DataContextInfo(dataContext);
#else
			return dataContext == null ? (IDataContextInfo)new DefaultDataContextInfo() : new DataContextInfo(dataContext, false);
#endif
		}
	}
}
