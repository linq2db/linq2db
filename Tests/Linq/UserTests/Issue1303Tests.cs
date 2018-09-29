using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Data.Linq;
using System.Linq;

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
			[Column]
			public byte[] Array  { get; set; }
			[Column]
			public Binary Binary { get; set; }
		}

		[Test, DataContextSource]
		public void TestBinaryLiterals(string context)
		{
			using (var db = new DataConnection(context))
			using (var tbl = db.CreateLocalTable<Issue1303>())
			{
				tbl.Insert(() => new Issue1303()
				{
					ID = 1,
					Array = new byte[] { 1, 2,3 },
					Binary = new Binary(new byte[] { 4, 5})
				});

				var byId     = tbl.Where(_ => _.ID == 1)                                  .Single();
				var byArray  = tbl.Where(_ => _.Array == new byte[] { 1, 2, 3 })          .Single();
				var byBinary = tbl.Where(_ => _.Binary == new Binary(new byte[] { 4, 5 })).Single();

				Assert.AreEqual(1, byId.ID);
				Assert.True(new byte[] { 1, 2, 3 }.SequenceEqual(byId.Array));
				Assert.True(new byte[] { 4, 5 }.SequenceEqual(byId.Binary.ToArray()));

				Assert.AreEqual(1, byArray.ID);
				Assert.True(new byte[] { 1, 2, 3 }.SequenceEqual(byArray.Array));
				Assert.True(new byte[] { 4, 5 }.SequenceEqual(byArray.Binary.ToArray()));

				Assert.AreEqual(1, byBinary.ID);
				Assert.True(new byte[] { 1, 2, 3 }.SequenceEqual(byBinary.Array));
				Assert.True(new byte[] { 4, 5 }.SequenceEqual(byBinary.Binary.ToArray()));
			}
		}
	}
}
