using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue513Tests : TestBase
	{
		[Test]
		public void Simple([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(InheritanceParent[0].GetType(), Is.EqualTo(typeof(InheritanceParentBase)));
					Assert.That(InheritanceParent[1].GetType(), Is.EqualTo(typeof(InheritanceParent1)));
					Assert.That(InheritanceParent[2].GetType(), Is.EqualTo(typeof(InheritanceParent2)));
				}

				AreEqual(InheritanceParent, db.InheritanceParent);
				AreEqual(InheritanceChild,  db.InheritanceChild);
			}
		}

		// Informix disabled due to issue, described here (but it reproduced with client 4.1):
		// https://www-01.ibm.com/support/docview.wss?uid=swg1IC66046
		[Test]
		public void Test([DataSources(TestProvName.AllInformix)] string context)
		{
			using (new DisableBaseline("Multi-threading"))
			using (var semaphore = new Semaphore(0, 10))
			{
				var tasks = new Task[10];
				for (var i = 0; i < 10; i++)
					tasks[i] = new Task(() => TestInternal(context, semaphore));

				for (var i = 0; i < 10; i++)
					tasks[i].Start();

				Thread.Sleep(100);
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
