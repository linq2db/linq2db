using System;

using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	[CLSCompliant(false)]
	public interface IMapDataDestinationList
	{
		void                InitMapping       (InitContext initContext);
		[CLSCompliant(false)]
		IMapDataDestination GetDataDestination(InitContext initContext);
		object              GetNextObject     (InitContext initContext);
		void                EndMapping        (InitContext initContext);
	}
}
