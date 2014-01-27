using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public class PostgreSQLSpecific
	{
		public class SequenceTest1
		{
			[Column(IsIdentity = true), SequenceName("sequencetestseq")]
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
			[Identity, SequenceName("sequencetestseq")]
			public int    ID;
			public string Value;
		}

		[Table(Schema = "test_schema")]
		public class TestSchemaIdentity
		{
			[Column(IsIdentity = true), SequenceName("TestSchemaIdentity_ID_seq")]
			public int ID;
		}

		[Table(Schema = "test_schema", Name = "testserialidentity")]
		public class TestSerialIdentity
		{
			[Identity, PrimaryKey]
			public int ID;
		}
	}
}
