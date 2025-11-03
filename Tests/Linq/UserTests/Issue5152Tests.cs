using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5152Tests : TestBase
	{
		[ScalarType]
		public abstract class MySpecialBaseClass : IConvertible, IEquatable<MySpecialBaseClass>
		{
			[ExpressionMethod(nameof(ValueCast))]
			public string Value { get; set; }

			static Expression<Func<MySpecialBaseClass, string>> ValueCast() => awl => awl.ToString();

			protected MySpecialBaseClass(string value)
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				Value = value;
			}

			public static bool operator ==(MySpecialBaseClass? leftSide, string? rightSide)
				=> leftSide?.Value == rightSide;

			public static bool operator !=(MySpecialBaseClass? leftSide, string? rightSide)
				=> leftSide?.Value != rightSide;

			public override string ToString() => Value;

			public override int GetHashCode() => Value.GetHashCode();

			public override bool Equals(object? obj)
			{
				if (obj == null)
					return false;

				if (obj is string str)
					return Value.Equals(str);

				if (obj.GetType() == GetType())
					return string.Equals(((MySpecialBaseClass)obj).Value, Value);

				return base.Equals(obj);
			}

			public bool Equals(MySpecialBaseClass? obj)
			{
				if (obj == null && Value == null)
					return true;

				if (obj == null)
					return false;

				if (obj.GetType() != GetType())
					return false;

				return string.Equals(obj.Value, Value);
			}

			#region IConvertible

			public TypeCode GetTypeCode() => TypeCode.String;

			public string ToString(IFormatProvider? provider) => Value;

			public object ToType(Type conversionType, IFormatProvider? provider)
			{
				if (conversionType.IsSubclassOf(typeof(MySpecialBaseClass))
					|| conversionType == typeof(MySpecialBaseClass))
				{
					return this;
				}

				return Value;
			}

			public bool ToBoolean(IFormatProvider? provider) { throw new NotImplementedException(); }
			public char ToChar(IFormatProvider? provider) { throw new NotImplementedException(); }
			public sbyte ToSByte(IFormatProvider? provider) { throw new NotImplementedException(); }
			public byte ToByte(IFormatProvider? provider) { throw new NotImplementedException(); }
			public short ToInt16(IFormatProvider? provider) { throw new NotImplementedException(); }
			public ushort ToUInt16(IFormatProvider? provider) { throw new NotImplementedException(); }
			public int ToInt32(IFormatProvider? provider) { throw new NotImplementedException(); }
			public uint ToUInt32(IFormatProvider? provider) { throw new NotImplementedException(); }
			public long ToInt64(IFormatProvider? provider) { throw new NotImplementedException(); }
			public ulong ToUInt64(IFormatProvider? provider) { throw new NotImplementedException(); }
			public float ToSingle(IFormatProvider? provider) { throw new NotImplementedException(); }
			public double ToDouble(IFormatProvider? provider) { throw new NotImplementedException(); }
			public decimal ToDecimal(IFormatProvider? provider) { throw new NotImplementedException(); }
			public DateTime ToDateTime(IFormatProvider? provider) { throw new NotImplementedException(); }
			#endregion
		}

		[ScalarType]
		public class MySpecialStringClass : MySpecialBaseClass
		{
			public const string Example = "Example";

			public static ISet<MySpecialStringClass> AllValues = new HashSet<MySpecialStringClass>
			{
				new MySpecialStringClass(Example)
			};

			public MySpecialStringClass(string value)
				: base(value)
			{
			}

			[return: NotNullIfNotNull(nameof(value))]
			public static implicit operator MySpecialStringClass?(string? value)
			{
				if (value == null)
					return null;

				return AllValues.FirstOrDefault(a => string.Equals(a.Value, value, StringComparison.OrdinalIgnoreCase)) ?? new MySpecialStringClass(value);
			}

			[return: NotNullIfNotNull(nameof(auswahlliste))]
			public static implicit operator string?(MySpecialStringClass? auswahlliste)
				=> auswahlliste?.Value;
		}

		[Table]
		class SampleClass
		{
			[Column] public int Id { get; set; }

			[Column(DataType = DataType.NVarChar, Length = 50)] public MySpecialStringClass? MyString { get; set; }

			[Column] public string? NormalString { get; set; }
		}

		[Test]
		public void TestCase1([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>();

			// CAST in setter lvalue
			/* UPDATE
				[SampleClass]
			   SET
				// !!!!
				CAST([SampleClass].[MyString] AS NVarChar(4000)) = ... */
			var queryBrokenSet = (from sample in table
									  // I Have to cast with ToString() or use .Value so I can use string.Contains.
								  where sample.MyString!.ToString().Contains(MySpecialStringClass.Example)
								  select sample)
								  .Set(
									sample => sample.MyString!.ToString(), // ToString() Or Value -> Broken Set Clause with cast. Works in Linq2db 5.
									sample => sample.MyString!.ToString().Replace(";" + MySpecialStringClass.Example, null).Replace(MySpecialStringClass.Example, null))
								  .Update();

			if (db is DataConnection dc)
				Assert.That(dc.LastQuery?.ToLowerInvariant(), Does.Not.Contain("cast(")
					.And.Not.Contain("::")
					.And.Not.Contain("cstr("));
		}

		[Test]
		public void TestCase2([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>();

			// Problem 2: This produces an Update-Statement with a correct Set Clause. But there are Casts everywhere, in the Where Statement and in the Value Part of the Set-Clause.
			// This is also a performance Problem, because the Casts prevent the use of Indexes and casts the whole nvarchar-column to nvarchar again.
			/* UPDATE
				[SampleClass]
			   SET
				[SampleClass].[MyString] = CAST(Replace(Replace([SampleClass].[MyString], @param1, NULL), CAST(@param2 AS NVarChar(255)), NULL) AS NVarChar(Max)) 
				WHERE
				   CAST([SampleClass].[MyString] AS NVarChar(4000)) LIKE @param3 ESCAPE N'~'
			 */
			var queryWithCasts = (from sample in table
								  // I Have to cast with ToString() or use .Value so I can use string.Contains.
								  where sample.MyString!.ToString().Contains(MySpecialStringClass.Example)
								  select sample)
								  .Set(
									sample => sample.MyString,
									sample => (MySpecialStringClass)sample.MyString!.ToString().Replace(";" + MySpecialStringClass.Example, null).Replace(MySpecialStringClass.Example, null))
								.Update();

			if (db is DataConnection dc)
				Assert.That(dc.LastQuery?.ToLowerInvariant(), Does.Not.Contain("cast(")
					.And.Not.Contain("::")
					.And.Not.Contain("cstr("));
		}
	}
}
