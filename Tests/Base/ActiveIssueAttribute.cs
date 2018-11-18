using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace Tests
{
	/// <summary>
	/// Marks test or fixture to be run only explicitly, because they should fail due to existing issue.
	/// Also allows to mark specific configuration for existing test as explicit.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class ActiveIssueAttribute : CombiningStrategyAttribute, ITestBuilder, IApplyToTest
	{
		readonly string _issue;

		/// <summary>
		/// Marks test or fixture to run explicit due to active issue.
		/// </summary>
		/// <param name="issue">Number of issue or pull request in LINQ To DB repository with additional details.</param>
		public ActiveIssueAttribute(int issue)
			: base(new CombinatorialStrategy(), new ParameterDataSourceProvider())
		{
			// we generate link to issues and github redirects us if it is not in issue
			_issue =  $"https://github.com/linq2db/linq2db/issues/{issue}";
		}

		/// <summary>
		/// Marks test or fixture to run explicit due to active issue.
		/// </summary>
		/// <param name="issue">Link to a page with additional issue details.</param>
		public ActiveIssueAttribute(string issue)
			: base(new CombinatorialStrategy(), new ParameterDataSourceProvider())
		{
			_issue = issue;
		}

		/// <summary>
		/// Marks test or fixture to run explicit due to active issue.
		/// </summary>
		public ActiveIssueAttribute()
			: base(new CombinatorialStrategy(), new ParameterDataSourceProvider())
		{
		}

		/// <summary>
		/// Gets or sets additional details for an issue.
		/// </summary>
		public string Details { get; set; }

		/// <summary>
		/// Gets or sets configuration names, to which this attribute should be applied.
		/// Applied only to tests marked with attributes, based on <see cref="DataSourcesBaseAttribute"/>.
		/// </summary>
		public string[] Configurations { get; set; }

		/// <summary>
		/// Gets or sets flag if this attribute should be skipped for LinqOverWcf test.
		/// Default value: false.
		/// </summary>
		public bool SkipForLinqService { get; set; }

		/// <summary>
		/// Gets or sets flag if this attribute should be skipped for non-LinqOverWcf test.
		/// Default value: false.
		/// </summary>
		public bool SkipForNonLinqService { get; set; }

		HashSet<string> _configurationsToSkip;

		HashSet<string> GetConfigurations()
		{
			return _configurationsToSkip ?? (_configurationsToSkip = new HashSet<string>(Configurations ?? new string[0]));
		}

		IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
		{
			foreach (var testMethod in base.BuildFrom(method, suite))
			{
				((IApplyToTest)this).ApplyToTest(testMethod);
				yield return testMethod;
			}
		}

		void IApplyToTest.ApplyToTest(Test test)
		{
			if (test.RunState != RunState.Runnable)
				return;

			var configurations = GetConfigurations();

			if (configurations.Count > 0 || SkipForLinqService || SkipForNonLinqService)
			{
				if (test.Arguments.Length == 0)
					return;

				var provider       = null as string;
				var isLinqService  = false;
				var hasLinqService = configurations.Any(c => c.EndsWith(".LinqService"));
				var parameters     = test.Method.GetParameters();

				for (var i = 0; i < parameters.Length; i++)
				{
					var attr = parameters[i].GetCustomAttributes<DataSourcesBaseAttribute>(true);

					if (attr.Length != 0)
					{
						var context = (string)test.Arguments[i];

						if (hasLinqService)
						{
							if (configurations.Contains(context))
								break;
							return;
						}

						provider = context;

						if (provider.EndsWith(".LinqService"))
						{
							provider = provider.Replace(".LinqService", "");
							isLinqService = true;
						}

						break;
					}
				}

				if (provider != null)
				{
					// first check that wcf/non-wcf flags applicable for current case
					var matched =
						!SkipForLinqService    && isLinqService == true ||
						!SkipForNonLinqService && isLinqService == false;

					// next check configuration name
					matched = matched && (configurations.Count == 0 || configurations.Contains(provider));

					// attribute is not applicable to current test case
					if (!matched)
						return;
				}
			}

			var reason = string.IsNullOrWhiteSpace(_issue) ? "Active issue" : $"Issue {_issue}";

			if (!string.IsNullOrWhiteSpace(Details))
			{
				reason += $". {Details}";
			}

			// apply attribute
			test.RunState = RunState.Explicit;
			test.Properties.Add(PropertyNames.Category, "ActiveIssue");
			test.Properties.Set(PropertyNames.SkipReason, reason);
		}
	}
}
