using System;

namespace LinqToDB.DataAccess
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SqlIgnoreAttribute : Attribute
	{
		public SqlIgnoreAttribute()
		{
			_ignore = true;
		}

		public SqlIgnoreAttribute(bool ignore)
		{
			_ignore = ignore;
		}

		private bool _ignore;
		public  bool  Ignore
		{
			get { return _ignore;  }
			set { _ignore = value; }
		}
	}
}
