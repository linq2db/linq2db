using System;
using System.Collections;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	public class MapResultSet
	{
		public MapResultSet(Type objectType)
		{
			_objectType = objectType;
		}

		public MapResultSet(Type objectType, IList list)
		{
			_objectType = objectType;
			_list       = list;
		}

		public MapResultSet(Type objectType, object[] parameters)
		{
			_objectType = objectType;
			_parameters = parameters;
		}

		public MapResultSet(Type objectType, IList list, object[] parameters)
		{
			_objectType = objectType;
			_parameters = parameters;
			_list       = list;
		}

		internal MapResultSet(MapResultSet resultSet)
		{
			_objectType = resultSet._objectType;
			_parameters = resultSet._parameters;

			if (resultSet._relationList != null)
			{
				_relationList = new List<MapRelation>(resultSet._relationList.Count);
				_relationList.AddRange(resultSet._relationList);
			}
		}

		private readonly Type _objectType;
		internal         Type  ObjectType
		{
			get { return _objectType; }
		}

		private object[] _parameters;
		public  object[]  Parameters
		{
			get { return _parameters;  }
			set { _parameters = value; }
		}

		private IList _list;
		public  IList  List
		{
			get { return _list ?? (_list = new List<object>()); }
			set { _list = value; }
		}

		private  MapRelation[] _relations;
		internal MapRelation[]  Relations
		{
			get 
			{
				if (_relationList != null && (_relations == null || _relations.Length != _relationList.Count))
					_relations = _relationList.ToArray();

				return _relations;
			}

			set { _relations = value; }
		}

		private List<MapRelation> _relationList;

		public void AddRelation(
			MapResultSet slaveResultSet,
			MapIndex     slaveIndex,
			MapIndex     masterIndex,
			string       containerName)
		{
			if (_relationList == null)
				_relationList = new List<MapRelation>();

			_relationList.Add(new MapRelation(slaveResultSet, slaveIndex, masterIndex, containerName));
		}

		public void AddRelation(
			MapResultSet slaveResultSet,
			string       slaveIndex,
			string       masterIndex,
			string       containerName)
		{
			AddRelation( slaveResultSet, new MapIndex(slaveIndex), new MapIndex(masterIndex),containerName);
		}

		public void AddRelation(MapResultSet slaveResultSet, MapRelationBase relation)
		{
			AddRelation(slaveResultSet, relation.SlaveIndex, relation.MasterIndex, relation.ContainerName);
		}

		readonly Dictionary<string,IDictionary<object,IList>> _indexies = new Dictionary<string,IDictionary<object,IList>>();

		public  IDictionary<object,IList> GetIndex(MappingSchema ms, MapIndex masterIndex)
		{
			var indexId = masterIndex.ID;

			IDictionary<object,IList> indexHash;

			if (_indexies.TryGetValue(indexId, out indexHash))
				return indexHash;

			var masterMapper = ms.GetObjectMapper(ObjectType);

			indexHash = new Dictionary<object, IList>();

			foreach (var o in List)
			{
				var key = masterIndex.GetValueOrIndex(masterMapper, o);

				if (ms.IsNull(key))
					continue;

				IList matches;

				if (!indexHash.TryGetValue(key, out matches))
					indexHash.Add(key, matches = new List<object>());

				matches.Add(o);
			}

			return indexHash;
		}

		public IDictionary<object,IList> GetIndex(MappingSchema ms, MapRelation relation)
		{
			return GetIndex(ms, relation.MasterIndex);
		}
	}
}

