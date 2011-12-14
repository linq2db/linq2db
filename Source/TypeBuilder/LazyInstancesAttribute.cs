using System;

namespace LinqToDB.TypeBuilder
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class LazyInstancesAttribute : Attribute
	{
		public LazyInstancesAttribute()
		{
		}

		public LazyInstancesAttribute(Type type)
		{
			_type = type;
		}

		public LazyInstancesAttribute(bool isLazy)
		{
			_isLazy = isLazy;
		}

		public LazyInstancesAttribute(Type type, bool isLazy)
		{
			_type   = type;
			_isLazy = isLazy;
		}

		private bool _isLazy = true;
		public  bool  IsLazy
		{
			get { return _isLazy;  }
			set { _isLazy = value; }
		}

		private Type _type = typeof(object);
		public  Type  Type
		{
			get { return _type; }
			set { _type = value ?? typeof(object); }
		}
	}
}
