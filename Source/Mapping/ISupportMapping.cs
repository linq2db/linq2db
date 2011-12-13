using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	public interface ISupportMapping
	{
		void BeginMapping(InitContext initContext);
		void EndMapping  (InitContext initContext);
	}
}
