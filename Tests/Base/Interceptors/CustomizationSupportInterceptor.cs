using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

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
		/// Intercepts the create data method for database intialization.
		/// </summary>
		/// <param name="context">The context/datasource for which to run create scripts.</param>
		/// <returns>A CreateDataScript instance that describes the data creation script. Otherwise null.</returns>
		public virtual CreateDataScript? InterceptCreateData(string context)
			=> null;

		/// <summary>
		/// Intercept the ResetPersonIdentity helper from TestBase.Identity.
		/// </summary>
		/// <param name="context">The context/datasource for which to reset Person identity.</param>
		/// <returns>An array of SQL scripts to reset Person identiy for given provider or null for default behaviour.</returns>
		public virtual string[]? InterceptResetPersonIdentity(string context, int lastValue)
			=> null;

		/// <summary>
		/// Intercept the InterceptResetAllTypesIdentity helper from TestBase.Identity.
		/// </summary>
		/// <param name="context">The context/datasource for which to reset AllTypes identity.</param>
		/// <returns>An array of SQL scripts to reset AllTypes identiy for given provider or null for default behaviour.</returns>
		public virtual string[]? InterceptResetAllTypesIdentity(string context, int lastValue, int keepIdentityLastValue)
			=> null;

		/// <summary>
		/// Intercept the InterceptResetTestSequence
		/// </summary>
		/// <param name="context">The context/datasource for which to reset the autonumber sequemce on AllTypes table.</param>
		/// <param name="lastValue"></param>
		/// <returns></returns>
		public virtual string[]? InterceptResetTestSequence(string context, int lastValue)
			=> null;

		/// <summary>
		/// Intercept the IsCaseSensitiveDB helper from Test.Base.
		/// </summary>
		/// <param name="context">The context/datasource for which to check.</param>
		/// <returns></returns>
		public virtual bool IsCaseSensitiveDB(string context)
			=> false;

		/// <summary>
		/// Intercept the IsCaseSensitiveComparison helper from Test.Base.
		/// </summary>
		/// <param name="context">The context/datasource for which to check.</param>
		/// <returns></returns>
		public virtual bool IsCaseSensitiveComparison(string context)
			=> false;

		/// <summary>
		/// Intercept the IsCollatedTableConfigured helper from Test.Base.
		/// </summary>
		/// <param name="context">The context/datasource for which to check.</param>
		/// <returns></returns>
		public virtual bool IsCollatedTableConfigured(string context)
			=> false;

		/// <summary>
		/// Helper method to extract the class name and method name of a test method.
		/// </summary>
		/// <param name="testMethod"></param>
		/// <returns></returns>
		protected static (string className, string methodName) ExtractMethod(IMethodInfo testMethod)
			=> (testMethod.TypeInfo.Name, testMethod.Name);
	}

	public class CreateDataScript
	{
		public string ConfigString { get; }
		public string Divider { get; }
		public string Name { get; }
		public Action<DbConnection>? Action { get; }
		public string? Database { get; }

		public CreateDataScript(string configString, string divider, string name, Action<DbConnection>? action = null, string? database = null)
		{
			ConfigString = configString;
			Divider = divider;
			Name = name;
			Action = action;
			Database = database;
		}
	}
}
