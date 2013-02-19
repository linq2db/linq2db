using System;

using LinqToDB;

namespace Tests.Model
{
	public class FirebirdSpecific
	{
		public class SequenceTest
		{
			[Column(IsIdentity = true), SequenceName("SequenceTestSeq")]
			public int    ID;

			[Column("VALUE_")] // 'Value' reserved by firebird
			public string Value;
		}
	}
}
