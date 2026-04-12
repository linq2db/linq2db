using System.Linq;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void PercentileDiscGrouping([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					g.Key,
					PercentileDisc = g.PercentileDisc(0.5, (e, f) => f.OrderBy(e.IntValue)),
				};

				_ = query.ToList();
		}

		[Test]
		public void PercentileDiscGroupingProjection([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					g.Key,
					PercentileDisc = g.AsQueryable().Select(e => new
					{
						PD = g.PercentileDisc(0.5, (e2, f) => f.OrderBy(e2.IntValue)),
					}).First()
				};

				_ = query.ToList();
		}

		[Test, Explicit("IQueryable overload ambiguity with public WindowFunctionBuilder — needs review")]
		public async Task PercentileDisc([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var result1 = table.PercentileDisc(0.5, (e, f) => f.OrderBy(e.IntValue));

			var result2 = await table.PercentileDiscAsync(0.5, (e, f) => f.OrderBy(e.IntValue));
		}
	}
}
