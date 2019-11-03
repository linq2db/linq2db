using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB;
using LinqToDB.Expressions;
using NUnit.Framework;

namespace Tests.Playground
{
	namespace Dynamic
	{
		public class SampleClass
		{
			public int Id    { get; set; }
			public int Value { get; set; }
			public string StrValue { get; set; }
			public OtherClass GetOther(int idx) => new OtherClass { OtherStrProp = "OtherStrValue" + idx };
			public OtherClass GetOtherAnother(int idx) => new OtherClass { OtherStrProp = "OtherAnotherStrValue" + idx };

			public SampleClass()
			{

			}

			public SampleClass(int id, int value)
			{
				Id = id;
				Value = value;
			}

		}

		public static class SampleClassExtensions
		{
			public static string GetOtherStr(this SampleClass sc, int idx)
			{
				return sc.GetOther(idx).OtherStrProp;
			}
		}

		public class OtherClass
		{
			public string OtherStrProp { get; set; }
		}
	}
	
	namespace Wrappers
	{
		class SampleClass : TypeWrapper
		{
			public int Id    => this.Wrap(t => t.Id);
			public int Value => this.Wrap(t => t.Value);
			public string StrValue { get; set; }
			public OtherClass GetOther(int idx) => throw new NotImplementedException();
			public OtherClass GetOtherAnother(int idx) => this.Wrap(t => t.GetOtherAnother(idx));

			public SampleClass(object instance, [NotNull] TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SampleClass(int id, int value) => throw new NotImplementedException();
		}

		[Wrapper]
		internal static class SampleClassExtensions
		{
			internal static string GetOtherStr(this SampleClass sc, int idx) => throw new NotImplementedException();
		}

		class OtherClass : TypeWrapper
		{
			public string OtherStrProp => this.Wrap(t => t.OtherStrProp);

			public OtherClass(object instance, [NotNull] TypeMapper mapper) : base(instance, mapper)
			{
			}
		}

		[TestFixture]
		public class MappingTests : TestBase
		{
			private TypeMapper _typeMapper = new TypeMapper(typeof(Dynamic.SampleClass), typeof(Dynamic.OtherClass), typeof(Dynamic.SampleClassExtensions));

			[Test]
			public void WrappingTests()
			{

				var concrete = new Dynamic.SampleClass{ Id = 1, Value = 33 };

				var l1 = _typeMapper.MapLambda((SampleClass s) => s.Value);
				var l2 = _typeMapper.MapLambda((SampleClass s) => s.Id);
				var l3 = _typeMapper.MapLambda((SampleClass s, int i) => s.GetOther(i));
				var l4 = _typeMapper.MapLambda((SampleClass s, int i) => s.GetOther(i).OtherStrProp);


				var cl1 = (Func<Dynamic.SampleClass, int>)l1.Compile();
				var cl2 = (Func<Dynamic.SampleClass, int>)l2.Compile();
				var cl3 = (Func<Dynamic.SampleClass, int, Dynamic.OtherClass>)l3.Compile();
				var cl4 = (Func<Dynamic.SampleClass, int, string>)l4.Compile();

				Assert.That(cl1(concrete), Is.EqualTo(33));
				Assert.That(cl2(concrete), Is.EqualTo(1));
				Assert.That(cl3(concrete, 11).OtherStrProp, Is.EqualTo("OtherStrValue11"));
				Assert.That(cl4(concrete, 22), Is.EqualTo("OtherStrValue22"));

				var dynamicInstance = (object)concrete;

				var value1 = _typeMapper.Evaluate<SampleClass>(dynamicInstance, s => s.GetOther(1).OtherStrProp);
				Assert.That(value1, Is.EqualTo("OtherStrValue1"));

				var value2 = _typeMapper.Evaluate<SampleClass>(dynamicInstance, s => s.GetOther(2).OtherStrProp);
				Assert.That(value2, Is.EqualTo("OtherStrValue2"));

				var value11 = _typeMapper.Evaluate<SampleClass>(dynamicInstance, s => s.GetOtherStr(3));
				Assert.That(value11, Is.EqualTo("OtherStrValue3"));

				var wrapper = _typeMapper.Wrap<SampleClass>(dynamicInstance);

				var str1 = wrapper.GetOtherAnother(5).OtherStrProp;
				Assert.That(str1, Is.EqualTo("OtherAnotherStrValue5"));

				Assert.Throws<NotImplementedException>(() => wrapper.GetOther(10));

				var obj = (Dynamic.OtherClass)wrapper.Evaluate(w => w.GetOther(10));
				Assert.That(obj.GetType(), Is.EqualTo(typeof(Dynamic.OtherClass)));

			}

			[Test]
			public void TestNew()
			{
				var newExpression = _typeMapper.MapExpression(() => new SampleClass(55, 77));
				
				var newLambda = Expression.Lambda<Func<Dynamic.SampleClass>>(newExpression);
				var instance = newLambda.Compile()();

				Assert.That(instance.Id, Is.EqualTo(55));
				Assert.That(instance.Value, Is.EqualTo(77));
			}

			[Test]
			public void TestMemberInit()
			{
				var newMemberInit = _typeMapper.MapExpression(() => new SampleClass(55, 77) {StrValue = "Str"});
				var memberInitLambda = Expression.Lambda<Func<Dynamic.SampleClass>>(newMemberInit);

				var instance = memberInitLambda.Compile()();

				Assert.That(instance.Id, Is.EqualTo(55));
				Assert.That(instance.Value, Is.EqualTo(77));
				Assert.That(instance.StrValue, Is.EqualTo("Str"));
			}

			[Test]
			public void TestMapFunc()
			{
				var newMemberInit = _typeMapper.MapLambda((int i) => new SampleClass(i + 55, i + 77) {StrValue = "Str"});
				var func = _typeMapper.MapFunc<byte, object>(newMemberInit);
				
				var instance = (Dynamic.SampleClass)func(1);

				Assert.That(instance.Id, Is.EqualTo(56));
				Assert.That(instance.Value, Is.EqualTo(78));
				Assert.That(instance.StrValue, Is.EqualTo("Str"));
			}


		}

	}


}
