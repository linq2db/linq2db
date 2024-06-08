using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1964Tests : TestBase
	{
		public class BranchSelectOutput
		{
			public int    BranchId      { get; set; }
			public string BranchName    { get; set; } = null!;
			public string BranchAddress { get; set; } = null!;
			public string BusinessHours { get; set; } = null!;
			public double Distance      { get; set; }
			public string BranchPic     { get; set; } = null!;

			public decimal PointX { get; set; }
			public decimal PointY { get; set; }
		}

		[Table(Schema="dbo", Name="Attachment")]
		public partial class AttachmentEntity
		{
			[PrimaryKey, Identity] public int      Id          { get; set; } // int
			[Column,     NotNull ] public int      Type        { get; set; } // int
			[Column,     NotNull ] public int      ItemId      { get; set; } // int
			[Column,     NotNull ] public string   Name        { get; set; } = null!; // nvarchar(128)
			[Column,     NotNull ] public string   ContentType { get; set; } = null!; // nvarchar(64)
			[Column,     NotNull ] public string   Url         { get; set; } = null!; // nvarchar(128)
			[Column,     NotNull ] public int      Status      { get; set; } // int
			[Column,     NotNull ] public DateTime CreateTime  { get; set; } // datetime
		}

		[Table(Schema="dbo", Name="BranchInfo")]
		public partial class BranchInfoEntity
		{
			[PrimaryKey, Identity] public int      BranchId         { get; set; } // int
			[Column,     NotNull ] public string   BranchCode       { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   BranchName       { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   BranchParentCode { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   CompanyCode      { get; set; } = null!; // varchar(32)
			[Column,     NotNull ] public string   AreaCode         { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   BranchAddress    { get; set; } = null!; // varchar(100)
			[Column,     NotNull ] public string   BrandContacts    { get; set; } = null!; // varchar(20)
			[Column,     NotNull ] public string   BrandPhone       { get; set; } = null!; // varchar(20)
			[Column,     NotNull ] public int      Status           { get; set; } // int
			[Column,     NotNull ] public DateTime CreateTime       { get; set; } // datetime
			[Column,     NotNull ] public string   CreateUser       { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public DateTime LastUpdateTime   { get; set; } // datetime
			[Column,     NotNull ] public string   LastUpdateUser   { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   BusinessHours    { get; set; } = null!; // varchar(100)
			[Column,     NotNull ] public decimal  PointX           { get; set; } // decimal(18, 4)
			[Column,     NotNull ] public decimal  PointY           { get; set; } // decimal(18, 4)
			[Column,     NotNull ] public int      BranchIsShow     { get; set; } // int
			[Column,     NotNull ] public int      BranchType       { get; set; } // int
		}

		private IQueryable<BranchInfoEntity> NotDeletedBranchInfo(IDataContext dc)
		{
			return dc.GetTable<BranchInfoEntity>().Where(m => m.Status == 0); 
		}

		[Test]
		public void SelectManyLetJoinTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<AttachmentEntity>())
			using (db.CreateLocalTable<BranchInfoEntity>())
			{
				var attachmentQry = from att in (from temp in db.GetTable<AttachmentEntity>()
						where temp.Status == 0 && temp.Type == 2
						select new
						{
							temp.ItemId,
							temp.Url,
							Index = Sql.Ext.RowNumber().Over().PartitionBy(temp.ItemId).OrderBy(temp.CreateTime)
								.ToValue()
						})
					where att.Index == 1
					select att;

				var qry = 
					from branchInfo in NotDeletedBranchInfo(db)
					from att in attachmentQry.LeftJoin(m => m.ItemId == branchInfo.BranchId)
					where branchInfo.BranchIsShow == 0
					select new BranchSelectOutput
					{
						BranchAddress = branchInfo.BranchAddress,
						BranchId = branchInfo.BranchId,
						BranchName = branchInfo.BranchName,
						BusinessHours = branchInfo.BusinessHours,
						PointX = branchInfo.PointX,
						PointY = branchInfo.PointY,
						BranchPic = att.Url
					};

				Assert.DoesNotThrow(() => qry.ToList());
			}
		}
	}
}
