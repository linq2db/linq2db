using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class MapIgnoreAttribute : Attribute
	{
		public MapIgnoreAttribute()
		{
			_ignore = true;
		}

		public MapIgnoreAttribute(bool ignore)
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
