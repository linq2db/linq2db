using System.Text;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.DataProvider.Sybase;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue6023Tests : TestBase
	{
		[Test]
		public void EmptyFieldName()
		{
			var flags = new SqlProviderFlags();
			var optimizer = new SybaseSqlOptimizer(flags);

			var builder = new SybaseSqlBuilder(
				null,
				new MappingSchema(),
				new DataOptions(),
				optimizer,
				flags);

			builder.Convert(new StringBuilder(), string.Empty, ConvertType.NameToQueryField)
				.ToString()
				.ShouldBe("[]");
		}
	}
}