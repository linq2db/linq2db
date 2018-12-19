using System;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class SkipCategoryAttribute : NUnitAttribute, IApplyToTest
	{
		public SkipCategoryAttribute(string category)
		{
			Category = category;
		}

		public SkipCategoryAttribute(string category, string providerName)
		{
			Category     = category;
			ProviderName = providerName;
		}

		public string Category     { get; }
		public string ProviderName { get; }

		public void ApplyToTest(Test test)
		{
			if (test.RunState == RunState.NotRunnable || test.RunState == RunState.Explicit || ProviderName != null)
				return;

			if (TestBase.SkipCategories.Contains(Category))
			{
				test.RunState = RunState.Explicit;
				test.Properties.Set(PropertyNames.Category, Category);
				test.Properties.Set(PropertyNames.SkipReason, $"Skip category '{Category}'");
			}
		}
	}
}
