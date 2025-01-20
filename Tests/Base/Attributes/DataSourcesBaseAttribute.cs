using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tests
{
	public abstract class DataSourcesBaseAttribute : NUnitAttribute, IParameterDataSource
	{
		public bool IncludeLinqService { get; }
		public string[] Providers { get; }

		public static bool NoLinqService { get; set; }

		protected DataSourcesBaseAttribute(bool includeLinqService, string[] providers)
		{
			try
			{
				IncludeLinqService = includeLinqService;
				Providers          = CustomizationSupport.Interceptor.InterceptDataSources(this, Split(providers)).ToArray();
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}
		}

		IEnumerable IParameterDataSource.GetData(IParameterInfo parameter)
		{
			try
			{
				var skipAttrs = new HashSet<string>(
					from a in parameter.Method.GetCustomAttributes<SkipCategoryAttribute>(true)
					where a.ProviderName != null && TestConfiguration.SkipCategories.Contains(a.Category)
					select a.ProviderName);

				var providers = skipAttrs.Count == 0 ?
					GetProviders().ToList() :
					GetProviders().Where(a => !skipAttrs.Contains(a)).ToList();

				if (!NoLinqService && IncludeLinqService && !TestConfiguration.DisableRemoteContext)
					providers.AddRange(providers.Select(p => p + TestBase.LinqServiceSuffix).ToList());

				return CustomizationSupport.Interceptor.InterceptTestDataSources(this, parameter.Method, providers);
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}
		}

		protected static IEnumerable<string> Split(IEnumerable<string> providers)
		{
			try
			{
				return providers.SelectMany(x => x.Split(',')).Select(x => x.Trim());
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}
		}

		protected abstract IEnumerable<string> GetProviders();
	}
}
