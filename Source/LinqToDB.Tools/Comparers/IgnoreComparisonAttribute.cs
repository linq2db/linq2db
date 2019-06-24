using System;

namespace LinqToDB.Tools.Comparers
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class IgnoreComparisonAttribute: Attribute
	{
		
	}
}
