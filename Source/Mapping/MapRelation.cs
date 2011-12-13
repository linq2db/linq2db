using System;

namespace LinqToDB.Mapping
{
	public class MapRelation : MapRelationBase
	{
		public MapRelation(
			MapResultSet slaveResultSet,
			MapIndex     slaveIndex,
			MapIndex     masterIndex,
			string       containerName)
			: base(slaveResultSet.ObjectType, slaveIndex, masterIndex, containerName)
		{
			_slaveResultSet  = slaveResultSet;
		}

		public MapRelation(MapResultSet slaveResultSet, MapRelationBase relation)
			: this(slaveResultSet, relation.SlaveIndex, relation.MasterIndex, relation.ContainerName)
		{ }

		private readonly MapResultSet _slaveResultSet;
		public           MapResultSet  SlaveResultSet
		{
			get { return _slaveResultSet; }
		}
	}
}
