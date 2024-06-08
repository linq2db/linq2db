using LinqToDB.Mapping;

namespace Tests.Model
{
	public static class FirebirdSpecific
	{
		public class SequenceTest
		{
			[Column(IsIdentity = true), SequenceName("SequenceTestSeq")]
			public int    ID;

			[Column("Value_")] // 'Value' reserved by firebird
			public string? Value;
		}
	}
}
