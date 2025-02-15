using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4813Tests : TestBase
	{
		[Table("Returns")]
		public class Return
		{
			[Column("Id"), PrimaryKey, NotNull]
			public int Id { get; set; }

			// Always contains a valid value in our tests.
			[Column("OriginalSaleId"), NotNull]
			public int OriginalSaleId { get; set; }

			// Will be null in our tests.
			[Column("ReshipSaleId"), Nullable]
			public int? ReshipSaleId { get; set; }
		}

		[Table("Sales")]
		public class Sale
		{
			[Column("Id"), PrimaryKey, NotNull]
			public int Id { get; set; }
		}

		/// <summary>
		/// This test demonstrates the correct behavior when not using dictionary projections.
		/// Expected:
		/// - OriginalSale is materialized as an object with Id=43757.
		/// - ReshipSale is null.
		/// </summary>
		[Test]
		public void WorksCorrectlyTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			// Create a Returns table with one row.
			using var returnsTable = db.CreateLocalTable(new[]
			{
				new Return { Id = 1, OriginalSaleId = 43757, ReshipSaleId = null }
			});

			// Create a Sales table with one row.
			using var salesTable = db.CreateLocalTable(new[]
			{
				new Sale { Id = 43757 }
			});

			var queryWorksCorrectly =
				from r in db.GetTable<Return>()
				where r.ReshipSaleId == null
				select new
				{
					r.Id,
					r.OriginalSaleId,
					OriginalSale = db.GetTable<Sale>()
						.Where(x => x.Id == r.OriginalSaleId)
						.Select(x => new { x.Id })
						.FirstOrDefault(),
					r.ReshipSaleId,
					ReshipSale = db.GetTable<Sale>()
						.Where(x => x.Id == r.ReshipSaleId)
						.Select(x => new { x.Id })
						.FirstOrDefault()
				};

			var result = queryWorksCorrectly.Take(1).ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result, Has.Count.EqualTo(1), "Expected exactly one result row.");
				var row = result.First();

				// Verify that OriginalSale is correctly materialized.
				Assert.That(row.OriginalSale, Is.Not.Null, "OriginalSale should not be null.");
				Assert.That(row.OriginalSale.Id, Is.EqualTo(43757), "OriginalSale Id should be 43757.");

				// Verify that ReshipSale is null.
				Assert.That(row.ReshipSale, Is.Null, "ReshipSale should be null.");
			});
		}

		/// <summary>
		/// This test uses dictionary projections with OriginalSale projected first.
		/// Expected:
		/// - OriginalSale: a dictionary with {"Id": 43757}.
		/// - ReshipSale: null because ReshipSaleId is null.
		/// </summary>
		[Test]
		public void FailsNotNullTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			using var returnsTable = db.CreateLocalTable(new[]
			{
				new Return { Id = 1, OriginalSaleId = 43757, ReshipSaleId = null }
			});

			using var salesTable = db.CreateLocalTable(new[]
			{
				new Sale { Id = 43757 }
			});

			var queryFailsNotNull =
				from r in db.GetTable<Return>()
				where r.ReshipSaleId == null
				select new
				{
					r.Id,
					r.OriginalSaleId,
					OriginalSale = db.GetTable<Sale>()
						.Where(x => x.Id == r.OriginalSaleId)
						.Select(x => new Dictionary<string, object> { { "Id", x.Id } })
						.FirstOrDefault(),
					r.ReshipSaleId,
					ReshipSale = db.GetTable<Sale>()
						.Where(x => x.Id == r.ReshipSaleId)
						.Select(x => new Dictionary<string, object> { { "Id", x.Id } })
						.FirstOrDefault()
				};

			var result = queryFailsNotNull.Take(1).ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result, Has.Count.EqualTo(1), "Expected exactly one result row.");
				var row = result.First();

				// Expected behavior: OriginalSale should be a dictionary with {"Id": 43757}.
				Assert.That(row.OriginalSale, Is.Not.Null, "OriginalSale dictionary should not be null.");
				Assert.That(row.OriginalSale.ContainsKey("Id"), Is.True, "OriginalSale dictionary should contain key 'Id'.");
				Assert.That(row.OriginalSale["Id"], Is.EqualTo(43757), "OriginalSale dictionary should have Id 43757.");

				// Expected behavior: ReshipSale should be null because ReshipSaleId is null.
				Assert.That(row.ReshipSale, Is.Null, "ReshipSale dictionary should be null when ReshipSaleId is null.");
			});
		}

		/// <summary>
		/// This test uses dictionary projections with the order reversed.
		/// Expected:
		/// - ReshipSale: null (because ReshipSaleId is null).
		/// - OriginalSale: a dictionary with {"Id": 43757}.
		/// </summary>
		[Test]
		public void FailsNullTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			using var returnsTable = db.CreateLocalTable(new[]
			{
				new Return { Id = 1, OriginalSaleId = 43757, ReshipSaleId = null }
			});

			using var salesTable = db.CreateLocalTable(new[]
			{
				new Sale { Id = 43757 }
			});

			var queryFailsNull =
				from r in db.GetTable<Return>()
				where r.ReshipSaleId == null
				select new
				{
					r.Id,
					r.ReshipSaleId,
					ReshipSale = db.GetTable<Sale>()
						.Where(x => x.Id == r.ReshipSaleId)
						.Select(x => new Dictionary<string, object> { { "Id", x.Id } })
						.FirstOrDefault(),
					r.OriginalSaleId,
					OriginalSale = db.GetTable<Sale>()
						.Where(x => x.Id == r.OriginalSaleId)
						.Select(x => new Dictionary<string, object> { { "Id", x.Id } })
						.FirstOrDefault()
				};

			var result = queryFailsNull.Take(1).ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result, Has.Count.EqualTo(1), "Expected exactly one result row.");
				var row = result.First();

				// Expected behavior: ReshipSale should be null.
				Assert.That(row.ReshipSale, Is.Null, "ReshipSale should be null because ReshipSaleId is null.");

				// Expected behavior: OriginalSale should be a dictionary with {"Id": 43757}.
				Assert.That(row.OriginalSale, Is.Not.Null, "OriginalSale dictionary should not be null.");
				Assert.That(row.OriginalSale.ContainsKey("Id"), Is.True, "OriginalSale dictionary should contain key 'Id'.");
				Assert.That(row.OriginalSale["Id"], Is.EqualTo(43757), "OriginalSale dictionary should have Id 43757.");
			});
		}
	}
}
