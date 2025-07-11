using System;
using System.Collections;
using System.Collections.Generic;

using LinqToDB.Internal.Common;
using LinqToDB.SqlQuery;

namespace LinqToDB;

partial class Sql
{
	public static partial class SqlRow
	{
		public const int MaxMemberCount = 10;

		public static readonly Type[] Types = new []
		{
				typeof(SqlRow<>),
				typeof(SqlRow<,>),
				typeof(SqlRow<,,>),
				typeof(SqlRow<,,,>),
				typeof(SqlRow<,,,,>),
				typeof(SqlRow<,,,,,>),
				typeof(SqlRow<,,,,,,>),
				typeof(SqlRow<,,,,,,,>),
				typeof(SqlRow<,,,,,,,,>),
				typeof(SqlRow<,,,,,,,,,>),
		};

		internal static int CombineHashCodes(int h1, int h2)
			=> (((h1 << 5) + h1) ^ h2);

		internal static int CombineHashCodes(int h1, int h2, int h3)
			=> CombineHashCodes(CombineHashCodes(h1, h2), h3);

		internal static int CombineHashCodes(int h1, int h2, int h3, int h4)
			=> CombineHashCodes(CombineHashCodes(h1, h2), CombineHashCodes(h3, h4));

		internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5)
			=> CombineHashCodes(CombineHashCodes(h1, h2, h3), CombineHashCodes(h4, h5));

		internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6)
			=> CombineHashCodes(CombineHashCodes(h1, h2, h3), CombineHashCodes(h4, h5, h6));

		internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7)
			=> CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6, h7));

		internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8)
			=> CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6, h7, h8));

		internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8, int h9)
			=> CombineHashCodes(CombineHashCodes(h1, h2, h3, h4, h5), CombineHashCodes(h6, h7, h8, h9));

		internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8, int h9, int h10)
			=> CombineHashCodes(CombineHashCodes(h1, h2, h3, h4, h5), CombineHashCodes(h6, h7, h8, h9, h10));

	}

	public class SqlRow<T1>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1)
		{
			Value1 = value1;
		}

		public T1 Value1 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1);
		}

		public override int GetHashCode()
			=> Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1);

		public static bool operator > (SqlRow<T1> x, SqlRow<T1> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1> x, SqlRow<T1> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1> x, SqlRow<T1> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1> x, SqlRow<T1> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2)
		{
			Value1 = value1;
			Value2 = value2;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2));

		public static bool operator > (SqlRow<T1, T2> x, SqlRow<T1, T2> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2> x, SqlRow<T1, T2> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2> x, SqlRow<T1, T2> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2> x, SqlRow<T1, T2> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2, T3>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2, T3 value3)
		{
			Value1 = value1;
			Value2 = value2;
			Value3 = value3;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }
		public T3 Value3 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);
		static IEqualityComparer<T3> _value3ValueComparer = ValueComparer.GetDefaultValueComparer<T3>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2, T3> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			if (c != 0) return c;

			c = comparer.Compare(Value3, otherRow.Value3);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2, T3> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2)
				&& _value3ValueComparer.Equals(Value3, otherRow.Value3);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2),
				Value3 is null ? 0 : _value3ValueComparer.GetHashCode(Value3));

		public static bool operator > (SqlRow<T1, T2, T3> x, SqlRow<T1, T2, T3> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2, T3> x, SqlRow<T1, T2, T3> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2, T3> x, SqlRow<T1, T2, T3> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2, T3> x, SqlRow<T1, T2, T3> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2, T3, T4>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2, T3 value3, T4 value4)
		{
			Value1 = value1;
			Value2 = value2;
			Value3 = value3;
			Value4 = value4;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }
		public T3 Value3 { get; }
		public T4 Value4 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);
		static IEqualityComparer<T3> _value3ValueComparer = ValueComparer.GetDefaultValueComparer<T3>(true);
		static IEqualityComparer<T4> _value4ValueComparer = ValueComparer.GetDefaultValueComparer<T4>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2, T3, T4> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			if (c != 0) return c;

			c = comparer.Compare(Value3, otherRow.Value3);
			if (c != 0) return c;

			c = comparer.Compare(Value4, otherRow.Value4);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2, T3, T4> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2)
				&& _value3ValueComparer.Equals(Value3, otherRow.Value3)
				&& _value4ValueComparer.Equals(Value4, otherRow.Value4);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2),
				Value3 is null ? 0 : _value3ValueComparer.GetHashCode(Value3),
				Value4 is null ? 0 : _value4ValueComparer.GetHashCode(Value4));

		public static bool operator > (SqlRow<T1, T2, T3, T4> x, SqlRow<T1, T2, T3, T4> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2, T3, T4> x, SqlRow<T1, T2, T3, T4> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2, T3, T4> x, SqlRow<T1, T2, T3, T4> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2, T3, T4> x, SqlRow<T1, T2, T3, T4> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2, T3, T4, T5>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
		{
			Value1 = value1;
			Value2 = value2;
			Value3 = value3;
			Value4 = value4;
			Value5 = value5;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }
		public T3 Value3 { get; }
		public T4 Value4 { get; }
		public T5 Value5 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);
		static IEqualityComparer<T3> _value3ValueComparer = ValueComparer.GetDefaultValueComparer<T3>(true);
		static IEqualityComparer<T4> _value4ValueComparer = ValueComparer.GetDefaultValueComparer<T4>(true);
		static IEqualityComparer<T5> _value5ValueComparer = ValueComparer.GetDefaultValueComparer<T5>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2, T3, T4, T5> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			if (c != 0) return c;

			c = comparer.Compare(Value3, otherRow.Value3);
			if (c != 0) return c;

			c = comparer.Compare(Value4, otherRow.Value4);
			if (c != 0) return c;

			c = comparer.Compare(Value5, otherRow.Value5);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2, T3, T4, T5> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2)
				&& _value3ValueComparer.Equals(Value3, otherRow.Value3)
				&& _value4ValueComparer.Equals(Value4, otherRow.Value4)
				&& _value5ValueComparer.Equals(Value5, otherRow.Value5);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2),
				Value3 is null ? 0 : _value3ValueComparer.GetHashCode(Value3),
				Value4 is null ? 0 : _value4ValueComparer.GetHashCode(Value4),
				Value5 is null ? 0 : _value5ValueComparer.GetHashCode(Value5));

		public static bool operator > (SqlRow<T1, T2, T3, T4, T5> x, SqlRow<T1, T2, T3, T4, T5> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2, T3, T4, T5> x, SqlRow<T1, T2, T3, T4, T5> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2, T3, T4, T5> x, SqlRow<T1, T2, T3, T4, T5> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2, T3, T4, T5> x, SqlRow<T1, T2, T3, T4, T5> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2, T3, T4, T5, T6>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
		{
			Value1 = value1;
			Value2 = value2;
			Value3 = value3;
			Value4 = value4;
			Value5 = value5;
			Value6 = value6;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }
		public T3 Value3 { get; }
		public T4 Value4 { get; }
		public T5 Value5 { get; }
		public T6 Value6 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);
		static IEqualityComparer<T3> _value3ValueComparer = ValueComparer.GetDefaultValueComparer<T3>(true);
		static IEqualityComparer<T4> _value4ValueComparer = ValueComparer.GetDefaultValueComparer<T4>(true);
		static IEqualityComparer<T5> _value5ValueComparer = ValueComparer.GetDefaultValueComparer<T5>(true);
		static IEqualityComparer<T6> _value6ValueComparer = ValueComparer.GetDefaultValueComparer<T6>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2, T3, T4, T5, T6> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			if (c != 0) return c;

			c = comparer.Compare(Value3, otherRow.Value3);
			if (c != 0) return c;

			c = comparer.Compare(Value4, otherRow.Value4);
			if (c != 0) return c;

			c = comparer.Compare(Value5, otherRow.Value5);
			if (c != 0) return c;

			c = comparer.Compare(Value6, otherRow.Value6);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2, T3, T4, T5, T6> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2)
				&& _value3ValueComparer.Equals(Value3, otherRow.Value3)
				&& _value4ValueComparer.Equals(Value4, otherRow.Value4)
				&& _value5ValueComparer.Equals(Value5, otherRow.Value5)
				&& _value6ValueComparer.Equals(Value6, otherRow.Value6);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2),
				Value3 is null ? 0 : _value3ValueComparer.GetHashCode(Value3),
				Value4 is null ? 0 : _value4ValueComparer.GetHashCode(Value4),
				Value5 is null ? 0 : _value5ValueComparer.GetHashCode(Value5),
				Value6 is null ? 0 : _value6ValueComparer.GetHashCode(Value6));

		public static bool operator > (SqlRow<T1, T2, T3, T4, T5, T6> x, SqlRow<T1, T2, T3, T4, T5, T6> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2, T3, T4, T5, T6> x, SqlRow<T1, T2, T3, T4, T5, T6> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2, T3, T4, T5, T6> x, SqlRow<T1, T2, T3, T4, T5, T6> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2, T3, T4, T5, T6> x, SqlRow<T1, T2, T3, T4, T5, T6> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2, T3, T4, T5, T6, T7>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
		{
			Value1 = value1;
			Value2 = value2;
			Value3 = value3;
			Value4 = value4;
			Value5 = value5;
			Value6 = value6;
			Value7 = value7;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }
		public T3 Value3 { get; }
		public T4 Value4 { get; }
		public T5 Value5 { get; }
		public T6 Value6 { get; }
		public T7 Value7 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);
		static IEqualityComparer<T3> _value3ValueComparer = ValueComparer.GetDefaultValueComparer<T3>(true);
		static IEqualityComparer<T4> _value4ValueComparer = ValueComparer.GetDefaultValueComparer<T4>(true);
		static IEqualityComparer<T5> _value5ValueComparer = ValueComparer.GetDefaultValueComparer<T5>(true);
		static IEqualityComparer<T6> _value6ValueComparer = ValueComparer.GetDefaultValueComparer<T6>(true);
		static IEqualityComparer<T7> _value7ValueComparer = ValueComparer.GetDefaultValueComparer<T7>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2, T3, T4, T5, T6, T7> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			if (c != 0) return c;

			c = comparer.Compare(Value3, otherRow.Value3);
			if (c != 0) return c;

			c = comparer.Compare(Value4, otherRow.Value4);
			if (c != 0) return c;

			c = comparer.Compare(Value5, otherRow.Value5);
			if (c != 0) return c;

			c = comparer.Compare(Value6, otherRow.Value6);
			if (c != 0) return c;

			c = comparer.Compare(Value7, otherRow.Value7);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2, T3, T4, T5, T6, T7> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2)
				&& _value3ValueComparer.Equals(Value3, otherRow.Value3)
				&& _value4ValueComparer.Equals(Value4, otherRow.Value4)
				&& _value5ValueComparer.Equals(Value5, otherRow.Value5)
				&& _value6ValueComparer.Equals(Value6, otherRow.Value6)
				&& _value7ValueComparer.Equals(Value7, otherRow.Value7);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2),
				Value3 is null ? 0 : _value3ValueComparer.GetHashCode(Value3),
				Value4 is null ? 0 : _value4ValueComparer.GetHashCode(Value4),
				Value5 is null ? 0 : _value5ValueComparer.GetHashCode(Value5),
				Value6 is null ? 0 : _value6ValueComparer.GetHashCode(Value6),
				Value7 is null ? 0 : _value7ValueComparer.GetHashCode(Value7));

		public static bool operator > (SqlRow<T1, T2, T3, T4, T5, T6, T7> x, SqlRow<T1, T2, T3, T4, T5, T6, T7> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2, T3, T4, T5, T6, T7> x, SqlRow<T1, T2, T3, T4, T5, T6, T7> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2, T3, T4, T5, T6, T7> x, SqlRow<T1, T2, T3, T4, T5, T6, T7> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2, T3, T4, T5, T6, T7> x, SqlRow<T1, T2, T3, T4, T5, T6, T7> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2, T3, T4, T5, T6, T7, T8>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
		{
			Value1 = value1;
			Value2 = value2;
			Value3 = value3;
			Value4 = value4;
			Value5 = value5;
			Value6 = value6;
			Value7 = value7;
			Value8 = value8;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }
		public T3 Value3 { get; }
		public T4 Value4 { get; }
		public T5 Value5 { get; }
		public T6 Value6 { get; }
		public T7 Value7 { get; }
		public T8 Value8 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);
		static IEqualityComparer<T3> _value3ValueComparer = ValueComparer.GetDefaultValueComparer<T3>(true);
		static IEqualityComparer<T4> _value4ValueComparer = ValueComparer.GetDefaultValueComparer<T4>(true);
		static IEqualityComparer<T5> _value5ValueComparer = ValueComparer.GetDefaultValueComparer<T5>(true);
		static IEqualityComparer<T6> _value6ValueComparer = ValueComparer.GetDefaultValueComparer<T6>(true);
		static IEqualityComparer<T7> _value7ValueComparer = ValueComparer.GetDefaultValueComparer<T7>(true);
		static IEqualityComparer<T8> _value8ValueComparer = ValueComparer.GetDefaultValueComparer<T8>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			if (c != 0) return c;

			c = comparer.Compare(Value3, otherRow.Value3);
			if (c != 0) return c;

			c = comparer.Compare(Value4, otherRow.Value4);
			if (c != 0) return c;

			c = comparer.Compare(Value5, otherRow.Value5);
			if (c != 0) return c;

			c = comparer.Compare(Value6, otherRow.Value6);
			if (c != 0) return c;

			c = comparer.Compare(Value7, otherRow.Value7);
			if (c != 0) return c;

			c = comparer.Compare(Value8, otherRow.Value8);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2)
				&& _value3ValueComparer.Equals(Value3, otherRow.Value3)
				&& _value4ValueComparer.Equals(Value4, otherRow.Value4)
				&& _value5ValueComparer.Equals(Value5, otherRow.Value5)
				&& _value6ValueComparer.Equals(Value6, otherRow.Value6)
				&& _value7ValueComparer.Equals(Value7, otherRow.Value7)
				&& _value8ValueComparer.Equals(Value8, otherRow.Value8);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2),
				Value3 is null ? 0 : _value3ValueComparer.GetHashCode(Value3),
				Value4 is null ? 0 : _value4ValueComparer.GetHashCode(Value4),
				Value5 is null ? 0 : _value5ValueComparer.GetHashCode(Value5),
				Value6 is null ? 0 : _value6ValueComparer.GetHashCode(Value6),
				Value7 is null ? 0 : _value7ValueComparer.GetHashCode(Value7),
				Value8 is null ? 0 : _value8ValueComparer.GetHashCode(Value8));

		public static bool operator > (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)
		{
			Value1 = value1;
			Value2 = value2;
			Value3 = value3;
			Value4 = value4;
			Value5 = value5;
			Value6 = value6;
			Value7 = value7;
			Value8 = value8;
			Value9 = value9;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }
		public T3 Value3 { get; }
		public T4 Value4 { get; }
		public T5 Value5 { get; }
		public T6 Value6 { get; }
		public T7 Value7 { get; }
		public T8 Value8 { get; }
		public T9 Value9 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);
		static IEqualityComparer<T3> _value3ValueComparer = ValueComparer.GetDefaultValueComparer<T3>(true);
		static IEqualityComparer<T4> _value4ValueComparer = ValueComparer.GetDefaultValueComparer<T4>(true);
		static IEqualityComparer<T5> _value5ValueComparer = ValueComparer.GetDefaultValueComparer<T5>(true);
		static IEqualityComparer<T6> _value6ValueComparer = ValueComparer.GetDefaultValueComparer<T6>(true);
		static IEqualityComparer<T7> _value7ValueComparer = ValueComparer.GetDefaultValueComparer<T7>(true);
		static IEqualityComparer<T8> _value8ValueComparer = ValueComparer.GetDefaultValueComparer<T8>(true);
		static IEqualityComparer<T9> _value9ValueComparer = ValueComparer.GetDefaultValueComparer<T9>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			if (c != 0) return c;

			c = comparer.Compare(Value3, otherRow.Value3);
			if (c != 0) return c;

			c = comparer.Compare(Value4, otherRow.Value4);
			if (c != 0) return c;

			c = comparer.Compare(Value5, otherRow.Value5);
			if (c != 0) return c;

			c = comparer.Compare(Value6, otherRow.Value6);
			if (c != 0) return c;

			c = comparer.Compare(Value7, otherRow.Value7);
			if (c != 0) return c;

			c = comparer.Compare(Value8, otherRow.Value8);
			if (c != 0) return c;

			c = comparer.Compare(Value9, otherRow.Value9);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2)
				&& _value3ValueComparer.Equals(Value3, otherRow.Value3)
				&& _value4ValueComparer.Equals(Value4, otherRow.Value4)
				&& _value5ValueComparer.Equals(Value5, otherRow.Value5)
				&& _value6ValueComparer.Equals(Value6, otherRow.Value6)
				&& _value7ValueComparer.Equals(Value7, otherRow.Value7)
				&& _value8ValueComparer.Equals(Value8, otherRow.Value8)
				&& _value9ValueComparer.Equals(Value9, otherRow.Value9);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2),
				Value3 is null ? 0 : _value3ValueComparer.GetHashCode(Value3),
				Value4 is null ? 0 : _value4ValueComparer.GetHashCode(Value4),
				Value5 is null ? 0 : _value5ValueComparer.GetHashCode(Value5),
				Value6 is null ? 0 : _value6ValueComparer.GetHashCode(Value6),
				Value7 is null ? 0 : _value7ValueComparer.GetHashCode(Value7),
				Value8 is null ? 0 : _value8ValueComparer.GetHashCode(Value8),
				Value9 is null ? 0 : _value9ValueComparer.GetHashCode(Value9));

		public static bool operator > (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	public class SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>: IComparable, IStructuralComparable
	{
		public SqlRow(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)
		{
			Value1 = value1;
			Value2 = value2;
			Value3 = value3;
			Value4 = value4;
			Value5 = value5;
			Value6 = value6;
			Value7 = value7;
			Value8 = value8;
			Value9 = value9;
			Value10 = value10;
		}

		public T1 Value1 { get; }
		public T2 Value2 { get; }
		public T3 Value3 { get; }
		public T4 Value4 { get; }
		public T5 Value5 { get; }
		public T6 Value6 { get; }
		public T7 Value7 { get; }
		public T8 Value8 { get; }
		public T9 Value9 { get; }
		public T10 Value10 { get; }

		static IEqualityComparer<T1> _value1ValueComparer = ValueComparer.GetDefaultValueComparer<T1>(true);
		static IEqualityComparer<T2> _value2ValueComparer = ValueComparer.GetDefaultValueComparer<T2>(true);
		static IEqualityComparer<T3> _value3ValueComparer = ValueComparer.GetDefaultValueComparer<T3>(true);
		static IEqualityComparer<T4> _value4ValueComparer = ValueComparer.GetDefaultValueComparer<T4>(true);
		static IEqualityComparer<T5> _value5ValueComparer = ValueComparer.GetDefaultValueComparer<T5>(true);
		static IEqualityComparer<T6> _value6ValueComparer = ValueComparer.GetDefaultValueComparer<T6>(true);
		static IEqualityComparer<T7> _value7ValueComparer = ValueComparer.GetDefaultValueComparer<T7>(true);
		static IEqualityComparer<T8> _value8ValueComparer = ValueComparer.GetDefaultValueComparer<T8>(true);
		static IEqualityComparer<T9> _value9ValueComparer = ValueComparer.GetDefaultValueComparer<T9>(true);
		static IEqualityComparer<T10> _value10ValueComparer = ValueComparer.GetDefaultValueComparer<T10>(true);

		int IComparable.CompareTo(object obj)
			=> ((IStructuralComparable) this).CompareTo(obj, (IComparer) Comparer<object>.Default);

		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null) return 1;

			if (other is not SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> otherRow)
				throw new ArgumentException("Argument is not SqlRow", nameof(other));

			var c = comparer.Compare(Value1, otherRow.Value1);
			if (c != 0) return c;

			c = comparer.Compare(Value2, otherRow.Value2);
			if (c != 0) return c;

			c = comparer.Compare(Value3, otherRow.Value3);
			if (c != 0) return c;

			c = comparer.Compare(Value4, otherRow.Value4);
			if (c != 0) return c;

			c = comparer.Compare(Value5, otherRow.Value5);
			if (c != 0) return c;

			c = comparer.Compare(Value6, otherRow.Value6);
			if (c != 0) return c;

			c = comparer.Compare(Value7, otherRow.Value7);
			if (c != 0) return c;

			c = comparer.Compare(Value8, otherRow.Value8);
			if (c != 0) return c;

			c = comparer.Compare(Value9, otherRow.Value9);
			if (c != 0) return c;

			c = comparer.Compare(Value10, otherRow.Value10);
			return c;
		}

		public override bool Equals(object other)
		{
			if (other is not SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> otherRow)
				return false;

			return _value1ValueComparer.Equals(Value1, otherRow.Value1)
				&& _value2ValueComparer.Equals(Value2, otherRow.Value2)
				&& _value3ValueComparer.Equals(Value3, otherRow.Value3)
				&& _value4ValueComparer.Equals(Value4, otherRow.Value4)
				&& _value5ValueComparer.Equals(Value5, otherRow.Value5)
				&& _value6ValueComparer.Equals(Value6, otherRow.Value6)
				&& _value7ValueComparer.Equals(Value7, otherRow.Value7)
				&& _value8ValueComparer.Equals(Value8, otherRow.Value8)
				&& _value9ValueComparer.Equals(Value9, otherRow.Value9)
				&& _value10ValueComparer.Equals(Value10, otherRow.Value10);
		}

		public override int GetHashCode()
			=> SqlRow.CombineHashCodes(Value1 is null ? 0 : _value1ValueComparer.GetHashCode(Value1),
				Value2 is null ? 0 : _value2ValueComparer.GetHashCode(Value2),
				Value3 is null ? 0 : _value3ValueComparer.GetHashCode(Value3),
				Value4 is null ? 0 : _value4ValueComparer.GetHashCode(Value4),
				Value5 is null ? 0 : _value5ValueComparer.GetHashCode(Value5),
				Value6 is null ? 0 : _value6ValueComparer.GetHashCode(Value6),
				Value7 is null ? 0 : _value7ValueComparer.GetHashCode(Value7),
				Value8 is null ? 0 : _value8ValueComparer.GetHashCode(Value8),
				Value9 is null ? 0 : _value9ValueComparer.GetHashCode(Value9),
				Value10 is null ? 0 : _value10ValueComparer.GetHashCode(Value10));

		public static bool operator > (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> y)
			=> ((IComparable)x).CompareTo(y) > 0;

		public static bool operator < (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> y)
			=> ((IComparable)x).CompareTo(y) < 0;

		public static bool operator >= (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> y)
			=> ((IComparable)x).CompareTo(y) >= 0;

		public static bool operator <= (SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> x, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> y)
			=> ((IComparable)x).CompareTo(y) <= 0;

	}

	#region Constructor methods

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1> Row<T1>(T1 value1)
		=> new SqlRow<T1>(value1);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2> Row<T1, T2>(T1 value1, T2 value2)
		=> new SqlRow<T1, T2>(value1, value2);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2, T3> Row<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
		=> new SqlRow<T1, T2, T3>(value1, value2, value3);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2, T3, T4> Row<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
		=> new SqlRow<T1, T2, T3, T4>(value1, value2, value3, value4);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2, T3, T4, T5> Row<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
		=> new SqlRow<T1, T2, T3, T4, T5>(value1, value2, value3, value4, value5);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2, T3, T4, T5, T6> Row<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
		=> new SqlRow<T1, T2, T3, T4, T5, T6>(value1, value2, value3, value4, value5, value6);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2, T3, T4, T5, T6, T7> Row<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
		=> new SqlRow<T1, T2, T3, T4, T5, T6, T7>(value1, value2, value3, value4, value5, value6, value7);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> Row<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
		=> new SqlRow<T1, T2, T3, T4, T5, T6, T7, T8>(value1, value2, value3, value4, value5, value6, value7, value8);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> Row<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9)
		=> new SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9>(value1, value2, value3, value4, value5, value6, value7, value8, value9);

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Row<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10)
		=> new SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(value1, value2, value3, value4, value5, value6, value7, value8, value9, value10);

	#endregion Constructor methods

	#region Overlaps

	// Note that SQL standard doesn't define OVERLAPS for all comparable data types, such as numbers.
	// RDBMS only support OVERLAPS for date(-time) and interval types.

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1>(this SqlRow<T1> thisRow, SqlRow<T1> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2>(this SqlRow<T1, T2> thisRow, SqlRow<T1, T2> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3>(this SqlRow<T1, T2, T3> thisRow, SqlRow<T1, T2, T3> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3, T4>(this SqlRow<T1, T2, T3, T4> thisRow, SqlRow<T1, T2, T3, T4> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3, T4, T5>(this SqlRow<T1, T2, T3, T4, T5> thisRow, SqlRow<T1, T2, T3, T4, T5> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3, T4, T5, T6>(this SqlRow<T1, T2, T3, T4, T5, T6> thisRow, SqlRow<T1, T2, T3, T4, T5, T6> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3, T4, T5, T6, T7>(this SqlRow<T1, T2, T3, T4, T5, T6, T7> thisRow, SqlRow<T1, T2, T3, T4, T5, T6, T7> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3, T4, T5, T6, T7, T8>(this SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> thisRow, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> thisRow, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9> other)
		=> throw new NotImplementedException();

	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> thisRow, SqlRow<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other)
		=> throw new NotImplementedException();

	#endregion Overlaps
}
