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
	[TestFixture]
	public class Issue513Tests : TestBase
	{
		System.Threading.Semaphore _semaphore = new System.Threading.Semaphore(0, 10);

		[Table ("Child")]
		[Column("ParentId", "Parent.ParentId")]
		public class Child513
		{
			[Association(ThisKey = "Parent.ParentId", OtherKey = "ParentId")]
			public Parent513 Parent;
		}

		[Table("Parent")]
		public class Parent513
		{
			[Column]
			public int ParentId;
		}

		[DataContextSource(false)]
		public void Test(string context)
		{
			var tasks = new Task[10];
			for (var i = 0; i < 10; i++)
				tasks[i] = new Task(() => TestInternal(context));

			for (var i = 0; i < 10; i++)
				tasks[i].Start();

			System.Threading.Thread.Sleep(1000);
			_semaphore.Release(10);

			Task.WaitAll(tasks);
		}

		public void TestInternal(string context)
		{
			using (var db = GetDataContext(context))
			{
				_semaphore.WaitOne();
				var r = db.GetTable<Child513>().Select(_ => _.Parent).Distinct();
				Assert.IsNotEmpty(r);
			}
		}
	}
}
