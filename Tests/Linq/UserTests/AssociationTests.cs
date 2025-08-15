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
			[MapValue(4)] Value3 = 3,
			[MapValue(5)] Value5 = 5,
		}

		[Table]
		class DisTable
		{
			[Column, PrimaryKey, NotNull] public DisTypeCode DisTypeID { get; set; }

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
			using var t1 = db.CreateLocalTable<DisTable>();
			using var t2 = db.CreateLocalTable<JurTable>();
			using var t3 = db.CreateLocalTable<DisTypeTable>();

			var q =
				(
					from d in t1
					join j in t2 on d.DisType.JurCode equals j.JurCode
					select d
				)
				.ToList();
		}
	}
}
