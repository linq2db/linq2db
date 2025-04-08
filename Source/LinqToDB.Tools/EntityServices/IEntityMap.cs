using System.Collections;

namespace LinqToDB.Tools.EntityServices
{
	interface IEntityMap
	{
		void        MapEntity(EntityCreatedEventArgs args);
		IEnumerable GetEntities();
	}
}
