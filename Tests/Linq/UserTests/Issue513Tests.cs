using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using Model;

	[TestFixture]
	public class Issue513Tests : TestBase
	{
		[Test, Category("WindowsOnly")]
		public void Simple([DataSources] string context)
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

		// Informix disabled due to issue, described here (but it reproduced with client 4.1):
		// https://www-01.ibm.com/support/docview.wss?uid=swg1IC66046
		[Test, Category("WindowsOnly")]
		public void Test([DataSources(ProviderName.SQLiteMS, ProviderName.Informix)] string context)
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
