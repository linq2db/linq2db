using System.Linq;
using System.Threading;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class StackUseTests : TestBase
	{
		sealed class Issue5265Table
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable01
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable02
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable03
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable04
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable05
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5265")]
		public void EagerLoadProjection([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue5265Table>();
			using var t1 = db.CreateLocalTable<Issue5265SubTable01>();
			using var t2 = db.CreateLocalTable<Issue5265SubTable02>();
			using var t3 = db.CreateLocalTable<Issue5265SubTable03>();
			using var t4 = db.CreateLocalTable<Issue5265SubTable04>();
			using var t5 = db.CreateLocalTable<Issue5265SubTable05>();

#if DEBUG
			// initial: 710K
			const int LKG_SIZE = 200 * 1024;
#else
			// initial: 390K
			const int LKG_SIZE = 190 * 1024;
#endif
			var thread = new Thread(ThreadBody, LKG_SIZE);
			thread.Start();
			thread.Join();

			void ThreadBody(object? context)
			{
				_ = tb
					.LoadWith(e => e.SubTable3!.SubTable5!.SubTable2!.SubTable4!.SubTable1!.SubTable2!
						.SubTable3!.SubTable3!.SubTable5!.SubTable2!.SubTable4!.SubTable1!.SubTable4)

					.ToArray();
			}
		}
	}
}
