using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class UnnecessaryInnerJoinTest : TestBase
	{
		[Table(Name = "EngineeringCircuitEnd")]
		public class EngineeringCircuitEndRecord
		{
			[PrimaryKey(1)]
			[Identity]
			public Int64 EngineeringCircuitID { get; set; }

			[Column]
			public Int64 EngineeringConnectorID { get; set; }
		}

		[Table(Name = "EngineeringConnector")]
		public class EngineeringConnectorRecord
		{
			[Association(ThisKey = "EngineeringConnectorID", OtherKey = "EngineeringConnectorID", CanBeNull = false)]
			public List<EngineeringCircuitEndRecord> EngineeringCircuits { get; set; }

			[PrimaryKey(1)]
			[Identity]
			public Int64 EngineeringConnectorID { get; set; }
		}

		[Test]
		public void Test([DataContextsAttribute(ExcludeLinqService=true)] string context)
		{
			var ids = new long[] { 1, 2, 3 };

			using (var db = new DataConnection(context))
			{
				var q =
					from engineeringConnector in db.GetTable<EngineeringConnectorRecord>()
					where engineeringConnector.EngineeringCircuits.Any(x => ids.Contains(x.EngineeringCircuitID))
					select new { engineeringConnector.EngineeringConnectorID };

				var sql = q.ToString();

				Assert.That(sql.IndexOf("INNER JOIN"), Is.LessThan(0));
			}
		}
	}
}
