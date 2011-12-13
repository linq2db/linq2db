using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	public interface IMapDataSourceList
	{
		void InitMapping      (InitContext initContext);
		bool SetNextDataSource(InitContext initContext);
		void EndMapping       (InitContext initContext);
	}
}
