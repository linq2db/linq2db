using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3109Tests : TestBase
	{
		private sealed class TestDb : DataConnection
		{
			public TestDb(string configuration) : base(configuration)
			{
			}

			public ITable<Left>      Lefts      => this.GetTable<Left>();
			public ITable<LeftRight> LeftRights => this.GetTable<LeftRight>();
			public ITable<Right>     Rights     => this.GetTable<Right>();
		}

		private sealed class Left
		{
			[Association(ThisKey = nameof(LeftId), OtherKey = nameof(LeftRight.LeftId))]
			public IEnumerable<LeftRight>? LeftRights;

			[Column(IsPrimaryKey = true, CanBeNull = false)]
			public int LeftId { get; set; }

			public string?             LeftData { get; set; }
			public IEnumerable<Right>? Rights   => LeftRights?.Select(x => x.Right!);

			public override string ToString()
			{
				return $"{nameof(Left)} : [{LeftId} : {LeftData}]";
			}
		}

		private sealed class LeftRight
		{
			[Column(IsPrimaryKey = true, CanBeNull = false)]
			public int LeftId { get; set; }

			[Column(IsPrimaryKey = true, CanBeNull = false, DataType = DataType.Blob)]
			[Column(IsPrimaryKey = true, CanBeNull = false, DataType = DataType.VarBinary, Configuration = ProviderName.ClickHouse)]
			public byte[] RightId { get; set; } = null!;

			[Association(ThisKey = nameof(LeftId), OtherKey = nameof(Issue3109Tests.Left.LeftId))]
			public Left? Left { get; set; }

			[Association(ThisKey = nameof(RightId), OtherKey = nameof(Issue3109Tests.Right.RightId))]
			public Right? Right { get; set; }

			public override string ToString()
			{
				return $"{nameof(LeftRight)} : [{LeftId} <=> {BitConverter.ToString(RightId)}]";
			}
		}

		private sealed class Right
		{
			[Column(IsPrimaryKey = true, CanBeNull = false, DataType = DataType.Blob)]
			[Column(IsPrimaryKey = true, CanBeNull = false, DataType = DataType.VarBinary, Configuration = ProviderName.ClickHouse)]
			public byte[] RightId { get; set; } = null!;

			public string? RightData { get; set; }

			[Association(ThisKey = nameof(RightId), OtherKey = nameof(LeftRight.RightId))]
			public IEnumerable<LeftRight> LeftRights { get; set; } = null!;

			public IEnumerable<Left> Lefts => LeftRights.Select(x => x.Left!);

			public override string ToString()
			{
				return $"{nameof(Right)} : [{BitConverter.ToString(RightId)} : {RightData}]";
			}
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)]
			string context)
		{
			Left      left      = new() { LeftId  = 1 };
			Right     right     = new() { RightId = new byte[] { 2 } };
			LeftRight leftRight = new() { LeftId  = 1, RightId = new byte[] { 2 } };

			using (var db = new TestDb(context))
			using (db.CreateLocalTable(new[] { left }))
			using (db.CreateLocalTable(new[] { right }))
			using (db.CreateLocalTable(new[] { leftRight }))
			{
				
				var leftRightItem = db.LeftRights
					.LoadWith(x => x.Left)
					.LoadWith(x => x.Right)
					.First();

				var leftItem      = db.Lefts.LoadWith(x => x.LeftRights).First();
				var rightItem     = db.Rights.LoadWith(x => x.LeftRights).First();

				if (leftRightItem.Left == null)
				{
					throw new InvalidOperationException("LeftRight type failed to load Left association.");
				}

				if (leftRightItem.Right == null)
				{
					throw new InvalidOperationException("LeftRight type failed to load Right association.");
				}

				if (leftItem.LeftRights?.Count() != 1)
				{
					throw new InvalidOperationException("Left type failed to load LeftRight association.");
				}
				
				if (rightItem.LeftRights.Count() != 1)
				{
					throw new InvalidOperationException("Right type failed to load LeftRight association.");
				}
			}
		}
	}
}
