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
		public bool     IncludeLinqService { get; }
		public string[] Providers          { get; }

		public static bool NoLinqService   { get; set; }

		protected DataSourcesBaseAttribute(bool includeLinqService, string[] providers)
		{
			IncludeLinqService = includeLinqService;
			Providers          = providers;
		}

		public IEnumerable GetData(IParameterInfo parameter)
		{
			var skipAttrs = new HashSet<string>(
				from a in parameter.Method.GetCustomAttributes<SkipCategoryAttribute>(true)
				where a.ProviderName != null && TestBase.SkipCategories.Contains(a.Category)
				select a.ProviderName);

			var providers = skipAttrs.Count == 0 ?
				GetProviders().ToList() :
				GetProviders().Where(a => !skipAttrs.Contains(a)).ToList();

			if (NoLinqService || !IncludeLinqService)
				return providers;

			return providers.Concat(providers.Select(p => p + ".LinqService"));
		}

		protected abstract IEnumerable<string> GetProviders();
	}
}
