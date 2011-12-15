using System;

using LinqToDB.Data.Sql.SqlProvider;
using LinqToDB.DataAccess;

namespace Tests.Model
{
	public class OracleSpecific
	{
		public class SequenceTest
		{
			[Identity, SequenceName("SequenceTestSeq")]
			public int    ID;
			public string Value;
		}
	}
}
