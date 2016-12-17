using System;

using LinqToDB.Mapping;

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

		public class StringTest
		{
			public string KeyValue;
			public string StringValue1;
			public string StringValue2;

			public override bool Equals(object obj)
			{
				var other = obj as StringTest;
				if (other == null)
					return false;

				return    string.Equals(KeyValue, other.KeyValue)
				       && string.Equals(StringValue1, other.StringValue1)
				       && string.Equals(StringValue2, other.StringValue2);
			}

			public override int GetHashCode()
			{
				return string.Format("{0}{1}{2}", KeyValue, StringValue1, StringValue2).GetHashCode();
			}
		}
	}
}
