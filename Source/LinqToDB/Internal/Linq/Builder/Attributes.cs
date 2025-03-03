#pragma warning disable MA0048 // File name must match type name
using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	// These attributes are markers for our source generator. They have no state and are never used.
	//
	// Attributes taking arrays are not CLS-compliant, so instead of:
	//
	//   BuildsExpressionAttribute(params ExpressionType[] types)
	//
	// we declare overloads with various number of arguments.
	// This wouldn't matter for CLS-compliance because it's never publicly exposed 
	// but it's a "Won't fix" C# bug: https://github.com/dotnet/roslyn/issues/4293
	// Feel free to add more overloads as needed.

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	sealed class BuildsAnyAttribute : Attribute
	{ }

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	sealed class BuildsExpressionAttribute : Attribute
	{
		public string? CanBuildName { get; set; }

		public BuildsExpressionAttribute(ExpressionType type) { }
		public BuildsExpressionAttribute(ExpressionType type1, ExpressionType type2) { }
		public BuildsExpressionAttribute(ExpressionType type1, ExpressionType type2, ExpressionType type3) { }
		public BuildsExpressionAttribute(ExpressionType type1, ExpressionType type2, ExpressionType type3, ExpressionType type4) { }
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	sealed class BuildsMethodCallAttribute : Attribute
	{
		public string? CanBuildName { get; set; }

		public BuildsMethodCallAttribute(string method) { }
		public BuildsMethodCallAttribute(string method1, string method2) { }
		public BuildsMethodCallAttribute(string method1, string method2, string method3) { }
		public BuildsMethodCallAttribute(string method1, string method2, string method3, string method4) { }
		public BuildsMethodCallAttribute(string method1, string method2, string method3, string method4, string method5) { }
		public BuildsMethodCallAttribute(string method1, string method2, string method3, string method4, string method5, string method6) { }
		public BuildsMethodCallAttribute(string method1, string method2, string method3, string method4, string method5, string method6, string method7) { }
		public BuildsMethodCallAttribute(string method1, string method2, string method3, string method4, string method5, string method6, string method7, string method8) { }
	}
}
