using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

	[TestFixture]
	public class Issue1222Tests : TestBase
	{
		[Table("stLinks")]
		public class StLink
		{
			[Column("inId"), PrimaryKey, Identity] public int       InId          { get; set; } // int
			[Column("inIdParent"),    NotNull]     public int       InIdParent    { get; set; } // int
			[Column("inIdChild"),     NotNull]     public int       InIdChild     { get; set; } // int
			[Column("inIdTypeRel"),   NotNull]     public int       InIdTypeRel   { get; set; } // int
			[Column("inMaxQuantity"),    Nullable] public double?   InMaxQuantity { get; set; } // float
			[Column("inMinQuantity"),    Nullable] public double?   InMinQuantity { get; set; } // float
			[Column("inIdMeasure"),      Nullable] public int?      InIdMeasure   { get; set; } // int
			[Column("inIdUnit"),         Nullable] public int?      InIdUnit      { get; set; } // int
			[Column,                     Nullable] public int?      State         { get; set; } // int
			[Column("dtModified"),    NotNull]     public DateTime  DtModified    { get; set; } // datetime
			[Column("inIdOrgOwner"),     Nullable] public int?      InIdOrgOwner  { get; set; } // int
			[Column("dtSynchDate"),      Nullable] public DateTime? DtSynchDate   { get; set; } // datetime
			[Column("stGUID"),        NotNull]     public string    StGUID        { get; set; } // varchar(255)

			[Association(ThisKey = "InIdTypeRel", OtherKey = "InId", CanBeNull = false, Relationship = Relationship.ManyToOne, KeyName = "FK_stLinks_inIdTypeRel_rlTypesAndTypes")]
			public RlTypesAndType RlTypesAndType { get; set; }
		}

		[Table("rlTypesAndTypes")]
		public class RlTypesAndType
		{
			[Column("inId"), PrimaryKey, Identity] public int InId         { get; set; } // int
			[Column("inIdLinkType"), NotNull]      public int InIdLinkType { get; set; } // int
		}

		[Table("stVersions")]
		public class StVersion
		{
			[Column("inId"), PrimaryKey, Identity] public int InId     { get; set; } // int
			[Column("inIdMain"), NotNull]          public int InIdMain { get; set; } // int
		}

		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<StLink>())
				using (db.CreateLocalTable<StVersion>())
				{
					var parentId = 111;

					var query =
						from u in Queryable.Concat(
							from link in db.GetTable<StLink>()
							where link.InIdParent == parentId
							select new { VersionId = link.InIdChild, Link = link },

							from link in db.GetTable<StLink>()
							where link.InIdChild == parentId
							select new { VersionId = link.InIdParent, Link = link })

						join version in db.GetTable<StVersion>() on u.VersionId equals version.InId
						select new
						{
							//LinkTypeId = u.Link.RlTypesAndType.InIdLinkType,
							//LinkId = u.Link.InId,
							MainId = version.InIdMain
						};

					var _ = query.ToList();
				}
			}
		}
	}
}
