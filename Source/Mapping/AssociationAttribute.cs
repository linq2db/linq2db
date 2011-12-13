using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
	public class AssociationAttribute : Attribute
	{
		private string _thisKey;          public string ThisKey   { get { return _thisKey;   } set { _thisKey   = value; } }
		private string _otherKey;         public string OtherKey  { get { return _otherKey;  } set { _otherKey  = value; } }
		private string _storage;          public string Storage   { get { return _storage;   } set { _storage   = value; } }
		private bool   _canBeNull = true; public bool   CanBeNull { get { return _canBeNull; } set { _canBeNull = value; } }

		public string[] GetThisKeys () { return Association.ParseKeys(_thisKey);  }
		public string[] GetOtherKeys() { return Association.ParseKeys(_otherKey); }
	}
}
