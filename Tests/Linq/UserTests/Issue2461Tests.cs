using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2461Tests : TestBase
	{
		[LinqToDB.Mapping.Table("MRECEIPT")]
		public class TestReceipt
		{
			[LinqToDB.Mapping.PrimaryKey] public int Id { get; set; }

			public static string TableName => "MRECEIPT";
			public static string ExternalReceiptsTableName => "EXTERNAL_RECEIPTS";

			[LinqToDB.Mapping.Column("RECEIPT_NO")]            public string           ReceiptNo            { get; set; } = null!;
			[LinqToDB.Mapping.Column("CUSTKEY")]               public string           Custkey              { get; set; } = null!;

			[LinqToDB.Mapping.Association(ThisKey=nameof(Custkey), OtherKey=nameof(TestCustomer.Custkey), CanBeNull=true)]
			public TestCustomer Customer { get; } = null!;
		}

		[LinqToDB.Mapping.Table("CUST_DTL")]
		public class TestCustomer
		{
			[LinqToDB.Mapping.PrimaryKey] public int Id { get; set; }

			[LinqToDB.Mapping.Column("CUSTKEY")]
			public string Custkey { get; set;} = null!;

			[LinqToDB.Mapping.Column("BILLGROUP")] public string BillingGroup { get; set; } = null!;

			public static string GetName(string name) => name;
		}

		[Test]
		public void AssociationConcat([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TestReceipt>())
			using (db.CreateLocalTable<TestReceipt>(TestReceipt.ExternalReceiptsTableName))
			using (db.CreateLocalTable<TestCustomer>())
			{
				var query = db.GetTable<TestReceipt>()
					.Concat(db.GetTable<TestReceipt>().TableName(TestReceipt.ExternalReceiptsTableName))
					.Select(
						i =>
							new { i.ReceiptNo, a = TestCustomer.GetName(i.Customer.BillingGroup) });

				var act = () => query.ToArray();
				act.ShouldNotThrow();
			}
		}
	}
}
