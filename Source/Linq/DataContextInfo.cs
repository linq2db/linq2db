using System;
using LinqToDB.Data;

namespace LinqToDB.Linq
{
	class DataContextInfo : IDataContextInfo
	{
		public DataContextInfo(IDataContext dataContext, bool disposeContext)
		{
			DataContext    = dataContext;
			DisposeContext = disposeContext;
		}

		public IDataContext     DataContext      { get; private set; }
		public bool             DisposeContext   { get; private set; }

		public IDataContextInfo Clone(bool forNestedQuery)
		{
			return new DataContextInfo(DataContext.Clone(forNestedQuery), true);
		}

		public static IDataContextInfo Create(IDataContext dataContext)
		{
#if SILVERLIGHT || NETFX_CORE
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return new DataContextInfo(dataContext, false);
#else
			return dataContext == null ?
				new DataContextInfo(new DataConnection(), true) :
				new DataContextInfo(dataContext, false);
#endif
		}
	}
}
