using System;

namespace LinqToDB.Linq
{
	using Mapping;
	using SqlProvider;

	public class DataContextInfo : IDataContextInfo
	{
		public DataContextInfo(IDataContext dataContext)
		{
			DataContext    = dataContext;
			DisposeContext = false;
		}

		public DataContextInfo(IDataContext dataContext, bool disposeContext)
		{
			DataContext    = dataContext;
			DisposeContext = disposeContext;
		}

		public IDataContext  DataContext    { get; private set; }
		public bool          DisposeContext { get; private set; }
		public string        ContextID      { get { return DataContext.ContextID;     } }
		public MappingSchemaOld MappingSchema  { get { return DataContext.MappingSchema; } }

		public ISqlProvider CreateSqlProvider()
		{
			return DataContext.CreateSqlProvider();
		}

		public IDataContextInfo Clone(bool forNestedQuery)
		{
			return new DataContextInfo(DataContext.Clone(forNestedQuery));
		}

		public static IDataContextInfo Create(IDataContext dataContext)
		{
#if SILVERLIGHT
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return new DataContextInfo(dataContext);
#else
			return dataContext == null ? (IDataContextInfo)new DefaultDataContextInfo() : new DataContextInfo(dataContext);
#endif
		}
	}
}
