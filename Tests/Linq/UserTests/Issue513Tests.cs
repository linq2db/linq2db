using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue513Tests : TestBase
	{
		System.Threading.Semaphore _semaphore = new System.Threading.Semaphore(0, 10);

		[Table ("Child")]
		[InheritanceMapping(Code = 1,    Type = typeof(Child513Base))]
		[InheritanceMapping(Code = null, Type = typeof(Child513))]
		public class Child513Base
		{
			[Column, PrimaryKey, NotNull]
			public int? ChildId { get; set; }

			[Column(IsDiscriminator = true)]
			public string TypeDiscriminator { get; set; }
		}

		[Column("ParentId", "Parent.ParentId")]
		public class Child513 : Child513Base
		{
			[Association(ThisKey = "Parent.ParentID", OtherKey = "ParentID")]
			public Parent513 Parent;
		}

		[Table("Parent")]
		public class Parent513
		{
			[Column]
			public int ParentID;
		}

		[DataContextSource(false)]
		public void Test(string context)
		{
			using (var semaphore = new Semaphore(0, 10))
			{
				var tasks = new Task[10];
				for (var i = 0; i < 10; i++)
					tasks[i] = new Task(() => TestInternal(context, semaphore));

				for (var i = 0; i < 10; i++)
					tasks[i].Start();

				Thread   .Sleep(100);
				semaphore.Release(10);

				Task.WaitAll(tasks);
			}
		}

		public void TestInternal(string context, Semaphore semaphore)
		{
			try
			{
				using (var db = GetDataContext(context))
				{
					semaphore.WaitOne();
					var r = db.GetTable<Child513>().Select(_ => _.Parent).Distinct();
					Assert.IsNotEmpty(r);
				}
			}
			finally
			{
				semaphore.Release();
			}
		}
	}
}
