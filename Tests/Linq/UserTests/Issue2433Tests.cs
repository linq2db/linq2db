﻿using NUnit.Framework;
using LinqToDB.Data;
using System;
using LinqToDB.Mapping;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2433Tests : TestBase
	{
		public enum InventoryResourceStatus
		{
			Undefined = 0,
			Used = 40,
			Finished = 88
		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column]
			public Guid Id { get; set; }

			[Column]
			public InventoryResourceStatus Status { get; set; }

			[Column]
			public Guid ResourceID { get; set; }

			[Column]
			public DateTime? ModifiedTimeStamp { get; set; }
		}

		[Test]
		public void Issue2433Test(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (var itb = db.CreateLocalTable<InventoryResourceDTO>())
				{
					var dto1 = new InventoryResourceDTO
					{
						ResourceID = Guid.NewGuid(),
						Status = InventoryResourceStatus.Used,
						ModifiedTimeStamp = DateTime.UtcNow - TimeSpan.FromHours(2),
						Id = Guid.NewGuid()
					};
					((DataConnection)db).BulkCopy(new BulkCopyOptions
					{
						CheckConstraints = true,
						BulkCopyType = BulkCopyType.ProviderSpecific,
						MaxBatchSize = 5000,
						UseInternalTransaction = false,
						NotifyAfter = 2000,
						BulkCopyTimeout = 0,
						RowsCopiedCallback = i =>
						{
						}
					},
					new[] { dto1 });
				}
			}
		}
	}
}
