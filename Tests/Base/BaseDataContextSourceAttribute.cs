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
	public abstract class BaseDataContextSourceAttribute : NUnitAttribute, ITestBuilder, IImplyFixture
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

		protected virtual IEnumerable<Tuple<object[], string>> GetParameters(string provider)
		{
			yield return Tuple.Create(new object[] {provider}, (string)null);
		}

		public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test suite)
		{
			var explic = method.GetCustomAttributes<ExplicitAttribute>(true)
				.Cast<IApplyToTest>()
				.Union(method.GetCustomAttributes<IgnoreAttribute>(true))
				.ToList();

			var maxTime = method.GetCustomAttributes<MaxTimeAttribute>(true).FirstOrDefault();
			explic.Add(maxTime ?? new MaxTimeAttribute(10000));

//#if !NETSTANDARD1_6
//				var timeout = method.GetCustomAttributes<TimeoutAttribute>(true).FirstOrDefault();
//				explic.Add(timeout ?? new TimeoutAttribute(10000));
//#endif

			var builder = new NUnitTestCaseBuilder();

			TestMethod test = null;
			var hasTest = false;

			foreach (var provider in _providerNames)
			{
				var isIgnore = !TestBase.UserProviders.Contains(provider);

				var caseNumber = 0;
				foreach (var parameters in GetParameters(provider))
				{
					var data = new TestCaseParameters(parameters.Item1);

					test = builder.BuildTestMethod(method, suite, data);

					foreach (var attr in explic)
						attr.ApplyToTest(test);

					test.Properties.Set(PropertyNames.Order,         GetOrder(method));
					//test.Properties.Set(PropertyNames.ParallelScope, ParallelScope);
					test.Properties.Set(PropertyNames.Category,      provider);

					SetName(test, method, provider, false, caseNumber++, parameters.Item2);

					if (isIgnore)
					{
						if (test.RunState != RunState.NotRunnable && test.RunState != RunState.Explicit)
							test.RunState = RunState.Ignored;

#if !APPVEYOR && !TRAVIS
						test.Properties.Set(PropertyNames.SkipReason, "Provider is disabled. See UserDataProviders.json or DataProviders.json");
#endif
						continue;
					}

					if (test.RunState != RunState.Runnable)
						test.Properties.Set(PropertyNames.Category, "Ignored");

					hasTest = true;
					yield return test;

				}

#if !NETSTANDARD1_6 && !NETSTANDARD2_0 && !MONO
				if (!isIgnore && _includeLinqService)
				{
					var linqCaseNumber = 0;
					foreach (var parameters in GetParameters(provider + ".LinqService"))
					{

						var data = new TestCaseParameters(parameters.Item1);
						test = builder.BuildTestMethod(method, suite, data);

						foreach (var attr in explic)
							attr.ApplyToTest(test);

						test.Properties.Set(PropertyNames.Order,         GetOrder(method));
						test.Properties.Set(PropertyNames.ParallelScope, ParallelScope);
						test.Properties.Set(PropertyNames.Category,      provider);

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
