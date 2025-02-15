using System.Data.Linq;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1303Tests : TestBase
	{
		[Table]
		public class Issue1303
		{
			[PrimaryKey]
			public int    ID     { get; set; }
			[Column(Length = 10)]
			public byte[]? Array  { get; set; }
			[Column(Length = 10)]
			public Binary? Binary { get; set; }
		}

		[Test]
		public void TestBinary([DataSources] string context, [Values] bool inlineParameters)
		{
			using (var db  = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Issue1303>())
			{
				// Informix: apply inlining to insert to test binary parameters
				if (context.IsAnyOf(TestProvName.AllInformix))
					db.InlineParameters = inlineParameters;

				tbl.Insert(() => new Issue1303()
				{
					ID     = 1,
					Array  = new byte[] { 1, 2,3 },
					Binary = new Binary(new byte[] { 4, 5})
				});

				db.InlineParameters = inlineParameters;

				var byId     = tbl.Where(_ => _.ID == 1).Single();

				Assert.Multiple(() =>
				{
					Assert.That(byId.ID, Is.EqualTo(1));
					Assert.That(new byte[] { 1, 2, 3 }.SequenceEqual(byId.Array!), Is.True);
					Assert.That(new byte[] { 4, 5 }.SequenceEqual(byId.Binary!.ToArray()), Is.True);
				});

				// Informix: doesn't support blobs in conditions
				if (!context.IsAnyOf(TestProvName.AllInformix))
				{
					var byArray  = tbl.Where(_ => _.Array  == new byte[] { 1, 2, 3 })         .Single();
					var byBinary = tbl.Where(_ => _.Binary == new Binary(new byte[] { 4, 5 })).Single();

					Assert.Multiple(() =>
					{
						Assert.That(byArray.ID, Is.EqualTo(1));
						Assert.That(new byte[] { 1, 2, 3 }.SequenceEqual(byArray.Array!), Is.True);
						Assert.That(new byte[] { 4, 5 }.SequenceEqual(byArray.Binary!.ToArray()), Is.True);

						Assert.That(byBinary.ID, Is.EqualTo(1));
						Assert.That(new byte[] { 1, 2, 3 }.SequenceEqual(byBinary.Array!), Is.True);
						Assert.That(new byte[] { 4, 5 }.SequenceEqual(byBinary.Binary!.ToArray()), Is.True);
					});
				}
			}
		}
	}
}
