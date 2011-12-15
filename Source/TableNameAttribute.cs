using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class TableNameAttribute : Attribute
	{
		public TableNameAttribute()
		{
		}

		public TableNameAttribute(string name)
		{
			_name = name;
		}

		public TableNameAttribute(string database, string name)
		{
			_database = database;
			_name     = name;
		}

		public TableNameAttribute(string database, string owner, string name)
		{
			_database = database;
			_owner    = owner;
			_name     = name;
		}

		private string _database; public virtual string Database { get { return _database; } set { _database = value; } }
		private string _owner;    public virtual string Owner    { get { return _owner;    } set { _owner = value;    } }
		private string _name;     public virtual string Name     { get { return _name;     } set { _name = value;     } }
	}
}
