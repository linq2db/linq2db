using System;

namespace LinqToDB.Mapping
{
	public class MapRelationBase
	{
		public MapRelationBase(
			Type         slave,
			MapIndex     slaveIndex,
			MapIndex     masterIndex,
			string       containerName)
		{
			if (slave == null)
				throw new ArgumentNullException("slave");

			if (masterIndex.Fields.Length == 0)
				throw new MappingException("Master index length can not be 0.");

			if ( slaveIndex.Fields.Length == 0)
				throw new MappingException("Slave index length can not be 0.");

			if (masterIndex.Fields.Length != slaveIndex.Fields.Length)
				throw new MappingException("Master and slave indexes do not match.");

			if (string.IsNullOrEmpty(containerName))
				throw new MappingException("Master container field name is wrong.");
			
			_slave           = slave;
			_masterIndex     = masterIndex;
			_slaveIndex      = slaveIndex;
			_containerName   = containerName;
		}

		private readonly MapIndex _masterIndex;
		public           MapIndex  MasterIndex
		{
			get { return _masterIndex; }
		}

		private readonly MapIndex _slaveIndex;
		public           MapIndex  SlaveIndex
		{
			get { return _slaveIndex; }
		}

		private readonly string _containerName;
		public           string  ContainerName
		{
			get { return _containerName; }
		}

		private readonly Type _slave;
		public           Type  Slave
		{
			get { return _slave; }
		}
	}
}
