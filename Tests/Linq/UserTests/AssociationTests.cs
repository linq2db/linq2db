using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class AssociationTests : TestBase
	{
		enum DisTypeCode
		{
			[MapValue(1)] Value1 = 1,
			[MapValue(2)] Value2 = 2,
			[MapValue(4)] Value3 = 3
		}

		[Table]
		class DisTable
		{
			[Column, NotNull] public DisTypeCode DisTypeID { get; set; }

			[Association(ThisKey = nameof(DisTypeID), OtherKey = nameof(DisTypeTable .DisTypeID), CanBeNull = false)]
			public DisTypeTable DisType { get; set; } = null!;
		}

		[Table]
		class DisTypeTable
		{
			[Column, PrimaryKey, NotNull]  public int    DisTypeID { get; set; }
			[Column(Length = 50), NotNull] public string JurCode   { get; set; } = null!;
		}

		[Table]
		class JurTable
		{
			[Column(Length = 2), PrimaryKey, NotNull]  public string JurCode { get; set; } = null!;
		}

		[Test]
		public void Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var sql =
			(
				from d in db.GetTable<DisTable>()
				join j in db.GetTable<JurTable>() on d.DisType.JurCode equals j.JurCode
				select d
			)
			.ToString();

			TestContext.WriteLine(sql);
		}
	}
}
