using System;

namespace LinqToDB.Mapping
{
	public class MapNextResult
	{
		public MapNextResult(
			Type     type,
			MapIndex slaveIndex,
			MapIndex masterIndex,
			string   containerName,
			params MapNextResult[] nextResults)
		{
			_objectType    = type;
			_slaveIndex    = slaveIndex;
			_masterIndex   = masterIndex;
			_containerName = containerName;
			_nextResults   = nextResults;
		}

		public MapNextResult(
			Type   type,
			string slaveIndex,
			string masterIndex,
			string containerName,
			params MapNextResult[] nextResults)
			: this(type, new MapIndex(slaveIndex), new MapIndex(masterIndex), containerName, nextResults)
		{
		}

		private readonly Type _objectType;
		internal         Type  ObjectType
		{
			get { return _objectType;  }
		}

		private readonly MapIndex _slaveIndex;
		internal         MapIndex  SlaveIndex
		{
			get { return _slaveIndex;  }
		}

		private readonly MapIndex _masterIndex;
		internal         MapIndex  MasterIndex
		{
			get { return _masterIndex;  }
		}

		private readonly string _containerName;
		internal string          ContainerName
		{
			get { return _containerName;  }
		}

		private readonly MapNextResult[] _nextResults;
		internal         MapNextResult[]  NextResults
		{
			get { return _nextResults; }
		}
	}
}
