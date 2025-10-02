using System;
using System.Linq;

using LinqToDB;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class RecursiveCteContextSourceAttribute : CteContextSourceAttribute
	{
		private static readonly string[] RecursiveCteUnsupportedProviders = new[]
			{
				TestProvName.AllSapHana,
			}.SelectMany(_ => _.Split(',')).ToArray();

		public RecursiveCteContextSourceAttribute() : this(true)
		{
		}

		public RecursiveCteContextSourceAttribute(bool includeLinqService)
			: base(includeLinqService, RecursiveCteUnsupportedProviders)
		{
		}

		public RecursiveCteContextSourceAttribute(params string[] excludedProviders)
			: base(RecursiveCteUnsupportedProviders.Concat(excludedProviders.SelectMany(_ => _.Split(','))).Distinct().ToArray())
		{
		}

		public RecursiveCteContextSourceAttribute(bool includeLinqService, params string[] excludedProviders)
			: base(includeLinqService, RecursiveCteUnsupportedProviders.Concat(excludedProviders.SelectMany(_ => _.Split(','))).Distinct().ToArray())
		{
		}
	}
}
