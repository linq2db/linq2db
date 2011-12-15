using System;

using LinqToDB;
using LinqToDB.Data.Sql.SqlProvider;

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
