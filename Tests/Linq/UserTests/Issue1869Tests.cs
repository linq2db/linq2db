using System;
using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue1869Tests : TestBase
	{
		public class PropertyChangedNotifier
		{
		}

		[Table(Name = "tblFtq")]
		public class FtqData : PropertyChangedNotifier
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column, NotNull]
			public DateTime EntryDate { get; set; }

			[Column, NotNull]
			public byte EntryShift { get; set; }

			[Column, NotNull]
			public int Id_Operator { get; set; }

			[Column, NotNull]
			public int Id_Reference { get; set; }

			[Column, NotNull]
			public int Id_Defect { get; set; }

			[Column, NotNull]
			public int Qty { get; set; }

			[Association(ThisKey = nameof(Id_Defect), OtherKey = nameof(Issue1869Tests.Defect.Id), CanBeNull = false)]
			public Defect Defect { get; set; } = null!;
		}

		[Table(Name = "tblDefect")]
		public class Defect : PropertyChangedNotifier
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column, NotNull]
			public string Nam { get; set; } = null!;

			[Column, NotNull]
			public int Id_Workstation { get; set; }

			[Column, NotNull]
			public bool Ok { get; set; }

			[Column, NotNull]
			public bool Del { get; set; }

			[Association(ThisKey = nameof(Id_Workstation), OtherKey = nameof(Issue1869Tests.Workstation.Id), CanBeNull = false)]
			public Workstation Workstation { get; set; } = null!;
		}

		[Table(Name = "tblWorkstation")]
		public class Workstation : PropertyChangedNotifier
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column, NotNull]
			public int Id_WorkstationGroup { get; set; }

			[Column, NotNull]
			public string Nam { get; set; } = null!;

			[Column, NotNull]
			public bool Del { get; set; }

			[Association(ThisKey = nameof(Id_WorkstationGroup), OtherKey = nameof(Issue1869Tests.WorkstationGroup.Id), CanBeNull = false)]
			public WorkstationGroup WorkstationGroup { get; set; } = null!;
		}

		[Table(Name = "tblWorkstationGroup")]
		public class WorkstationGroup : PropertyChangedNotifier
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column, NotNull]
			public string Nam { get; set; } = null!;

			[Column, NotNull]
			public int Id_SectorPart { get; set; }

			[Column, NotNull]
			public int Id_Sector { get; set; }

			[Column, NotNull]
			public bool Del { get; set; }
		}

		[Table(Name = "tblMonth")]
		public class Month : PropertyChangedNotifier
		{
			[Column]
			public int MonthNumber { get; set; }
		}

		[Test]
		public void TestLeftJoin([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Month>())
			using (db.CreateLocalTable<Defect>())
			using (db.CreateLocalTable<FtqData>())
			using (db.CreateLocalTable<Workstation>())
			using (db.CreateLocalTable<WorkstationGroup>())
			{
				var dateMin = TestData.DateTime;
				var dateMax = TestData.DateTime;
				var sectorId = 1;

				var query1 = from q in db.GetTable<FtqData>()
							 where q.EntryDate >= dateMin
						  && q.EntryDate <= dateMax
						  && q.Defect.Workstation.WorkstationGroup.Id_Sector == sectorId
							 let MonthNumber = q.EntryDate.Month
							 group q by new { MonthNumber, q.Defect.Workstation.Id_WorkstationGroup }
					into g
							 select new
							 {
								 MonthNumber = g.Key.MonthNumber,
								 Id_WorkstationGroup = g.Key.Id_WorkstationGroup,
								 Ftq = g.Sum(_ => _.Qty) / g.Sum(_ => _.Defect.Ok ? 0 : _.Qty)
							 };

				var query2 = from q in query1
							 group q by q.MonthNumber
					into g
							 select new
							 {
								 MonthNumber = g.Key,
								 Ftq = g.Sum(_ => _.Ftq)
							 };

				var query3 = from m in db.GetTable<Month>()
							 from q in query2.Where(q => q.MonthNumber == m.MonthNumber).DefaultIfEmpty()
							 select new
							 {
								 m,
								 q
							 };

				var result = query3.ToArray();

				query3.GetSelectQuery().Select.Columns.Should().HaveCount(3);
			}
		}

	}
}
