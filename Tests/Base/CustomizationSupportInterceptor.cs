using NUnit.Framework.Interfaces;
using System.Collections.Generic;

namespace Tests
{
	/// <summary>
	/// Providers an API for intercepting test filtering operations.
	/// For use in running the tests with 3rd party DataProviders.
	/// </summary>
	public class CustomizationSupportInterceptor
	{
		/// <summary>
		/// Intercept and mutate the set of unsupported providers for tests annotated with InsertOrUpdateDataSourcesAttribute
		/// (tests that include InsertOrUpdate/InserOrReplace calls)
		/// </summary>
		/// <param name="providers">The default unsupported providers from InsertOrUpdateDataSourcesAttribute</param>
		/// <returns></returns>
		public virtual IEnumerable<string> GetSupportedProviders(IEnumerable<string> providers)
			=> providers;

		/// <summary>
		/// Intercept and mutate the set of unsupported providers for tests annotated with MergeDataSourcesAttribute
		/// (tests that include Merge API calls)
		/// </summary>
		/// <param name="providers">The default unsupported providers from MergeDataSourcesAttribute</param>
		/// <returns></returns>
		public virtual IEnumerable<string> InterceptDataSources(DataSourcesBaseAttribute dataSourcesAttribute, IEnumerable<string> contexts)
			=> contexts;

		/// <summary>
		/// Intercept and mutate the set of unsupported providers for tests annotated with IdentityInsertMergeDataSourcesAttribute
		/// (tests that include Merge API with Identity Insert calls)
		/// </summary>
		/// <param name="providers">The default unsupported providers from IdentityInsertMergeDataSourcesAttribute</param>
		/// <returns></returns>
		public virtual IEnumerable<string> InterceptTestDataSources(DataSourcesBaseAttribute dataSourcesAttribute, IMethodInfo testMethod, IEnumerable<string> contexts)
			=> contexts;
	}
}
