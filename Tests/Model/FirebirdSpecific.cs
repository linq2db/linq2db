using System;

using LinqToDB;
using LinqToDB.SqlProvider;

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
