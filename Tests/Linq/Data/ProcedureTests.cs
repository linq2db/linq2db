using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Data
{
	using Model;
	using System.Data.SqlClient;

	[TestFixture]
	public class ProcedureTests : TestBase
	{
		public static IEnumerable<Person> PersonSelectByKey(DataConnection dataConnection, int? @id)
		{
			var databaseName = TestUtils.GetDatabaseName(dataConnection);
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
			var escapedTableName = new SqlCommandBuilder().QuoteIdentifier(databaseName);
#else
			var escapedTableName = "[" + databaseName + "]";
#endif
			return dataConnection.QueryProc<Person>(escapedTableName + "..[Person_SelectByKey]",
				new DataParameter("@id", @id));
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				var p1 = PersonSelectByKey(db, 1).First();
				var p2 = db.Query<Person>("SELECT * FROM Person WHERE PersonID = @id", new { id = 1 }).First();

				Assert.AreEqual(p1, p2);
			}
		}

		class VariableResult
		{
			public int Code      { get; set; }
			public string Value1 { get; set; }

			protected bool Equals(VariableResult other)
			{
				return Code == other.Code && string.Equals(Value1, other.Value1) && string.Equals(Value2, other.Value2);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((VariableResult)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = Code;
					hashCode = (hashCode * 397) ^ (Value1 != null ? Value1.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (Value2 != null ? Value2.GetHashCode() : 0);
					return hashCode;
				}
			}

			public string Value2 { get; set; }
		}

		[Test]
		public void VariableResultsTest([IncludeDataSources(false, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var db = new DataConnection(context))
			{
				var set1 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 0)).First();

				var set2 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 1)).First();

				var set11 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 0)).First();

				var set22 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 1)).First();

				Assert.AreEqual(2, set1.Code);
				Assert.AreEqual("v", set1.Value1);
				Assert.IsNull(set1.Value2);

				Assert.AreEqual(1, set2.Code);
				Assert.AreEqual("Val1", set2.Value1);
				Assert.AreEqual("Val2", set2.Value2);

				Assert.AreEqual(set1, set11);
				Assert.AreEqual(set2, set22);
			}
		}

	}
}
