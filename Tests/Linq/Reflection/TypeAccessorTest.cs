using System;

using LinqToDB.Reflection;

using NUnit.Framework;

namespace Tests.Reflection
{
	[TestFixture]
	public class TypeAccessorTest : TestBase
	{
		class TestClass1
		{
			public int Prop1
			{
				get { return 0; }
			}

			public int Prop2 { get; set; }

			public TestClass2 Class2;
		}

		class TestClass2
		{
			public TestClass3  Class3;
			public TestStruct1 Struct1;
		}

		class TestClass3
		{
			public TestClass4 Class4;
		}

		class TestClass4
		{
			public int Field1;
		}

		struct TestStruct1
		{
			public TestClass3 Class3;
		}

		[Test]
		public void Test1()
		{
			var ta = TypeAccessor.GetAccessor<TestClass1>();

			var obj = ta.CreateInstance();

			ta["Prop1"].SetValue(obj, 10);
		}

		[Test]
		public void Test2()
		{
			var ta = TypeAccessor.GetAccessor<TestClass1>();

			var obj = ta.Create();

			ta["Prop2"].SetValue(obj, 10);

			Assert.That(obj.Prop2, Is.EqualTo(10));
		}

		[Test]
		public void Test3()
		{
			var ta  = TypeAccessor.GetAccessor<TestClass1>();
			var obj = new TestClass1
			{
				Class2 = new TestClass2 {
				Class3 = new TestClass3 {
				Class4 = new TestClass4 { Field1 = 50 }
			}}};

			var value = ta["Class2.Class3.Class4.Field1"].GetValue(obj);

			Assert.That(value, Is.EqualTo(50));
		}

		[Test]
		public void Test4()
		{
			var ta  = TypeAccessor.GetAccessor<TestClass1>();
			var obj = new TestClass1
			{
				Class2  = new TestClass2  {
				Struct1 = new TestStruct1 {
				Class3  = new TestClass3  {
				Class4  = new TestClass4  { Field1 = 50 }
			}}}};

			var value = ta["Class2.Struct1.Class3.Class4.Field1"].GetValue(obj);

			Assert.That(value, Is.EqualTo(50));
		}

		[Test]
		public void Test5()
		{
			var ta  = TypeAccessor.GetAccessor<TestClass1>();
			var obj = new TestClass1();

			ta["Class2.Class3.Class4.Field1"].SetValue(obj, 42);

			Assert.That(obj.Class2.Class3.Class4.Field1, Is.EqualTo(42));
		}

		[Test]
		public void Test6()
		{
			var ta  = TypeAccessor.GetAccessor<TestClass1>();
			var obj = new TestClass1();

			ta["Class2.Struct1.Class3.Class4.Field1"].SetValue(obj, 42);

			Assert.That(obj.Class2.Struct1.Class3.Class4.Field1, Is.EqualTo(42));
		}
	}
}
