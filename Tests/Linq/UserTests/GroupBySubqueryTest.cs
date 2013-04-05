using System;
using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class GroupBySubqueryTest : TestBase
	{
		[Table(Name = "EngineeringCircuitEnd")]
		public class EngineeringCircuitEndRecord
		{
			[PrimaryKey(1)]
			[Identity] public long EngineeringCircuitID { get; set; }

			[Column] public long EngineeringConnectorID     { get; set; }
			[Column] public int  EngineeringCircuitNumberID { get; set; }

			[Column, Nullable]
			public int? ServiceCircuitID { get; set; }

			[Association(ThisKey = "EngineeringConnectorID", OtherKey = "EngineeringConnectorID", CanBeNull = false)]
			public EngineeringConnectorRecord EngineeringConnectoRef { get; set; }

			[Association(ThisKey = "ServiceCircuitID", OtherKey = "ServiceCircuitID", CanBeNull = true)]
			public ServiceCircuitEndRecord ServiceCircuitRef { get; set; }

			[Association(ThisKey = "EngineeringCircuitNumberID", OtherKey = "EngineeringCircuitNumberID", CanBeNull = true)]
			public EngineeringCircuitNumberRecord EngineeringCircuitNumberRef { get; set; }
		}

		[Table(Name = "EngineeringCircuitNumber")]
		public class EngineeringCircuitNumberRecord
		{
			[Column] public int    EngineeringCircuitNumberID { get; set; }
			[Column] public string EngineeringCircuitNumber   { get; set; }
		}

		[Table(Name = "EngineeringConnector")]
		public class EngineeringConnectorRecord
		{
			[Column] public int  HarnessID   { get; set; }
			[Column] public long EngineeringConnectorID { get; set; }

			[AssociationAttribute(ThisKey = "HarnessID", OtherKey = "HarnessID", CanBeNull = false)]
			public HarnessRecord HarnessRef { get; set; }
		}

		[Table(Name = "Harness")]
		public class HarnessRecord
		{
			[Column] public int HarnessID  { get; set; }
			[Column] public int RevisionID { get; set; }
		}

		[Table(Name = "ServiceCircuitEnd")]
		public class ServiceCircuitEndRecord
		{
			[Nullable]
			[Column] public int? ServiceCircuitID  { get; set; }
			[Column] public int  ServiceFunctionID { get; set; }

			[Association(ThisKey = "ServiceFunctionID", OtherKey = "ServiceFunctionID", CanBeNull = true)]
			public ServiceFunctionNameRecord ServiceFunctionRef { get; set; }
		}

		[Table(Name = "ServiceFunctionNames")]
		public class ServiceFunctionNameRecord
		{
			[Column] public int    ServiceFunctionID    { get; set; }
			[Column] public string ServiceFunctionNames { get; set; }
		}

		[Test]
		public void Test()
		{
			using (var db = new TestDataConnection())
			{
				var q = (
					from engineeringCircuitEnd in db.GetTable<EngineeringCircuitEndRecord>()
					where engineeringCircuitEnd.ServiceCircuitID != null
					select new
					{
						RevisionId = engineeringCircuitEnd.EngineeringConnectoRef.HarnessRef.RevisionID,
						engineeringCircuitEnd.EngineeringCircuitNumberRef.EngineeringCircuitNumber,
						ServiceFunction = engineeringCircuitEnd.ServiceCircuitRef.ServiceFunctionRef.ServiceFunctionNames ?? string.Empty
					}
				).Distinct();

				var sql1 = q.ToString();

				var q2 =
					from t3 in q
					group t3 by new { t3.RevisionId, t3.EngineeringCircuitNumber }
					into g
					where g.Count() > 1
					select new { g.Key.RevisionId, g.Key.EngineeringCircuitNumber, Count = g.Count() };

				var sql2 = q2.ToString();

				var idx = sql2.IndexOf("DISTINCT");

				Assert.That(idx, Is.GreaterThanOrEqualTo(0));

				idx = sql2.IndexOf("ServiceFunctionNames", idx);

				Assert.That(idx, Is.GreaterThanOrEqualTo(0));
			}
		}
	}
}
