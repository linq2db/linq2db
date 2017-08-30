using System;

namespace LinqToDB
{
	/// <summary>
	/// When applied to method or property, tells linq2db to replace them in queryable LINQ expression with another expression,
	/// returned by method, specified in this attribute.
	/// 
	/// Requirements to expression method:
	/// <para>
	/// - expression method should be in the same class and replaced property of method;
	/// - method could be private.
	/// </para>
	/// <para>
	/// When applied to property, expression:
	/// - method should return function expression with the same return type as property type;
	/// - expression method could take up to two parameters in any order - current object parameter and database connection context object.
	/// </para>
	/// <para>
	/// When applied to method:
	/// - expression method should return function expression with the same return type as method return type;
	/// - method cannot have void return type;
	/// - parameters in expression method should go in the same order as in substituted method;
	/// - expression could take method instance object as first parameter;
	/// - expression could take database connection context object as last parameter;
	/// - last method parameters could be ommited from expression method, but only if you don't add database connection context parameter.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class ExpressionMethodAttribute : Attribute
	{
		/// <summary>
		/// Creates instance of attribute.
		/// </summary>
		/// <param name="methodName">Name of method in the same class that returns substitution expression.</param>
		public ExpressionMethodAttribute(string methodName)
		{
			MethodName = methodName;
		}

		/// <summary>
		/// Creates instance of attribute.
		/// </summary>
		/// <param name="configuration">Connection configuration, for which this attribute should be taken into account.</param>
		/// <param name="methodName">Name of method in the same class that returns substitution expression.</param>
		public ExpressionMethodAttribute(string configuration, string methodName)
		{
			Configuration = configuration;
			MethodName    = methodName;
		}

		/// <summary>
		/// Mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Name of method in the same class that returns substitution expression.
		/// </summary>
		public string MethodName    { get; set; }
	}
}
