using System;
using System.Data.Linq;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public class LinqDataTypes : IEquatable<LinqDataTypes>, IComparable
	{
		public int      ID;
		public decimal  MoneyValue;
		public DateTime DateTimeValue;
		public bool     BoolValue;
		public Guid     GuidValue;
		public Binary   BinaryValue;
		public short    SmallIntValue;
		public string   StringValue;

		public override bool Equals(object obj)
		{
			return Equals(obj as LinqDataTypes);
		}

		public bool Equals(LinqDataTypes other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return
				other.ID                   == ID                   &&
				other.MoneyValue           == MoneyValue           &&
				other.BoolValue            == BoolValue            &&
				other.GuidValue            == GuidValue            &&
				other.SmallIntValue        == SmallIntValue        &&
				other.DateTimeValue.Date   == DateTimeValue.Date   &&
				other.DateTimeValue.Hour   == DateTimeValue.Hour   &&
				other.DateTimeValue.Minute == DateTimeValue.Minute &&
				other.DateTimeValue.Second == DateTimeValue.Second &&
				(
					other.StringValue                            == StringValue ||
					string.IsNullOrWhiteSpace(other.StringValue) == string.IsNullOrWhiteSpace(StringValue)
				)
				;
		}

		public override int GetHashCode()
		{
			return ID;
		}

		public int CompareTo(object obj)
		{
			return ID - ((LinqDataTypes)obj).ID;
		}

		public static bool operator == (LinqDataTypes left, LinqDataTypes right)
		{
			return Equals(left, right);
		}

		public static bool operator != (LinqDataTypes left, LinqDataTypes right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			return string.Format("{{{0,2}, {1,7}, {2:O}, {3,5}, {4}, {5}, '{6}'}}", ID, MoneyValue, DateTimeValue, BoolValue, GuidValue, SmallIntValue, StringValue);
		}
	}

	[Table("LinqDataTypes")]
	public class LinqDataTypes2 : IEquatable<LinqDataTypes2>, IComparable
	{
		[PrimaryKey]                                    public int       ID;
		[Column]                                        public decimal   MoneyValue;
		// type it explicitly, because SQL Server 2005+ provider maps DateTime .Net type DataType.DateTime2 by default
		[Column(DataType = LinqToDB.DataType.DateTime)] public DateTime? DateTimeValue;
		[Column]                                        public DateTime? DateTimeValue2;
		[Column]                                        public bool?     BoolValue;
		[Column]                                        public Guid?     GuidValue;
		[Column]                                        public short?    SmallIntValue;
		[Column]                                        public int?      IntValue;
		[Column]                                        public long?     BigIntValue;
		[Column]                                        public string    StringValue;

		public override bool Equals(object obj)
		{
			return Equals(obj as LinqDataTypes2);
		}

		public bool Equals(LinqDataTypes2 other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return
				other.ID                     == ID                     &&
				other.MoneyValue             == MoneyValue             &&
				other.BoolValue              == BoolValue              &&
				other.GuidValue              == GuidValue              &&
				other.StringValue            == StringValue            &&
				other.DateTimeValue.HasValue == DateTimeValue.HasValue &&
				(other.DateTimeValue == null ||
				(
					other.DateTimeValue.Value.Date   == DateTimeValue.Value.Date   &&
					other.DateTimeValue.Value.Hour   == DateTimeValue.Value.Hour   &&
					other.DateTimeValue.Value.Minute == DateTimeValue.Value.Minute &&
					other.DateTimeValue.Value.Second == DateTimeValue.Value.Second
				));
		}

		public override int GetHashCode()
		{
			return ID;
		}

		public int CompareTo(object obj)
		{
			return ID - ((LinqDataTypes2)obj).ID;
		}

		public static bool operator ==(LinqDataTypes2 left, LinqDataTypes2 right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(LinqDataTypes2 left, LinqDataTypes2 right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			return string.Format("{{{0,2}, {1,7}, {2:O}, {3,5}, {4}, {5}, '{6}'}}", ID, MoneyValue, DateTimeValue, BoolValue, GuidValue, SmallIntValue, StringValue);
		}
	}
}
