using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Text;

namespace Tests
{
	/// <summary>
	/// Marks test or fixture to be run only explicitly, because they should fail due to existing issue.
	/// Also allows to mark specific configuration for existing test as explicit.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
	public class ActiveIssueAttribute : NUnitAttribute, IApplyToTest
	{
		private readonly string _issue;

		/// <summary>
		/// Marks test or fixture to run explicit due to active issue.
		/// </summary>
		/// <param name="issue">Number of issue or pull request in LINQ To DB repository with additional details.</param>
		public ActiveIssueAttribute(int issue)
		{
			// we generate link to issues and github redirects us if it is not in issue
			_issue =  $"https://github.com/linq2db/linq2db/issues/{issue}";
		}

		/// <summary>
		/// Marks test or fixture to run explicit due to active issue.
		/// </summary>
		/// <param name="issue">Link to a page with additional issue details.</param>
		public ActiveIssueAttribute(string issue)
		{
			_issue = issue;
		}

		/// <summary>
		/// Marks test or fixture to run explicit due to active issue.
		/// </summary>
		public ActiveIssueAttribute()
		{
		}

		/// <summary>
		/// Gets or sets additional details for an issue.
		/// </summary>
		public string Details { get; set; }

		/// <summary>
		/// Gets or sets configuration name, to which this attribute should be applied.
		/// Applied only to tests marked with attributes, based on <see cref="BaseDataContextSourceAttribute"/>.
		/// </summary>
		public string Configuration { get; set; }

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

		void IApplyToTest.ApplyToTest(Test test)
		{
			var debug = new StringBuilder();
			debug.AppendLine("ActiveIssue debug details");
			debug.AppendLine($"RunState: {test.RunState}");
			debug.AppendLine($"IsSuite: {test.IsSuite}");
			debug.AppendLine($"Details: {Details}");
			debug.AppendLine($"Configuration: {Configuration}");
			debug.AppendLine($"SkipForLinqService: {SkipForLinqService}");
			debug.AppendLine($"SkipForNonLinqService: {SkipForNonLinqService}");

			if (test.RunState != RunState.NotRunnable
				&& test.RunState != RunState.Ignored)
			{
				var reason = string.IsNullOrWhiteSpace(_issue) ? "Active issue" : $"Issue {_issue}";

				if (!string.IsNullOrWhiteSpace(Details))
				{
					reason += $". {Details}";
				}

				// *DataContextSource ignores detection:
				// 1. those tests always passed as suite
				// 2. we need to do special processing only when configuration-specific filters set
				if (test.IsSuite && (Configuration != null || SkipForLinqService || SkipForNonLinqService))
				{
					// this case is handled by BaseDataContextSourceAttribute.BuildFrom
					// it will call this attribute again on test level
					throw new InvalidOperationException(debug.ToString());
				}
				else
				{
					// all other tests could be disabled here
					if (Configuration != null || SkipForLinqService || SkipForNonLinqService)
					{
						var provider = test.Properties.Get(BaseDataContextSourceAttribute.ProviderProperty) as string;
						var isLinqService = test.Properties.Get(BaseDataContextSourceAttribute.IsLinqServiceProperty) as bool?;

						// check that we are called by BaseDataContextSourceAttribute
						if (provider != null && isLinqService != null)
						{
							// first check that wcf/non-wcf flags applicable for current case
							var matched = !SkipForLinqService    && isLinqService == true
									   || !SkipForNonLinqService && isLinqService == false;

							// next check configuration name
							matched = matched && (Configuration == null || Configuration == provider);

							// attribute is not applicable to current test case
							if (!matched)
							{
								throw new InvalidOperationException(debug.ToString());
								//return;
							}
						}
					}

					// apply attribute
					test.RunState = RunState.Explicit;
					test.Properties.Add(PropertyNames.Category, "ActiveIssue");
					test.Properties.Set(PropertyNames.SkipReason, reason);
				}
			}
		}
	}
}
