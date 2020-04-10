using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class QueryFilterAttribute : Attribute
	{
		public Delegate? FilterFunc { get; set; }
	}
}
