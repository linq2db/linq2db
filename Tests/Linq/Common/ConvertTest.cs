using System;

using LinqToDB_Temp.Common;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ConvertTest : TestBase
	{
		[Test]
		public void SameType()
		{
			Assert.AreEqual(1,   ConvertTo<int>.From(1));
			Assert.AreEqual("1", ConvertTo<string>.From("1"));
			Assert.AreEqual(1,   Convert<int,int>.From(1));
			Assert.AreEqual("1", Convert<string,string>.From("1"));
		}

		[Test]
		public void SetExpression()
		{
			Convert<int, string>.Lambda = i => (i * 2).ToString();

			Assert.AreEqual("4", Convert<int,string>.From(2));

			Convert<int, string>.Expression = i => (i * 3).ToString();

			Assert.AreEqual("9", Convert<int,string>.From(3));

			Convert<int, string>.Lambda = null;

			Assert.AreEqual("1", Convert<int,string>.From(1));
		}

		[Test]
		public void ObjectToString()
		{
			Assert.AreEqual("1", Convert<int,string>.From(1));
		}

		[Test]
		public void ToObject()
		{
			Assert.AreEqual(1, ConvertTo<object>.From(1));
		}

		enum Enum1
		{
			Value1,
			Value2
		}

		[Test]
		public void Nullable()
		{
			Assert.AreEqual(0,            Convert<int?,  int>.   From(null));
			Assert.AreEqual(10,           Convert<int,   int?>.  From(10));
			Assert.AreEqual(Enum1.Value1, Convert<int?,  Enum1>. From(null));
			Assert.AreEqual(null,         Convert<int?,  Enum1?>.From(null));
			Assert.AreEqual(1,            Convert<Enum1, int?>.  From(Enum1.Value2));
			Assert.AreEqual(null,         Convert<Enum1?,int?>.  From(null));
		}

		[Test]
		public void Ctor()
		{
			Assert.AreEqual(10m, Convert<int,decimal>.From(10));
		}
	}
}
