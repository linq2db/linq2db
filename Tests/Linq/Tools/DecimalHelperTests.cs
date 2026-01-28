using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Tools
{
	[TestFixture]
	public class DecimalHelperTests
	{
		static readonly object[][] _testCases =
		[
			[ 0m, 1, 0 ],
			[ 0.0m, 1, 1 ],
			[ 1.0m, 2, 1 ],
			[ 1m, 1, 0 ],
			[ 1.1m, 2, 1 ],
			[ 0.01m, 2, 2 ],
			[ 0.010m, 3, 3 ],
			[ 0123.01230m, 8, 5 ],
			[ 12.30000m, 7, 5 ],
			[ 12345.30m, 7, 2 ],

			[ -0m, 1, 0 ],
			[ -0.0m, 1, 1 ],
			[ -1.0m, 2, 1 ],
			[ -1m, 1, 0 ],
			[ -1.1m, 2, 1 ],
			[ -0.01m, 2, 2 ],
			[ -0.010m, 3, 3 ],
			[ -0123.01230m, 8, 5 ],
			[ -12.30000m, 7, 5 ],
			[ -12345.30m, 7, 2 ],
		];

		[TestCaseSource(nameof(_testCases))]
		public void Test(decimal value, int expectedPrecision, int expectedScale)
		{
			var (precision, scale) = DecimalHelper.GetFacets(value);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(precision, Is.EqualTo(expectedPrecision));
				Assert.That(scale, Is.EqualTo(expectedScale));
			}
		}
	}
}
