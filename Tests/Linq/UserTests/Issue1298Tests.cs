using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Linq;

namespace Tests.UserTests
{
	sealed class Issue1298Tests : TestBase
	{
		[Table("qwerty")]
		public sealed class Qwerty
		{
			[Column]
			public long Id { get; set; }

			[Column]
			public string? asdfgh { get; set; }
		}

		[Table("mega_composites")]
		public class mega_composites
		{
			public mega_composites() : base()
			{
				{
					y1 = new mega_composites__y1();
				}
			}

			public virtual mega_composites__y1 y1 { get; set; }

			[Column]
			public long? ref1 { get; set; }

			public sealed class mega_composites__y1
			{
				public mega_composites__y1() : base()
				{
					q1 = new mega_composites__y1__q1();
				}
				public mega_composites__y1__q1 q1 { get; set; }

				public sealed class mega_composites__y1__q1
				{
					[Column("\"y1.q1.ref1\"")]
					public long? ref1 { get; set; }
				}
			}
		}

		public sealed class __mega_composites_View : mega_composites
		{
			public string? __face_y1_q1_ref1 { get; set; }
			public string? __face_ref1 { get; set; }
		}

		[Test]
		public void Issue1298Test([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			using (db.CreateLocalTable<mega_composites>())
			using (db.CreateLocalTable<Qwerty>())
			{
				db.Insert(new Qwerty() { Id = 1, asdfgh = "res1" });
				db.Insert(new Qwerty() { Id = 100500, asdfgh = "res100500" });

				db.Insert(new mega_composites()
				{
					ref1 = 100500,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 100500
						}
					}
				});
				db.Insert(new mega_composites()
				{
					ref1 = 1,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 100500
						}
					}
				});
				db.Insert(new mega_composites()
				{
					ref1 = 100500,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 1
						}
					}
				});

				var ref1 = db.GetTable<mega_composites>()
									.Select(x => new __mega_composites_View
									{
										ref1 = x.ref1,
										__face_ref1 = db.GetTable<Qwerty>().Where(q => q.Id == x.ref1).Select(q => q.asdfgh).FirstOrDefault()
									}).Take(2).ToArray();

				Assert.That(ref1, Is.Not.Null);
			}

		}

		[Test, ActiveIssue(1298, Details = "Expression 'x.y1' is not a Field.")]
		public void Issue1298Test1([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			using (db.CreateLocalTable<mega_composites>())
			using (db.CreateLocalTable<Qwerty>())
			{
				db.Insert(new Qwerty() { Id = 1, asdfgh = "res1" });
				db.Insert(new Qwerty() { Id = 100500, asdfgh = "res100500" });

				db.Insert(new mega_composites()
				{
					ref1 = 100500,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 100500
						}
					}
				});
				db.Insert(new mega_composites()
				{
					ref1 = 1,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 100500
						}
					}
				});
				db.Insert(new mega_composites()
				{
					ref1 = 100500,
					y1 = new mega_composites.mega_composites__y1()
					{
						q1 = new mega_composites.mega_composites__y1.mega_composites__y1__q1()
						{
							ref1 = 1
						}
					}
				});

				var ref1 = db.GetTable<mega_composites>()
									.Select(x => new __mega_composites_View
									{
										y1 = x.y1,
										__face_y1_q1_ref1 = db.GetTable<Qwerty>().Where(q => q.Id == x.y1.q1.ref1).Select(q => q.asdfgh).FirstOrDefault()
									}).Take(2).ToArray();

				Assert.That(ref1, Is.Not.Null);
			}


		}
	}
}
