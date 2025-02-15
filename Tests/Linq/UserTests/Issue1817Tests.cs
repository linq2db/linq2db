using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Tools;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1817Tests : TestBase
	{
		public partial class Account
		{
			#region Simple columns

			public long Id { get; set; } // bigint
			public string? Number { get; set; } // varchar(20)
			public string? Name { get; set; } // varchar(5000)
			public AccountType Type { get; set; } // smallint
			public AccountState State { get; set; } // smallint

			#endregion
		}

		public partial class Transaction
		{
			#region Simple columns

			public long Id { get; set; } // bigint
			public long RequestId { get; set; } // bigint
			public DateTime OperDate { get; set; } // date
			public long DebitAccountId { get; set; } // bigint
			public long CreditAccountId { get; set; } // bigint

			#endregion
		}

		public enum AccountState
		{
		}

		public enum AccountType
		{
		}

		[Test]
		public void CteTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var accountIds = new long[] { 1, 2, 3, 4 };

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Account>())
			using (db.CreateLocalTable<Transaction>())
			{
				var accountsInfo =
					from a in db.GetTable<Account>()
					where a.Id.In(accountIds)
					select new
					{
						a.Id,
						a.Number,
						a.Name,
						a.Type,
						incomeBalance = Sql.AsSql(1),
						outgoingBalance = Sql.AsSql(2)
					};

				var accountsCte = accountsInfo.AsCte("accountsInfo");

				var query = from cte in accountsCte
					let max = db.GetTable<Transaction>().Where(_ => _.DebitAccountId == cte.Id)
						.Select(_ => _.OperDate)
						.Union(db.GetTable<Transaction>().Where(_ => _.CreditAccountId == cte.Id)
							.Select(_ => _.OperDate))
						.Max()
					select new
					{
						AccountId = cte.Id,
						AccountNumber = cte.Number,
						AccountType = cte.Type,
						AccountName = cte.Name,
						LastOperDate = max,
						IncomeBalance = cte.incomeBalance,
						OutgoingBalance = cte.outgoingBalance
					};

				var accInfo = query.ToArray();
			}
		}
	}

}
