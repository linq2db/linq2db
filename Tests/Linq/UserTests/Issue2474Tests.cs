using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2474Tests : TestBase
	{
		[Table(Name = "STATUS_DATA")]
		public class StatusData
		{
			[Column("STATUS_TYPE_ID")]
			[PrimaryKey(0)]
			[NotNull]
			public int StatusTypeId { get; set; }

			[Column]
			[PrimaryKey(1)]
			[NotNull]
			public string NR { get; set; } = null!;
		}

		[Table(Name = "DETAIL")]
		public class Detail
		{
			[Column]
			[PrimaryKey]
			[NotNull]
			public int ID { get; set; }

			[Column("TYP_STATUS")]
			[NotNull]
			public int StatusType { get; set; }

			[Column]
			[NotNull]
			public string NR { get; set; } = null!;

			[Column]
			[NotNull]
			public DateTime DATUM { get; set; }

			[Association(QueryExpressionMethod = nameof(StatusDataDataExpr), CanBeNull = true)]
			public StatusData Status { get; set; } = null!;

			[ExpressionMethod(nameof(CashExistsExpr), IsColumn = true)]
			public bool HasCash { get; set; }

			public static Expression<Func<Detail, IDataContext, bool>> CashExistsExpr()
			{
				return (n, db) => db.GetTable<Cash>().Any(u => u.IdDetail == n.ID);
			}

			public static Expression<Func<Detail, IDataContext, IQueryable<StatusData>>> StatusDataDataExpr()
			{
				return (n, db) => db.GetTable<StatusData>().Where(c => c.StatusTypeId == n.StatusType && c.NR == n.NR);
			}
		}

		[Table(Name = "CASH")]
		public class Cash
		{
			[Column("ID_DETAIL")]
			[NotNull]
			public int IdDetail { get; set; }

			[Column("ID_TEXT")]
			[NotNull]
			public int IdText { get; set; }

			[Column]
			[NotNull]
			public decimal Fee { get; set; }
		}

		[Test]
		public void LoadWithTest([IncludeDataSources(TestProvName.AllSQLite)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Detail>())
			using (db.CreateLocalTable<StatusData>())
			using (db.CreateLocalTable<Cash>())
			{
				var q = from n in db.GetTable<Detail>()
						.LoadWith(n => n.Status)
					where n.StatusType == 2
					select n;
				Assert.DoesNotThrow(() => _ = q.ToList());
			}
		}
	}
}
