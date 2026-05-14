using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5154Tests : TestBase
	{
		[Table]
		sealed class MasterClass
		{
			[Column] [PrimaryKey] public int     Id1   { get; set; }
			[Column]              public string? Value { get; set; }

			[Association(ThisKey = nameof(Id1), OtherKey = nameof(DetailClass.MasterId))]
			public List<DetailClass> Details { get; set; } = null!;
		}

		[Table]
		sealed class DetailClass
		{
			[Column] [PrimaryKey] public int     DetailId    { get; set; }
			[Column]              public int?    MasterId    { get; set; }
			[Column]              public string? DetailValue { get; set; }

			[Association(ThisKey = nameof(DetailId), OtherKey = nameof(SubDetailClass.DetailId))]
			public SubDetailClass[] SubDetails { get; set; } = null!;
		}

		[Table]
		sealed class SubDetailClass
		{
			[Column] [PrimaryKey] public int     SubDetailId    { get; set; }
			[Column]              public int?    DetailId       { get; set; }
			[Column]              public string? SubDetailValue { get; set; }
		}

		static (MasterClass[] masters, DetailClass[] details, SubDetailClass[] subDetails) GenerateData()
		{
			var masters = Enumerable.Range(1, 5)
				.Select(i => new MasterClass { Id1 = i, Value = "M" + i })
				.ToArray();

			var details = masters
				.SelectMany(m => Enumerable.Range(1, 3).Select(i => new DetailClass
				{
					DetailId    = m.Id1 * 100 + i,
					MasterId    = m.Id1,
					DetailValue = "D" + (m.Id1 * 100 + i),
				}))
				.ToArray();

			var subDetails = details
				.SelectMany(d => Enumerable.Range(1, 2).Select(i => new SubDetailClass
				{
					SubDetailId    = d.DetailId * 10 + i,
					DetailId       = d.DetailId,
					SubDetailValue = "S" + (d.DetailId * 10 + i),
				}))
				.ToArray();

			return (masters, details, subDetails);
		}

		// Regression test for #5154. Before the fix `Sql.Expr<T>(RawSqlString, params object[])` carried
		// `[SqlQueryDependentParams]` on its `parameters` arg; the cache-compare path
		// (EqualsToVisitor -> SqlQueryDependentParamsAttribute.ExpressionsEqual ->
		// ExpressionEvaluator.EvaluateExpression) compiled each parameter sub-expression on its own.
		// Multi-level eager-loaded projections capture transparent identifiers from outer scope; after
		// ExpressionQuery mutates `this.Expression` to `expressions.MainExpression`, those identifiers
		// are orphaned and the per-parameter compile throws `InvalidOperationException`.
		[Test]
		public void ToSqlQueryAndToArray_InEitherOrder([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateData();

			using var db        = GetDataContext(context);
			using var master    = db.CreateLocalTable(masterRecords);
			using var detail    = db.CreateLocalTable(detailRecords);
			using var subDetail = db.CreateLocalTable(subDetailRecords);

			var buildQuery = () =>
				from m in master
				let detailsRaw = detail.Where(d => d.MasterId == m.Id1)
				let mTag       = Sql.Expr<string>("'M' || {0}", m.Id1)
				select new
				{
					m.Id1,
					mTag,
					Details = (from d in detailsRaw
							   let dTag = Sql.Expr<string>("'D' || {0} || '/' || {1}", m.Id1, d.DetailId)
							   select new
							   {
								   d.DetailId,
								   dTag,
								   SubDetails = subDetail
									   .Where(s => s.DetailId == d.DetailId)
									   .Select(s => new
									   {
										   s.SubDetailId,
										   sTag = Sql.Expr<string>("'S' || {0} || '/' || {1} || '/' || {2}", m.Id1, d.DetailId, s.SubDetailId)
									   })
									   .ToArray(),
								   Another = d.SubDetails
							   }).ToArray()
				};

			// Order 1: ToSqlQuery -> ToArray on the same query instance. First call seeds the cache;
			// second call walks the tree against the cached entry.
			{
				var query = buildQuery();
				string? sql    = null;
				object? result = null;
				Assert.DoesNotThrow(() => sql    = query.ToSqlQuery().Sql);
				Assert.DoesNotThrow(() => result = query.ToArray());
				Assert.That(sql,    Is.Not.Null.And.Not.Empty);
				Assert.That(result, Is.Not.Null);
			}

			// Order 2: ToArray -> ToSqlQuery on a fresh, structurally identical query. The cache-compare
			// path runs on the first call here (it finds the entry seeded by the first ordering), which
			// is the failure mode the original report described.
			{
				var query = buildQuery();
				object? result = null;
				string? sql    = null;
				Assert.DoesNotThrow(() => result = query.ToArray());
				Assert.DoesNotThrow(() => sql    = query.ToSqlQuery().Sql);
				Assert.That(result, Is.Not.Null);
				Assert.That(sql,    Is.Not.Null.And.Not.Empty);
			}
		}
	}
}
