using System;

namespace LinqToDB
{
	[AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class MethodExpressionAttribute : Attribute
	{
		public MethodExpressionAttribute(string methodName)
		{
			MethodName = methodName;
		}

		public MethodExpressionAttribute(string configuration, string methodName)
		{
			Configuration = configuration;
			MethodName    = methodName;
		}

		public string Configuration { get; set; }
		public string MethodName    { get; set; }
	}
}
