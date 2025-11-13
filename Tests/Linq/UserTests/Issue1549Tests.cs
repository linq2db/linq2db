using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1549Tests : TestBase
	{
		[Table(Name = "billing_devtypes")]
		public partial class billing_Devtype
		{
			[Column("devtypeid"), PrimaryKey, Identity] public int Devtypeid   { get; set; } // integer
			[Column("typename", Length = 50), NotNull]  public string Typename { get; set; } = null!; // character varying(255)
			[Column(), NotNull]                         public int GlobalType  { get; set; } // integer

			#region Associations

			/// <summary>
			/// FK_billing.DevReadingType_billing.devtypes_DevTypeId_BackReference
			/// </summary>
			[Association(ThisKey = "Devtypeid", OtherKey = "DevTypeId", CanBeNull = true)]
			public IEnumerable<billing_DevReadingType> BillingDevReadingTypebillingdevtypesDevTypeIds { get; set; } = null!;

			/// <summary>
			/// fk_devices_devtypeid_devtypes_devtypeid_BackReference
			/// </summary>
			[Association(ThisKey = "Devtypeid", OtherKey = "Devtypeid", CanBeNull = true)]
			public IEnumerable<billing_Device> Fkdevicesdevtypeiddevtypeids { get; set; } = null!;

			#endregion
		}

		[Table(Name = "billing_devices")]
		public partial class billing_Device
		{
			[Column("devid", Length = 50), PrimaryKey, NotNull] public string  Devid  { get; set; } = null!; // character varying(255)
			[Column("sernum", Length = 50), Nullable]           public string? Sernum { get; set; } // character varying(255)

			[Column("devtypeid"), NotNull]                      public int Devtypeid { get; set; } // integer

			#region Associations

			/// <summary>
			/// FK_billing.TempReading_billing.devices_devid_BackReference
			/// </summary>
			[Association(ThisKey = "Devid", OtherKey = "Devid", CanBeNull = true)]
			public IEnumerable<billing_TempReading> BillingTempReadingbillingdevicesdevids { get; set; } = null!;

			/// <summary>
			/// fk_devices_devtypeid_devtypes_devtypeid
			/// </summary>
			[Association(ThisKey = "Devtypeid", OtherKey = "Devtypeid", CanBeNull = false)]
			public billing_Devtype Devtype { get; set; } = null!;

			#endregion
		}

		// FB: default name hits 31-length limit for generator name (till FB 4.0)
		[Table(Name = "billing_DevReadType", Configuration = ProviderName.Firebird25)]
		[Table(Name = "billing_DevReadType", Configuration = ProviderName.Firebird3)]
		[Table(Name = "billing_DevReadingType")]
		public partial class billing_DevReadingType
		{
			[PrimaryKey, Identity]         public int Id             { get; set; } // integer
			[Column, NotNull]              public int? DevTypeId     { get; set; } // integer
			[Column(Length = 50), NotNull] public string Name        { get; set; } = null!; // text
			[Column, NotNull]              public int Responsibility { get; set; } // integer

			#region Associations

			/// <summary>
			/// FK_billing.TempReading_billing.DevReadingType_DevReadingTypeId_BackReference
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "DevReadingTypeId", CanBeNull = true)]
			public IEnumerable<billing_TempReading> BillingTempReadingbillingDevReadingTypeDevReadingTypeIds { get; set; } = null!;

			/// <summary>
			/// FK_billing.DevReadingType_billing.devtypes_DevTypeId
			/// </summary>
			[Association(ThisKey = "DevTypeId", OtherKey = "Devtypeid", CanBeNull = true)]
			public billing_Devtype? DevType { get; set; }
			#endregion
		}

		[Table(Name = "billing_TempReading")]
		public partial class billing_TempReading
		{
			[Column("id"), PrimaryKey, Identity]     public int Id                  { get; set; } // integer

			[Column(Length = 50), NotNull]           public string DevSerNum        { get; set; } = null!; // text

			[Column("devid", Length = 50), Nullable] public string? Devid           { get; set; } // character varying(255)
			[Column("tsdevice"), NotNull]            public DateTime Ts             { get; set; } // timestamp (6) without time zone
			[Column("value"), NotNull]               public decimal Value           { get; set; } // numeric(18,2)
			[Column(), Nullable]                     public int? Devtypeid          { get; set; } // integer
			[Column(), Nullable]                     public int? DevReadingTypeId   { get; set; } // integer
			[Column(Length = 50), Nullable]          public string? ReadingTypeName { get; set; } // text
			[Column(), NotNull]                      public int DevGlobalType       { get; set; } // integer
			[Column(), NotNull]                      public int Responsibility      { get; set; } // integer

			#region Associations

			/// <summary>
			/// FK_billing.TempReading_billing.devices_devid
			/// </summary>
			[Association(ThisKey = "Devid", OtherKey = "Devid", CanBeNull = true)]
			public billing_Device? Dev { get; set; }

			/// <summary>
			/// FK_billing.TempReading_billing.DevReadingType_DevReadingTypeId
			/// </summary>
			[Association(ThisKey = "DevReadingTypeId", OtherKey = "Id", CanBeNull = true)]
			public billing_DevReadingType? DevReadingType { get; set; }

			#endregion
		}

		[YdbTableNotFound]
		[Test]
		public void UpdateTest([DataSources(TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllOracle, TestProvName.AllClickHouse, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<billing_Devtype>())
			using (db.CreateLocalTable<billing_Device>())
			using (db.CreateLocalTable<billing_DevReadingType>())
			using (db.CreateLocalTable<billing_TempReading>())
			{
				var query =
					from tr in db.GetTable<billing_TempReading>()
					from drt in db.GetTable<billing_DevReadingType>().InnerJoin(drt =>
						drt.Name == tr.ReadingTypeName && drt.DevTypeId == tr.Devtypeid)
					select new
					{
						tr,
						drt
					};

				query
					.Set(p => p.tr.DevReadingTypeId, p => p.drt.Id)
					.Set(p => p.tr.Responsibility, p => p.drt.Responsibility)
					.Update();

				db.GetTable<billing_TempReading>()
					.Set(p => p.DevReadingTypeId,
						u => db.GetTable<billing_DevReadingType>()
							.Where(w => w.Name == u.ReadingTypeName && w.DevTypeId == u.Devtypeid).Select(s => s.Id)
							.FirstOrDefault()
					)
					.Set(p => p.Responsibility,
						u => db.GetTable<billing_DevReadingType>()
							.Where(w => w.Name == u.ReadingTypeName && w.DevTypeId == u.Devtypeid)
							.Select(s => s.Responsibility).FirstOrDefault()
					)
					.Update();

				//					var devs = db.GetTable<billing_Device>().Join(db.GetTable<billing_Devtype>(), d => d.Devtypeid, dt => dt.Devtypeid, (d, dt) => new { d, dt });
				//
				//					db.GetTable<billing_TempReading>()
				//						.Set(t => t.Devid, u => devs.Where(w => w.d.Sernum == u.DevSerNum && w.dt.GlobalType == u.DevGlobalType).Select(s => s.d.Devid).FirstOrDefault())
				//						.Set(t => t.Devtypeid, u => devs.Where(w => w.d.Sernum == u.DevSerNum && w.dt.GlobalType == u.DevGlobalType).Select(s => s.dt.Devtypeid).FirstOrDefault())
				//						.Update();

			}
		}
	}
}
