using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[Table(Name = "EngineeringCircuitEnd")]
	public class EngineeringCircuitEndRecord
	{
		[PrimaryKey(1)]
		[Identity]         public Int64 EngineeringCircuitID { get; set; }
		[Column, Nullable] public Char? Gender               { get; set; }
	}

	public class SqlServerDataRepository : DataConnection
	{
		public SqlServerDataRepository(string configurationString) : base(configurationString)
		{
		}

		public Table<EngineeringCircuitEndRecord> EngineeringCircuitEnds { get { return this.GetTable<EngineeringCircuitEndRecord>(); } }
	}

	[TestFixture]
	public class CompareNullableChars : TestBase
	{
		[Test]
		public void Test([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var db = new SqlServerDataRepository(context))
			{
				var q =
					from current  in db.EngineeringCircuitEnds
					from previous in db.EngineeringCircuitEnds
					where current.Gender == previous.Gender
					select new { CurrentId = current.EngineeringCircuitID, PreviousId = previous.EngineeringCircuitID };

				var sql = q.ToString();
			}
		}
	}
}
