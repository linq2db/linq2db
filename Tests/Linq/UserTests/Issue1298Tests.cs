using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.UserTests
{
	class Issue1298Tests : TestBase
	{
		[Table("qwerty")]
		public class qwerty
		{
			[Column]
			public long Id { get; set; }

			[Column]
			public System.String asdfgh { get; set; }
		}

		[Table("mega_composites")]
		public class mega_composites
		{
			public mega_composites() : base()
			{
				{
					this.y1 = new mega_composites__y1();
				}
			}

			public virtual mega_composites__y1 y1 { get; set; }

			[Column]
			public System.Nullable<System.Int64> ref1 { get; set; }

			public class mega_composites__y1
			{
				public mega_composites__y1() : base()
				{
					this.q1 = new mega_composites__y1__q1();
				}
				public virtual mega_composites__y1__q1 q1 { get; set; }

				public class mega_composites__y1__q1
				{
					[Column("\"y1.q1.ref1\"")]
					public System.Nullable<System.Int64> ref1 { get; set; }
				}
			}
		}

		public class __mega_composites_View : mega_composites
		{
			public System.String __face_y1_q1_ref1 { get; set; }
			public System.String __face_ref1 { get; set; }
		}

		[Test]
		public void Issue1298Test([IncludeDataSources(ProviderName.PostgreSQL)] string context)
		{
			using (var db = new DataConnection(context))
			using (db.BeginTransaction())
			{
				db.CreateTable<mega_composites>();
				db.CreateTable<qwerty>();

				db.Insert(new qwerty() { Id = 1, asdfgh = "res1" });
				db.Insert(new qwerty() { Id = 100500, asdfgh = "res100500" });

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
										__face_ref1 = db.GetTable<qwerty>().Where(q => q.Id == x.ref1).Select(q => q.asdfgh).FirstOrDefault()
									}).Take(2).ToArray();

				Assert.NotNull(ref1);
			}

		}

		[Test, ActiveIssue(1298)]
		public void Issue1298Test1([IncludeDataSources(ProviderName.PostgreSQL)] string context)
		{
			using (var db = new DataConnection(context))
			using (db.BeginTransaction())
			{
				db.CreateTable<mega_composites>();
				db.CreateTable<qwerty>();

				db.Insert(new qwerty() { Id = 1, asdfgh = "res1" });
				db.Insert(new qwerty() { Id = 100500, asdfgh = "res100500" });

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
										__face_y1_q1_ref1 = db.GetTable<qwerty>().Where(q => q.Id == x.y1.q1.ref1).Select(q => q.asdfgh).FirstOrDefault()
									}).Take(2).ToArray();

				Assert.NotNull(ref1);
			}


		}
	}
}
