using System;

using LinqToDB;

namespace Tests.Model
{
	public class OracleSpecific
	{
		public class SequenceTest
		{
			[Column(IsIdentity = true)]
			[Identity, SequenceName("SequenceTestSeq")]
			public int    ID;
			public string Value;
		}
	}
}
