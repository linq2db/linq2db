using System;

namespace LinqToDB.Expressions
{
	/// <summary>
	/// Used for controlling query caching of custom SQL Functions. 
	/// Parameter with this attribute will be evaluated on client side before generating SQL.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public class SqlQueryDependentAttribute : Attribute
	{
		
	}
}