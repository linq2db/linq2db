using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public static class OracleSpecific
	{
		[Table("SEQUENCETEST")]
		public class SequenceTest
		{
			[Identity, SequenceName("SEQUENCETESTSEQ")]
			public int    ID;
			[Column("VALUE")]
			public string Value = null!;
		}

		public class StringTest
		{
			public string  KeyValue = null!;
			public string? StringValue1;
			public string? StringValue2;

			public override bool Equals(object? obj)
			{
				if (obj is not StringTest other)
					return false;

				return string.Equals(KeyValue, other.KeyValue)
				    && string.Equals(StringValue1, other.StringValue1)
				    && string.Equals(StringValue2, other.StringValue2);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(
					KeyValue,
					StringValue1,
					StringValue2
				);
			}
		}
	}
}
