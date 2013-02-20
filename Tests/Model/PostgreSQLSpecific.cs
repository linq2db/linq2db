using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public class PostgreSQLSpecific
	{
		public class SequenceTest1
		{
			[Column(IsIdentity = true), SequenceName("SequenceTestSeq")]
			public int    ID;
			public string Value;
		}

		public class SequenceTest2
		{
			[Column(IsIdentity = true)]
			public int    ID;
			public string Value;
		}

		public class SequenceTest3
		{
			[Identity, SequenceName("SequenceTestSeq")]
			public int    ID;
			public string Value;
		}
	}
}
