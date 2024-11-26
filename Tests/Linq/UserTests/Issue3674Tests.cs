using System.Linq;
using System.Threading;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3674Tests : TestBase
	{
		// access disabled as it is in-process provider and needs to use our stack, which is very limited here
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3674")]
		public void InThread([DataSources(false, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext((string)context!);
			using var tb = db.CreateLocalTable<Entity>();

			// 512 Kb used by IIS and MacOS runtime
			// we will use less to detect regressions earlier
			// e.g. now it required 130Kb (release) / 190Kb (debug) of memory
			// Note that stack use could depend on provider, so we test all of them
			// UPDATE: after fixes of binary expression aggregation implementation it works with 70K limit (debug, net9.0)
			var thread = new Thread(ThreadBody, 80 * 1024);
			thread.Start(tb);
			thread.Join();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3674")]
		public void WithoutThread([DataSources(false)] string context)
		{
			using var db = GetDataContext((string)context!);
			using var tb = db.CreateLocalTable<Entity>();

			ThreadBody(tb);
		}

		void ThreadBody(object? context)
		{
			var entity = ((ITable<Entity>)context!).FirstOrDefault(p => p.Code == "42" && (
						p.DIM_Company == null || p.DIM_Company == ""
						|| p.DIM_Company == "1" || p.DIM_Company == "2" || p.DIM_Company == "3" || p.DIM_Company == "4" || p.DIM_Company == "5" || p.DIM_Company == "6" || p.DIM_Company == "7" || p.DIM_Company == "8" || p.DIM_Company == "9" || p.DIM_Company == "0"
					) && (
						p.DIM_Branch == null || p.DIM_Branch == ""
						|| p.DIM_Branch == "1" || p.DIM_Branch == "2" || p.DIM_Branch == "3" || p.DIM_Branch == "4" || p.DIM_Branch == "5" || p.DIM_Branch == "6" || p.DIM_Branch == "7" || p.DIM_Branch == "8" || p.DIM_Branch == "9" || p.DIM_Branch == "0"
					) && (
						p.DIM_Location == null || p.DIM_Location == ""
						|| p.DIM_Location == "1" || p.DIM_Location == "2" || p.DIM_Location == "3" || p.DIM_Location == "4" || p.DIM_Location == "5" || p.DIM_Location == "6" || p.DIM_Location == "7" || p.DIM_Location == "8" || p.DIM_Location == "9" || p.DIM_Location == "0"
					) && (
						p.DIM_MSegment == null || p.DIM_MSegment == ""
						|| p.DIM_MSegment == "1" || p.DIM_MSegment == "2" || p.DIM_MSegment == "3" || p.DIM_MSegment == "4" || p.DIM_MSegment == "5" || p.DIM_MSegment == "6" || p.DIM_MSegment == "7" || p.DIM_MSegment == "8" || p.DIM_MSegment == "9" || p.DIM_MSegment == "0"
					) && (
						p.DIM_Make == null || p.DIM_Make == ""
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
					)
				);
		}

		[Table("Issue3674Tests")]
		sealed class Entity
		{
			[Column(Length = 30, CanBeNull = false)] public string Code { get; set; } = null!;
			[Column(Length = 30)] public string? DIM_Company { get; set; }
			[Column(Length = 30)] public string? DIM_Branch { get; set; }
			[Column(Length = 30)] public string? DIM_Location { get; set; }
			[Column(Length = 30)] public string? DIM_MSegment { get; set; }
			[Column(Length = 30)] public string? DIM_Make { get; set; }
		}
	}
}
