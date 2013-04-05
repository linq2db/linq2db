using System;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	[AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class ExpressionMethodAttribute : Attribute
	{
		public ExpressionMethodAttribute(string methodName)
		{
			MethodName = methodName;
		}

		public ExpressionMethodAttribute(string configuration, string methodName)
		{
			Configuration = configuration;
			MethodName    = methodName;
		}

		public string Configuration { get; set; }
		public string MethodName    { get; set; }
	}
}
