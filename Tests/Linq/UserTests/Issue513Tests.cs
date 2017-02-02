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
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue513Tests : TestBase
	{
		System.Threading.Semaphore _semaphore = new System.Threading.Semaphore(0, 10);

		[DataContextSource]
		public void Simple(string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(typeof(InheritanceParentBase), InheritanceParent[0].GetType());
				Assert.AreEqual(typeof(InheritanceParent1),    InheritanceParent[1].GetType());
				Assert.AreEqual(typeof(InheritanceParent2),    InheritanceParent[2].GetType());

				AreEqual(InheritanceParent, db.InheritanceParent);
				AreEqual(InheritanceChild,  db.InheritanceChild);
			}
		}

		[DataContextSource]
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

		private void TestInternal(string context, Semaphore semaphore)
		{
			try
			{
				using (var db = GetDataContext(context))
				{
					semaphore.WaitOne();
					var r = db.InheritanceChild.Select(_ => _.Parent).Distinct();
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
