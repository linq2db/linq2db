using System;
using System.Collections;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.Expressions;
using NUnit.Framework;

namespace Tests
{
	namespace Dynamic
	{
		public delegate void        SimpleDelegate              (string input);
		public delegate void        SimpleDelegateWithMapping   (SampleClass input);
		public delegate string      ReturningDelegate           (string input);
		public delegate SampleClass ReturningDelegateWithMapping(SampleClass input);

		public class SampleClass
		{
			public int Id    { get; set; }
			public int Value { get; set; }
			public string StrValue { get; set; }
			public OtherClass GetOther(int idx) => new OtherClass { OtherStrProp = "OtherStrValue" + idx };
			public OtherClass GetOtherAnother(int idx) => new OtherClass { OtherStrProp = "OtherAnotherStrValue" + idx };

			public void SomeAction() => ++Value;

			public event SimpleDelegate               SimpleDelegateEvent;
			public event SimpleDelegateWithMapping    SimpleDelegateWithMappingEvent;
			// just test-case, nobody in their mind should use returning events
			public event ReturningDelegate            ReturningDelegateEvent;
			public event ReturningDelegateWithMapping ReturningDelegateWithMappingEvent;

			public void Fire(bool withHandlers)
			{
				// https://www.youtube.com/watch?v=r32LcBqiv7I
				SimpleDelegateEvent?.Invoke("param1");
				SimpleDelegateWithMappingEvent?.Invoke(this);
				var strResult  = ReturningDelegateEvent?.Invoke("event1");
				var thisResult = ReturningDelegateWithMappingEvent?.Invoke(this);

				if (withHandlers)
				{
					Assert.AreEqual("event1", strResult);
					Assert.AreEqual(this, thisResult);
				}
				else
				{
					Assert.IsNull(strResult);
					Assert.IsNull(thisResult);
				}
			}

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

		public class CollectionSample : CollectionBase
		{
			public SampleClass Add(SampleClass sample)
			{
				return sample;
			}
		}
	}
	
	namespace Wrappers
	{
		[Wrapper] delegate void        SimpleDelegate              (string input);
		[Wrapper] delegate void        SimpleDelegateWithMapping   (SampleClass input);
		[Wrapper] delegate string      ReturningDelegate           (string input);
		[Wrapper] delegate SampleClass ReturningDelegateWithMapping(SampleClass input);

		class SampleClass : TypeWrapper
		{
			private bool _event1Wrapped;
			private bool _event2Wrapped;
			public int Id    => this.Wrap(t => t.Id);
			public int Value => this.Wrap(t => t.Value);
			public string StrValue { get; set; }
			public OtherClass GetOther(int idx) => throw new NotImplementedException();
			public OtherClass GetOtherAnother(int idx) => this.Wrap(t => t.GetOtherAnother(idx));

			public void SomeAction() => this.WrapAction(t => t.SomeAction());

			public void Fire(bool withHandlers) => this.WrapAction(t => t.Fire(withHandlers));

			public event SimpleDelegate               SimpleDelegateEvent
			{
				add    => Events.AddHandler(nameof(SimpleDelegateEvent), value);
				remove => Events.RemoveHandler(nameof(SimpleDelegateEvent), value);
			}
			public event SimpleDelegateWithMapping    SimpleDelegateWithMappingEvent
			{
				add    => Events.AddHandler(nameof(SimpleDelegateWithMappingEvent), value);
				remove => Events.RemoveHandler(nameof(SimpleDelegateWithMappingEvent), value);
			}
			public event ReturningDelegate            ReturningDelegateEvent
			{
				add    => AddHandlerDelayed(nameof(ReturningDelegateEvent), value, ref _event1Wrapped);
				remove => Events.RemoveHandler(nameof(ReturningDelegateEvent), value);
			}
			public event ReturningDelegateWithMapping ReturningDelegateWithMappingEvent
			{
				add => AddHandlerDelayed(nameof(ReturningDelegateWithMappingEvent), value, ref _event2Wrapped);
				remove => Events.RemoveHandler(nameof(ReturningDelegateWithMappingEvent), value);
			}

