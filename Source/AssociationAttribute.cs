using System;

namespace LinqToDB
{
	using Mapping;

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
	public class AssociationAttribute : Attribute
	{
		public AssociationAttribute()
		{
			CanBeNull = true;
		}

		public string ThisKey   { get; set; }
		public string OtherKey  { get; set; }
		public string Storage   { get; set; }
		public bool   CanBeNull { get; set; }

		public string[] GetThisKeys () { return Association.ParseKeys(ThisKey);  }
		public string[] GetOtherKeys() { return Association.ParseKeys(OtherKey); }
	}
}
