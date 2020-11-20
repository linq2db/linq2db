using NUnit.Framework.Interfaces;
using System.Collections.Generic;

namespace Tests
{
	/// <summary>
	/// Provides an API for intercepting test filtering operations.
	/// For use in running the tests with 3rd party DataProviders.
	/// </summary>
	public class CustomizationSupportInterceptor
	{
		/// <summary>
		/// Intercept and mutate the set of providers available globally by the testing framework
		/// </summary>
		/// <param name="providers">The default providers supported by the testing framework.</param>
		/// <returns>The actual providers supported at runtime.</returns>
		public virtual IEnumerable<string> GetSupportedProviders(IEnumerable<string> providers)
			=> providers;

		/// <summary>
		/// Intercept and mutate the set of supported datasources/contexts when scanning for tests.
		/// Use this method to replace aliases with actual providers.
		/// </summary>
		/// <param name="dataSourcesAttribute">The DataSourcesAttribute instance associated with the test.</param>
		/// <param name="contexts">The default datasources passed to a test by DataSourcesAttribute.</param>
		/// <returns>The actual datasources that test should run with.</returns>
		public virtual IEnumerable<string> InterceptDataSources(DataSourcesBaseAttribute dataSourcesAttribute, IEnumerable<string> contexts)
			=> contexts;

		/// <summary>
		/// Intercept and mutate the set of datasources/contexts passed to a specific test.
		/// Use this method to mutate providers or switch off specific tess.
		/// </summary>
		/// <param name="dataSourcesAttribute">The DataSourcesAttribute instance associated with the test.</param>
		/// <param name="testMethod">The reflected test method.</param>
		/// <param name="contexts">The default datasources/contexts for given tests</param>
		/// <returns>The actual datasources that test should run with.</returns>
		public virtual IEnumerable<string> InterceptTestDataSources(DataSourcesBaseAttribute dataSourcesAttribute, IMethodInfo testMethod, IEnumerable<string> contexts)
			=> contexts;

		/// <summary>
		/// Intercept calls to get parameter token character
		/// </summary>
		/// <param name="token">The original token for the provider.</param>
		/// <param name="context">The provider context.</param>
		/// <returns>The actual parameter token for this provider.</returns>
		public virtual char GetParameterToken(char token, string context)
			=> token;

		/// <summary>
		/// Helper method to extract the class name and method name of a test method.
		/// </summary>
		/// <param name="testMethod"></param>
		/// <returns></returns>
		protected static (string className, string methodName) ExtractMethod(IMethodInfo testMethod)
			=> (testMethod.TypeInfo.Name, testMethod.Name);
	}
}
