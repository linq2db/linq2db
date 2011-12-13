using System;

namespace LinqToDB.Data.Linq
{
	[AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class MethodExpressionAttribute : Attribute
	{
		public MethodExpressionAttribute(string methodName)
		{
			MethodName = methodName;
		}

		public MethodExpressionAttribute(string sqlProvider, string methodName)
		{
			SqlProvider = sqlProvider;
			MethodName  = methodName;
		}

		public string SqlProvider { get; set; }
		public string MethodName  { get; set; }
	}
}
