using System;

using LinqToDB.Data.Sql.SqlProvider;
using LinqToDB.DataAccess;
using LinqToDB.Mapping;

namespace Tests.Model
{
	public class FirebirdSpecific
	{
		public class SequenceTest
		{
			[Identity, SequenceName("SequenceTestSeq")]
			public int    ID;

			[MapField("VALUE_")] // 'Value' reserved by firebird
			public string Value;
		}
	}
}
