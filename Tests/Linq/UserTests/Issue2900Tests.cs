using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2900Tests : TestBase
	{
		[Table]
		public class Request
		{
			[PrimaryKey, Identity, Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Metric.RequestId))]
			public virtual ICollection<Metric> Metrics { get; set; } = null!;
		}

		[Table]
		public class Metric
		{
			[PrimaryKey, Identity, Column] public int     Id        { get; set; }
			[Column                      ] public int     RequestId { get; set; }
			[Column                      ] public double? Value     { get; set; }
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void TestIssue2900([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var requests = db.CreateLocalTable<Request>())
			using (db.CreateLocalTable<Metric>())
			{
				var result = requests
					.Select(a => new
					{
						Metrics = a.Metrics
							.Select(aa => new
							{
								Value = aa.Value.HasValue
									? new { Value = aa.Value.Value }
									: null,
							})
							.FirstOrDefault()
					})
					.ToArray();
			}
		}
	}
}
