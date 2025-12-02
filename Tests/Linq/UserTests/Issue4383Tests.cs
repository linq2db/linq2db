using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4383Tests : TestBase
	{
		[Table]
		public class ElementTest
		{
			[Column("ELEMENT_ID"), PrimaryKey] public int Id { get; set; }
		}

		public interface IChainTest
		{
			[Column] public int Id { get; set; }
		}

		[Table("CHAINPOINTS")]
		public class SewerChainPointTest
		{
			[Column("CHAIN_ID"), PrimaryKey(0)] public int ElementId { get; set; }
		}

		[Table("CHAINS"), Column("CHAIN_ID", nameof(Id))]
		public class SewerChainTest : ElementTest, IChainTest
		{
			[Association(ThisKey = nameof(Id), OtherKey = nameof(SewerChainPointTest.ElementId), CanBeNull = true)]
			public IEnumerable<SewerChainPointTest>? ChainPoints { get; set; }
		}

		[Table]
		public class PumpLineChainTest<TChain> where TChain : IChainTest
		{
			[PrimaryKey, Column("LINE_ID")]  public int LineId  { get; set; }
			[PrimaryKey, Column("CHAIN_ID")] public int ChainId { get; set; }

			[Association(ThisKey = nameof(ChainId), OtherKey = nameof(IChainTest.Id))]
			public TChain Chain { get; set; } = default!;
		}

		[Table, Column("LINE_ID", nameof(Id))]
		public abstract class PumpLineTest<TChain, TPumpLineChain> : ElementTest
			where TChain : IChainTest
			where TPumpLineChain : PumpLineChainTest<TChain>
		{
			[Association(ThisKey = nameof(Id), OtherKey = nameof(PumpLineChainTest<>.LineId), CanBeNull = false)]
			public IEnumerable<TPumpLineChain> PipeLineChains { get; set; } = null!;
		}

		[Table("PUMPLINE_CHAINS")]
		public class SewerPumpLineChainTest : PumpLineChainTest<SewerChainTest>
		{
		}

		[Table("PUMPLINES")]
		public class SewerPumpLineTest : PumpLineTest<SewerChainTest, SewerPumpLineChainTest>
		{
		}

		[YdbTableNotFound]
		[Test]
		public void Test([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var data1 = new SewerPumpLineTest     [] { new() { Id = 1 }, new() { Id = 2 } };
			var data2 = new SewerPumpLineChainTest[] { new() { LineId = 1, ChainId = 11 }, new() { LineId = 2, ChainId = 22 } };
			var data3 = new SewerChainTest        [] { new() { Id = 11 }, new() { Id = 22 } };
			var data4 = new SewerChainPointTest   [] { new() { ElementId = 11 }, new() { ElementId = 22 } };

			using var t1 = db.CreateLocalTable(data1);
			using var t2 = db.CreateLocalTable(data2);
			using var t3 = db.CreateLocalTable(data3);
			using var t4 = db.CreateLocalTable(data4);

			var items = db.GetTable<SewerPumpLineTest>()
				.LoadWith(i => i.PipeLineChains)
				.ThenLoad(i => i.Chain.ChainPoints)
				.OrderBy (i => i.Id)
				.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(items.Select(r => new { r.Id }), Is.EquivalentTo(data1.Select(r => new { r.Id })));
				Assert.That(items[0].PipeLineChains.Select(r => new { r.LineId, r.ChainId }), Is.EquivalentTo(data2.Take(1).Select(r => new { r.LineId, r.ChainId })));
				Assert.That(items[1].PipeLineChains.Select(r => new { r.LineId, r.ChainId }), Is.EquivalentTo(data2.Skip(1).Select(r => new { r.LineId, r.ChainId })));
				Assert.That(items[0].PipeLineChains.Select(c => new { c.Chain.Id }), Is.EquivalentTo(data3.Take(1).Select(r => new { r.Id })));
				Assert.That(items[1].PipeLineChains.Select(c => new { c.Chain.Id }), Is.EquivalentTo(data3.Skip(1).Select(r => new { r.Id })));

				Assert.That(items[0].PipeLineChains.SelectMany(c => c.Chain.ChainPoints!).Select(c => new { c.ElementId }), Is.EquivalentTo(data4.Take(1).Select(r => new { r.ElementId })));
				Assert.That(items[1].PipeLineChains.SelectMany(c => c.Chain.ChainPoints!).Select(c => new { c.ElementId }), Is.EquivalentTo(data4.Skip(1).Select(r => new { r.ElementId })));
			}
		}
	}
}
