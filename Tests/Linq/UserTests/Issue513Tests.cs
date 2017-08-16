﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class Issue513Tests : TestBase
	{
		[Test, DataContextSource, Category("WindowsOnly")]
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

		[Test, DataContextSource(TestProvName.SQLiteMs), Category("WindowsOnly")]
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

		void TestInternal(string context, Semaphore semaphore)
		{
			try
			{
				using (var db = GetDataContext(context))
				{
					semaphore.WaitOne();

					AreEqual(
						   InheritanceChild.Select(_ => _.Parent).Distinct(),
						db.InheritanceChild.Select(_ => _.Parent).Distinct());
				}
			}
			finally
			{
				semaphore.Release();
			}
		}
	}
}
