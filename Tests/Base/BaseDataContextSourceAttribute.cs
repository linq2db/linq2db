using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Method)]
	[Obsolete]
	public abstract class BaseDataContextSourceAttribute : NUnitAttribute, ITestBuilder, IApplyToTest, IImplyFixture
	{
		protected BaseDataContextSourceAttribute(bool includeLinqService, string[] providers)
		{
			_includeLinqService = includeLinqService;
			_providerNames      = providers;
		}

		public ParallelScope ParallelScope { get; set; } = ParallelScope.None;// ParallelScope.Children;
		public int           Order         { get; set; }

		readonly bool     _includeLinqService;
		readonly string[] _providerNames;

		public static bool NoLinqService;

		internal const string ProviderProperty      = "linq2db.provider";
		internal const string ConfigurationProperty = "linq2db.configuration";
		internal const string IsLinqServiceProperty = "linq2db.is.wcf";

		static void SetName(TestMethod test, IMethodInfo method, string provider, bool isLinqService, int caseNumber, string baseName)
		{
			var name = (baseName ?? method.Name) + "." + provider;

			if (isLinqService)
				name += ".LinqService";

			// numerate cases starting from second case to preserve naming for most of tests
			if (caseNumber > 0)
			{
				if (baseName == null)
					name += "." + caseNumber;

				test.FullName += "." + caseNumber;
			}

			test.Name = method.TypeInfo.FullName.Replace("Tests.", "") + "." + name;
		}

		int GetOrder(IMethodInfo method)
		{
			if (Order == 0)
			{
				if (method.Name.StartsWith("Tests._Create")) return 100;
				if (method.Name.StartsWith("Tests.xUpdate")) return 2000;
				return 1000;
			}

			return Order;
		}

		protected virtual IEnumerable<Tuple<object[],string>> GetParameters(string provider)
		{
			yield return Tuple.Create(new object[] {provider}, (string)null);
		}

		public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test suite)
		{
			// pick attributes that we want to apply to each configuration-specific test case
			var testAttributes = method.GetCustomAttributes<ExplicitAttribute>(true)
				.Cast<IApplyToTest>()
				.Union(method.TypeInfo.GetCustomAttributes<ExplicitAttribute>(true))
#if !NETSTANDARD1_6
				.Union(method.TypeInfo.Assembly.GetCustomAttributes(true).OfType<ExplicitAttribute>())
#endif
				.Union(method.GetCustomAttributes<IgnoreAttribute>(true))
				.Union(method.TypeInfo.GetCustomAttributes<IgnoreAttribute>(true))
#if !NETSTANDARD1_6
				.Union(method.TypeInfo.Assembly.GetCustomAttributes(true).OfType<IgnoreAttribute>())
#endif
				.Union(method.GetCustomAttributes<ActiveIssueAttribute>(true))
				.Union(method.TypeInfo.GetCustomAttributes<ActiveIssueAttribute>(true))
				.ToList();

			var maxTime = method.GetCustomAttributes<MaxTimeAttribute>(true).FirstOrDefault();
			testAttributes.Add(maxTime ?? new MaxTimeAttribute(10000));

//#if !NETSTANDARD1_6
//				var timeout = method.GetCustomAttributes<TimeoutAttribute>(true).FirstOrDefault();
//				explic.Add(timeout ?? new TimeoutAttribute(10000));
//#endif

			var skipAttrs = method.GetCustomAttributes<SkipCategoryAttribute>(true)
				.Where(a => a.ProviderName != null)
				.ToDictionary(a => a.ProviderName, a => a.Category);

			var builder = new NUnitTestCaseBuilder();

			TestMethod test = null;
			var hasTest = false;

			foreach (var provider in _providerNames)
			{
				var isIgnore   = !TestBase.UserProviders.Contains(provider);
				var caseNumber = 0;

				if (!isIgnore)
					if (skipAttrs.TryGetValue(provider, out var category))
						isIgnore = TestBase.SkipCategories.Contains(category);

				foreach (var parameters in GetParameters(provider))
				{
					var data = new TestCaseParameters(parameters.Item1);

					test = builder.BuildTestMethod(method, suite, data);

					test.Properties.Set(ProviderProperty,      provider);
					test.Properties.Set(ConfigurationProperty, provider);
					test.Properties.Set(IsLinqServiceProperty, false);

					foreach (var attr in testAttributes)
						attr.ApplyToTest(test);

					test.Properties.Set(PropertyNames.Order,         GetOrder(method));
					//test.Properties.Set(PropertyNames.ParallelScope, ParallelScope);

					test.Properties.Add(PropertyNames.Category,      provider);

					SetName(test, method, provider, false, caseNumber++, parameters.Item2);

					if (isIgnore)
					{
						// if (test.RunState != RunState.NotRunnable && test.RunState != RunState.Explicit)
							test.RunState = RunState.Ignored;

#if !APPVEYOR && !TRAVIS
						if (!test.Properties.ContainsKey(PropertyNames.SkipReason))
							test.Properties.Set(PropertyNames.SkipReason, "Provider is disabled. See UserDataProviders.json or DataProviders.json");
#endif
						continue;
					}

					if (test.RunState != RunState.Runnable)
						test.Properties.Add(PropertyNames.Category, "Ignored");

					hasTest = true;
					yield return test;

				}

#if !NETSTANDARD1_6 && !NETSTANDARD2_0 && !MONO
				if (!isIgnore && _includeLinqService && !NoLinqService)
				{
					var linqCaseNumber = 0;
					var providerBase = provider;
					foreach (var parameters in GetParameters(provider + ".LinqService"))
					{

						var data = new TestCaseParameters(parameters.Item1);
						test = builder.BuildTestMethod(method, suite, data);

						test.Properties.Set(ProviderProperty,      providerBase);
						test.Properties.Set(ConfigurationProperty, provider);
						test.Properties.Set(IsLinqServiceProperty, true);

						foreach (var attr in testAttributes)
							attr.ApplyToTest(test);

						test.Properties.Set(PropertyNames.Order,         GetOrder(method));
						test.Properties.Set(PropertyNames.ParallelScope, ParallelScope);
						test.Properties.Add(PropertyNames.Category,      provider);

						SetName(test, method, provider, true, linqCaseNumber++, parameters.Item2);

						yield return test;
					}
				}
#endif
			}

			if (!hasTest)
			{
				yield return test;
			}
		}
	}
}
