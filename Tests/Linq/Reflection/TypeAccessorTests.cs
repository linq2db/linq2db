using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Reflection;

using NUnit.Framework;

namespace Tests.Reflection
{
	[TestFixture]
	public class TypeAccessorTests : TestBase
	{
		sealed class TestClass1
		{
			public int Prop1
			{
				get { return 0; }
			}

			public int Prop2 { get; set; }

			public TestClass2? Class2;

#pragma warning disable IDE0052 // Remove unread private members
			int _prop3;
			public int Prop3
			{
				set { _prop3 = value; }
			}
#pragma warning restore IDE0052 // Remove unread private members
		}

		sealed class TestClass2
		{
			public TestClass3? Class3;
			public TestStruct1 Struct1;
		}

		sealed class TestClass3
		{
			public TestClass4? Class4;
		}

		sealed class TestClass4
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

			ta.GetOrCreateMemberAccessor("Prop1").SetValue(obj, 10);
		}

		[Test]
		public void Test2()
		{
			var ta = TypeAccessor.GetAccessor<TestClass1>();

			var obj = ta.Create();

			ta.GetOrCreateMemberAccessor("Prop2").SetValue(obj, 10);

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

			var value = ta.GetOrCreateMemberAccessor("Class2.Class3.Class4.Field1").GetValue(obj);

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

			var value = ta.GetOrCreateMemberAccessor("Class2.Struct1.Class3.Class4.Field1").GetValue(obj);

			Assert.That(value, Is.EqualTo(50));
		}

		[Test]
		public void Test5()
		{
			var ta  = TypeAccessor.GetAccessor<TestClass1>();
			var obj = new TestClass1();

			ta.GetOrCreateMemberAccessor("Class2.Class3.Class4.Field1").SetValue(obj, 42);

			Assert.That(obj.Class2!.Class3!.Class4!.Field1, Is.EqualTo(42));
		}

		[Test]
		public void Test6()
		{
			var ta  = TypeAccessor.GetAccessor<TestClass1>();
			var obj = new TestClass1();

			ta.GetOrCreateMemberAccessor("Class2.Struct1.Class3.Class4.Field1").SetValue(obj, 42);

			Assert.That(obj.Class2!.Struct1.Class3.Class4!.Field1, Is.EqualTo(42));
		}

		[Test]
		public void GetterTest()
		{
#pragma warning disable CA2263 // Prefer generic overload when type is known
			var ta = TypeAccessor.GetAccessor(typeof(TestClass1));
#pragma warning restore CA2263 // Prefer generic overload when type is known
			var ma = ta.GetOrCreateMemberAccessor("Prop1");
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ma.HasGetter, Is.True);
				Assert.That(ma.HasSetter, Is.False);
			}
		}

		[Test]
		public void SetterTest()
		{
#pragma warning disable CA2263 // Prefer generic overload when type is known
			var ta = TypeAccessor.GetAccessor(typeof(TestClass1));
#pragma warning restore CA2263 // Prefer generic overload when type is known
			var ma = ta.GetOrCreateMemberAccessor("Prop3");
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ma.HasGetter, Is.False);
				Assert.That(ma.HasSetter, Is.True);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5361")]
		public void TypeAccessor_ThreadSafety_AddStorageField()
		{
			var typeAccessor = TypeAccessor.GetAccessor<TypeAccessorMutations1>();

			var tasks = new Task[10];

			using var wait = new ManualResetEvent(false);

			for (var i = 0; i < tasks.Length; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					foreach (var member in typeAccessor.Members)
					{
						wait.WaitOne();

						// emulate ColumnAttribute.Storage late init
						_ = typeAccessor.GetOrCreateMemberAccessor("_field");
	}
				});
}

			wait.Set();
			Task.WaitAll(tasks);

			Assert.That(typeAccessor.Members, Has.Count.EqualTo(3));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5361")]
		public void TypeAccessor_ThreadSafety_AddInternalMember()
		{
			var typeAccessor = TypeAccessor.GetAccessor<TypeAccessorMutations2>();

			var tasks = new Task[10];

			using var wait = new ManualResetEvent(false);

			for (var i = 0; i < tasks.Length; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					foreach (var member in typeAccessor.Members)
					{
						wait.WaitOne();

						// internal members not loaded by default
						_ = typeAccessor.GetOrCreateMemberAccessor(nameof(TypeAccessorMutations2.Field2));
					}
				});
			}

			wait.Set();
			Task.WaitAll(tasks);

			Assert.That(typeAccessor.Members, Has.Count.EqualTo(3));
		}

		sealed class TypeAccessorMutations1
		{
			private int _field { get; set; }

			public int Field1 { get; set; }
			public int Field2 { get; set; }
		}

		sealed class TypeAccessorMutations2
		{
			public int Field1 { get; set; }
			internal int Field2 { get; set; }
			public int Field3 { get; set; }
		}
	}
}
