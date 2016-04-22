using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
    using LinqToDB.Data;

    [TestFixture]
    public class Issue356 : TestBase
    {
        public class Foo
        {
            public long Id { get; set; }
            public string Value { get; set; }
        }

        public class Bar
        {
            public long Key { get; set; }
        }

        public void SetUp(string context)
        {
            using (var db = new DataConnection(context))
            {
                db.CreateTable<Foo>();
                db.CreateTable<Bar>();
            }
        }

        public void TearDown(string context)
        {
            using (var db = new DataConnection(context))
            {
                db.DropTable<Foo>();
                db.DropTable<Bar>();
            }
        }

        [Test, IncludeDataContextSource(ProviderName.SqlServer2014, ProviderName.SQLite, ProviderName.SqlCe)]
        public void Test(string context)
        {
            SetUp(context);
            try
            {
                using (var db = new DataConnection(context))
                {
                    var union = db.GetTable<Foo>()
                        .Union(db.GetTable<Foo>())
                        .Distinct();

                    var result = db.GetTable<Bar>()
                        .SelectMany(x => union.Where(date => date.Id == x.Key).Select(z => new {x.Key, z.Value}));

                    Assert.That(() => result.Take(10).ToArray(), Throws.Nothing);
                }
            }
            finally
            {
                TearDown(context);
            }
        }
    }
}
