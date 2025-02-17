using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		readonly string? _issue;
		string[]? _configurations;

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
		public string? Details { get; set; }

		/// <summary>
		/// Gets or sets configuration names, to which this attribute should be applied.
		/// Applied only to tests marked with attributes, based on <see cref="DataSourcesBaseAttribute"/>.
		/// </summary>
		[MaybeNull]
		public string[] Configurations
		{
			get => _configurations;
			set => _configurations = value.SelectMany(p => p.Split(',').Select(_ => _.Trim())).ToArray();
		}

		/// <summary>
		/// Gets or sets comma-separated configuration names, to which this attribute should be applied.
		/// Applied only to tests marked with attributes, based on <see cref="DataSourcesBaseAttribute"/>.
		/// </summary>
		public string Configuration
		{
			get => _configurations != null ? string.Join(",", _configurations) : string.Empty;
			set => _configurations = value.Split(',').Select(_ => _.Trim()).ToArray();
		}

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

		HashSet<string>? _issueConfigurations;

		HashSet<string> GetIssueConfigurations()
		{
			return _issueConfigurations ??= [.. Configurations ?? []];
		}

		IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test? suite)
		{
			foreach (var testMethod in BuildFrom(method, suite))
			{
				((IApplyToTest)this).ApplyToTest(testMethod);
				yield return testMethod;
			}
		}

		void IApplyToTest.ApplyToTest(Test test)
		{
			if (test.RunState != RunState.Runnable)
				return;

			var issueConfigurations = GetIssueConfigurations();

			var explicitTest = issueConfigurations.Count == 0;

			if (!explicitTest)
			{
				var (provider, isLinqService) = NUnitUtils.GetContext(test);

				if (provider != null)
				{
					explicitTest = issueConfigurations.Contains(provider)
						&& ((!SkipForLinqService && isLinqService)
							|| (!SkipForNonLinqService && !isLinqService));
				}
			}

			if (!explicitTest)
				return;

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
