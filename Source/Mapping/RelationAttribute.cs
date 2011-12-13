using System;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class RelationAttribute : Attribute
	{
		#region Constructors

		public RelationAttribute()
		{
		}

		public RelationAttribute(Type destination)
		{
			_destination = destination;
		}

		public RelationAttribute(string slaveIndex)
		{
			SlaveIndex1 = slaveIndex;
		}

		public RelationAttribute(string slaveIndex, string masterIndex)
			: this(slaveIndex)
		{
			MasterIndex1 = masterIndex;
		}

		public RelationAttribute(Type destination, string slaveIndex)
			: this(destination)
		{
			SlaveIndex1 = slaveIndex;
		}

		public RelationAttribute(Type destination, string slaveIndex, string masterIndex)
			: this(destination)
		{
			SlaveIndex1 = slaveIndex;
			MasterIndex1 = masterIndex;
		}

		#endregion

		private Type _destination;
		public  Type Destination { get { return _destination; } }

		private string _masterIndex1;
		public  string MasterIndex1 { get { return _masterIndex1; } set { _masterIndex1 = value; } }
		
		private string _masterIndex2;
		public  string MasterIndex2 { get { return _masterIndex2; } set { _masterIndex2 = value; } }
		
		private string _masterIndex3;
		public  string MasterIndex3 { get { return _masterIndex3; } set { _masterIndex3 = value; } }

		private string _slaveIndex1;
		public  string SlaveIndex1 { get { return _slaveIndex1; } set { _slaveIndex1 = value; } }
		
		private string _slaveIndex2;
		public  string SlaveIndex2 { get { return _slaveIndex2; } set { _slaveIndex2 = value; } }
		
		private string _slaveIndex3;
		public  string SlaveIndex3 { get { return _slaveIndex3; } set { _slaveIndex3 = value; } }

		public MapIndex MasterIndex
		{
			get
			{
				List<String> index = new List<string>();

				AddIndex(index, MasterIndex1);
				AddIndex(index, MasterIndex2);
				AddIndex(index, MasterIndex3);

				if (index.Count == 0)
					return null;

				return new MapIndex(index.ToArray());
			}
		}

		public MapIndex SlaveIndex
		{
			get
			{
				List<String> index = new List<string>();

				AddIndex(index, SlaveIndex1);
				AddIndex(index, SlaveIndex2);
				AddIndex(index, SlaveIndex3);

				if (index.Count == 0)
					return null;

				return new MapIndex(index.ToArray());
			}
		}

		private void AddIndex(List<string> index, string field)
		{
			if (!string.IsNullOrEmpty(field))
				index.Add(field);
		}
	}
}