			private void AddHandlerDelayed<TDelegate>(string eventName, TDelegate handler, ref bool wrapped)
				where TDelegate : Delegate
			{
				// wrap event only when handler added to avoid base event fired for empty handler
				// this is needed only for events with return value to work properly
				// (if `properly` word could even applied to such events, more like to make tests work)
				if (!wrapped)
				{
					this.WrapEvent<SampleClass, TDelegate>(eventName);
					wrapped = true;
				}

				Events.AddHandler(eventName, handler);
			}

			public SampleClass(object instance, [NotNull] TypeMapper mapper) : base(instance, mapper)
			{
				this.WrapEvent<SampleClass, SimpleDelegate>(nameof(SimpleDelegateEvent));
				this.WrapEvent<SampleClass, SimpleDelegateWithMapping>(nameof(SimpleDelegateWithMappingEvent));
				//this.WrapEvent<SampleClass, ReturningDelegate>(nameof(ReturningDelegateEvent));
				//this.WrapEvent<SampleClass, ReturningDelegateWithMapping>(nameof(ReturningDelegateWithMappingEvent));
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

		class CollectionSample : TypeWrapper
		{
			public CollectionSample()
			{
			}

			public CollectionSample(object instance, [NotNull] TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SampleClass Add(SampleClass sample) => this.Wrap(t => t.Add(sample));
		}

		[TestFixture]
		public class MappingTests : TestBase
		{
			private TypeMapper _typeMapper = new TypeMapper(
				typeof(Dynamic.SampleClass), 
				typeof(Dynamic.OtherClass), 
				typeof(Dynamic.SampleClassExtensions),
				typeof(Dynamic.CollectionSample),
				typeof(Dynamic.SimpleDelegate),
				typeof(Dynamic.SimpleDelegateWithMapping),
				typeof(Dynamic.ReturningDelegate),
				typeof(Dynamic.ReturningDelegateWithMapping)
				);

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
				var func = _typeMapper.BuildFunc<byte, object>(newMemberInit);
				
				var instance = (Dynamic.SampleClass)func(1);

				Assert.That(instance.Id, Is.EqualTo(56));
				Assert.That(instance.Value, Is.EqualTo(78));
				Assert.That(instance.StrValue, Is.EqualTo("Str"));
			}

			[Test]
			public void TesWrapper()
			{
				var wrapper = _typeMapper.CreateAndWrap(() => new SampleClass(1, 2));

				wrapper.SomeAction();
				Assert.That(wrapper.Value, Is.EqualTo(3));
			}

			[Test]
			public void TesCollection()
			{
				var collection = _typeMapper.CreateAndWrap(() => new CollectionSample());
				var obj = _typeMapper.CreateAndWrap(() => new SampleClass(1, 2));

				var same = collection.Add(obj);

				Assert.That(same.Id,    Is.EqualTo(1));
				Assert.That(same.Value, Is.EqualTo(2));
			}

			[Test]
			public void TestEvents()
			{
				var wrapper  = _typeMapper.CreateAndWrap(() => new SampleClass(1, 2));
				var instance = (Dynamic.SampleClass)wrapper.instance_;

				// no subscribers
				wrapper.Fire(false);

				// subscribed
				string strValue1 = null;
				SampleClass thisValue1 = null;
				wrapper.SimpleDelegateEvent                    += handler1;
				wrapper.SimpleDelegateWithMappingEvent         += handler2;
				wrapper.ReturningDelegateEvent                 += handler3;
				wrapper.ReturningDelegateWithMappingEvent      += handler4;
				wrapper.Fire(true);

				Assert.AreEqual("param1", strValue1);
				Assert.AreEqual(instance, (Dynamic.SampleClass)thisValue1.instance_);


				wrapper.SimpleDelegateEvent                    -= handler1;
				wrapper.SimpleDelegateWithMappingEvent         -= handler2;
				wrapper.ReturningDelegateEvent                 -= handler3;
				wrapper.ReturningDelegateWithMappingEvent      -= handler4;

				// no subscribers again
				wrapper.Fire(false);

				void handler1(string input)
				{
					strValue1 = input;
				}

				void handler2(SampleClass input)
				{
					thisValue1 = input;
				}

				string handler3(string input) => input;

				SampleClass handler4(SampleClass input) => input;
			}

		}

	}
}
