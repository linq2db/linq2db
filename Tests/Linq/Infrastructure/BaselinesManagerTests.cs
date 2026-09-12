using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.Infrastructure
{
	/// <summary>
	/// Capture accounting behind <see cref="BaselinesManager.LogQuery"/>. The trace sink calls it on whatever
	/// thread ran the query, so a test issuing concurrent queries appends from several threads at once — and the
	/// first appends of a test also race to <em>create</em> the shared builder, where a last-writer-wins store
	/// used to drop one thread's builder whole.
	/// </summary>
	/// <remarks>
	/// This fixture deliberately takes no data-source parameter: <c>BaselinesWriter.Write</c> returns before
	/// touching the disk when the test has none, so hammering the buffer here writes no baseline file.
	/// </remarks>
	[TestFixture]
	public class BaselinesManagerTests : TestBase
	{
		[Test]
		public void LogQueryKeepsEveryConcurrentAppend()
		{
			const int writers        = 8;
			const int linesPerWriter = 500;

			using var start = new ManualResetEventSlim(false);

			var writerTasks = Enumerable.Range(0, writers).Select(_ => Task.Run(() =>
			{
				// released together, so the contended window includes the builder's lazy creation
				start.Wait();

				for (var i = 0; i < linesPerWriter; i++)
					BaselinesManager.LogQuery("SELECT 1");
			})).ToArray();

			start.Set();
			Task.WaitAll(writerTasks);

			var baseline = CustomTestContext.Get().Get<StringBuilder>(CustomTestContext.BASELINE);

			baseline.ShouldNotBeNull();
			baseline.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Length
				.ShouldBe(writers * linesPerWriter);
		}
	}
}
