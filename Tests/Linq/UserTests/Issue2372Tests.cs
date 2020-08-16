﻿using NUnit.Framework;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using System;
using System.Collections.Generic;
using LinqToDB.Mapping;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2372Tests : TestBase
	{
		public enum InventoryResourceStatus
		{
			Undefined = 0,
			Used = 40,
			Finished = 88
		}

		public class InventoryResourceDTO
		{
			public Guid Id { get; set; }

			public InventoryResourceStatus Status { get; set; }
		}

		[Test]
		public void Issue2372Test(
			[IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			Model.ITestDataContext? db1 = null;
			try
			{
				var ms1 = new MappingSchema();
				var fmb1 = ms1.GetFluentMappingBuilder();
				fmb1.Entity<InventoryResourceDTO>()
				  .HasTableName("InventoryResource")
				  .Property(e => e.Id).IsPrimaryKey()
				  .Property(e => e.Status).HasDataType(DataType.NVarChar);

				var ms2 = new MappingSchema();
				ms2.SetConverter<InventoryResourceStatus, string>((obj) =>
				{
					return obj.ToString();
				});
				ms2.SetConverter<InventoryResourceStatus, DataParameter>((obj) =>
				{
					return new DataParameter { Value = obj.ToString() };
				});
				ms2.SetConverter<string, InventoryResourceStatus>((txt) =>
				{
					return (InventoryResourceStatus)Enum.Parse(typeof(InventoryResourceStatus), txt, true);
				});

				var fmb2 = ms2.GetFluentMappingBuilder();
				fmb2.Entity<InventoryResourceDTO>()
				  .HasTableName("InventoryResource")
				  .Property(e => e.Id).IsPrimaryKey()
				  .Property(e => e.Status);

				db1 = GetDataContext(context, ms1);
				db1.DropTable<InventoryResourceDTO>(throwExceptionIfNotExists: false);
				db1.CreateLocalTable<InventoryResourceDTO>();

				using (var db2 = GetDataContext(context, ms2))
				{
					var dto1 = new InventoryResourceDTO
					{
						Status = InventoryResourceStatus.Used,
						Id = Guid.NewGuid()
					};
					db2.Insert(dto1);
				}
			}
			finally
			{
				if (db1 != null)
					db1.Dispose();
			}
		}
	}
}
