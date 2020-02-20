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

			public RegularEnum GetRegularEnum(int raw) => (RegularEnum)raw;
			public FlagsEnum   GetFlagsEnum  (int raw) => (FlagsEnum)raw;

			public int SetRegularEnum(RegularEnum val) => (int)val;
			public int SetFlagsEnum  (FlagsEnum val  ) => (int)val;

			public RegularEnum RegularEnumProperty { get; set; } = RegularEnum.Two;
			public FlagsEnum   FlagsEnumProperty   { get; set; } = FlagsEnum  .Bit3;
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

		public enum RegularEnum
		{
			One   = 1,
			Two   = 2,
			Three = 3
		}

		[Flags]
		public enum FlagsEnum
		{
			Bit1   = 1,
			Bit3   = 4,
			Bits24 = 10
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
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get Id
				(Expression<Func<SampleClass, int>>)((SampleClass this_) => this_.Id),
				// [1]: get Value
				(Expression<Func<SampleClass, int>>)((SampleClass this_) => this_.Value),
				// [2]: GetOtherAnother
				(Expression<Func<SampleClass, int, OtherClass>>)((SampleClass this_, int idx) => this_.GetOtherAnother(idx)),
				// [3]: SomeAction
				(Expression<Action<SampleClass>>)((SampleClass this_) => this_.SomeAction()),
				// [4]: GetRegularEnum
				(Expression<Func<SampleClass, int, RegularEnum>>)((SampleClass this_, int raw) => this_.GetRegularEnum(raw)),
				// [5]: GetFlagsEnum
				(Expression<Func<SampleClass, int, FlagsEnum>>)((SampleClass this_, int raw) => this_.GetFlagsEnum(raw)),
				// [6]: SetRegularEnum
				(Expression<Func<SampleClass, RegularEnum, int>>)((SampleClass this_, RegularEnum val) => this_.SetRegularEnum(val)),
				// [7]: SetFlagsEnum
				(Expression<Func<SampleClass, FlagsEnum, int>>)((SampleClass this_, FlagsEnum val) => this_.SetFlagsEnum(val)),
				// [8]: get RegularEnumProperty
				(Expression<Func<SampleClass, RegularEnum>>)((SampleClass this_) => this_.RegularEnumProperty),
				// [9]: get FlagsEnumProperty
				(Expression<Func<SampleClass, FlagsEnum>>)((SampleClass this_) => this_.FlagsEnumProperty),
				// [10]: Fire
				(Expression<Action<SampleClass, bool>>)((SampleClass this_,bool withHandlers) => this_.Fire(withHandlers)),
				// [11]: set RegularEnumProperty
				PropertySetter((SampleClass this_) => this_.RegularEnumProperty),
				// [12]: set FlagsEnumProperty
				PropertySetter((SampleClass this_) => this_.FlagsEnumProperty),
			};

			private bool _event1Wrapped;
			private bool _event2Wrapped;
			public int Id    => ((Func<SampleClass, int>)CompiledWrappers[0])(this);
			public int Value => ((Func<SampleClass, int>)CompiledWrappers[1])(this);
			public string StrValue { get; set; }
			public OtherClass GetOther(int idx) => throw new NotImplementedException();
			public OtherClass GetOtherAnother(int idx) => ((Func<SampleClass, int, OtherClass>)CompiledWrappers[2])(this, idx);

			public void SomeAction() => ((Action<SampleClass>)CompiledWrappers[3])(this);

			public RegularEnum GetRegularEnum(int raw) => ((Func<SampleClass, int, RegularEnum>)CompiledWrappers[4])(this, raw);
			public FlagsEnum   GetFlagsEnum  (int raw) => ((Func<SampleClass, int, FlagsEnum>)CompiledWrappers[5])(this, raw);

			public int SetRegularEnum(RegularEnum val) => ((Func<SampleClass, RegularEnum, int>)CompiledWrappers[6])(this, val);
			public int SetFlagsEnum  (FlagsEnum   val) => ((Func<SampleClass, FlagsEnum, int>)CompiledWrappers[7])(this, val);

			public RegularEnum RegularEnumProperty
			{
				get => ((Func<SampleClass, RegularEnum>)CompiledWrappers[8])(this);
				set => ((Action<SampleClass, RegularEnum>)CompiledWrappers[11])(this, value);
			}

			public FlagsEnum FlagsEnumProperty
			{
				get => ((Func<SampleClass, FlagsEnum>)CompiledWrappers[9])(this);
				set => ((Action<SampleClass, FlagsEnum>)CompiledWrappers[12])(this, value);
			}

			public void Fire(bool withHandlers) => ((Action<SampleClass, bool>)CompiledWrappers[10])(this, withHandlers);

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

			public SampleClass(object instance, [NotNull] TypeMapper mapper, Delegate[] delegates) : base(instance, mapper, delegates)
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
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get OtherStrProp
				(Expression<Func<OtherClass, string>>)((OtherClass this_) => this_.OtherStrProp),
			};

			public string OtherStrProp => ((Func<OtherClass, string>)CompiledWrappers[0])(this);

			public OtherClass(object instance, [NotNull] TypeMapper mapper, Delegate[] delegates) : base(instance, mapper, delegates)
			{
			}
		}

		class CollectionSample : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Add
				(Expression<Func<CollectionSample, SampleClass, SampleClass>>)((CollectionSample this_, SampleClass item) => this_.Add(item)),
			};

			public CollectionSample()
			{
			}

			public CollectionSample(object instance, [NotNull] TypeMapper mapper, Delegate[] delegates) : base(instance, mapper, delegates)
			{
			}

			public SampleClass Add(SampleClass sample) => ((Func<CollectionSample, SampleClass, SampleClass>)CompiledWrappers[0])(this, sample);
		}

		[Wrapper]
		public enum RegularEnum
		{
			One   = 3,
			Two   = 1,
			Three = 2
		}

		[Wrapper, Flags]
		public enum FlagsEnum
		{
			Bit1   = 4,
			Bit3   = 10,
			Bits24 = 1
		}

		[TestFixture]
		public class MappingTests : TestBase
		{
			private TypeMapper CreateTypeMapper()
			{
				var typeMapper = new TypeMapper();

				typeMapper.RegisterTypeWrapper<SampleClass>(typeof(Dynamic.SampleClass));
				typeMapper.RegisterTypeWrapper<OtherClass>(typeof(Dynamic.OtherClass));
				typeMapper.RegisterTypeWrapper(typeof(SampleClassExtensions), typeof(Dynamic.SampleClassExtensions));
				typeMapper.RegisterTypeWrapper<CollectionSample>(typeof(Dynamic.CollectionSample));
				typeMapper.RegisterTypeWrapper<SimpleDelegate>(typeof(Dynamic.SimpleDelegate));
				typeMapper.RegisterTypeWrapper<SimpleDelegateWithMapping>(typeof(Dynamic.SimpleDelegateWithMapping));
				typeMapper.RegisterTypeWrapper<ReturningDelegate>(typeof(Dynamic.ReturningDelegate));
				typeMapper.RegisterTypeWrapper<ReturningDelegateWithMapping>(typeof(Dynamic.ReturningDelegateWithMapping));
				typeMapper.RegisterTypeWrapper<RegularEnum>(typeof(Dynamic.RegularEnum));
				typeMapper.RegisterTypeWrapper<FlagsEnum>(typeof(Dynamic.FlagsEnum));

				typeMapper.FinalizeMappings();

				return typeMapper;
			}

			[Test]
			public void WrappingTests()
			{

				var concrete = new Dynamic.SampleClass{ Id = 1, Value = 33 };

				var typeMapper = CreateTypeMapper();

				var l1 = typeMapper.MapLambda((SampleClass s) => s.Value);
				var l2 = typeMapper.MapLambda((SampleClass s) => s.Id);
				var l3 = typeMapper.MapLambda((SampleClass s, int i) => s.GetOther(i));
				var l4 = typeMapper.MapLambda((SampleClass s, int i) => s.GetOther(i).OtherStrProp);


				var cl1 = (Func<Dynamic.SampleClass, int>)l1.Compile();
				var cl2 = (Func<Dynamic.SampleClass, int>)l2.Compile();
				var cl3 = (Func<Dynamic.SampleClass, int, Dynamic.OtherClass>)l3.Compile();
				var cl4 = (Func<Dynamic.SampleClass, int, string>)l4.Compile();

				Assert.That(cl1(concrete), Is.EqualTo(33));
				Assert.That(cl2(concrete), Is.EqualTo(1));
				Assert.That(cl3(concrete, 11).OtherStrProp, Is.EqualTo("OtherStrValue11"));
				Assert.That(cl4(concrete, 22), Is.EqualTo("OtherStrValue22"));

				var dynamicInstance = (object)concrete;

				var value1 = typeMapper.Evaluate<SampleClass>(dynamicInstance, s => s.GetOther(1).OtherStrProp);
				Assert.That(value1, Is.EqualTo("OtherStrValue1"));

				var value2 = typeMapper.Evaluate<SampleClass>(dynamicInstance, s => s.GetOther(2).OtherStrProp);
				Assert.That(value2, Is.EqualTo("OtherStrValue2"));

				var value11 = typeMapper.Evaluate<SampleClass>(dynamicInstance, s => s.GetOtherStr(3));
				Assert.That(value11, Is.EqualTo("OtherStrValue3"));

				var wrapper = typeMapper.Wrap<SampleClass>(dynamicInstance);

				var str1 = wrapper.GetOtherAnother(5).OtherStrProp;
				Assert.That(str1, Is.EqualTo("OtherAnotherStrValue5"));

				Assert.Throws<NotImplementedException>(() => wrapper.GetOther(10));

				var obj = (Dynamic.OtherClass)wrapper.Evaluate(w => w.GetOther(10));
				Assert.That(obj.GetType(), Is.EqualTo(typeof(Dynamic.OtherClass)));
			}

			[Test]
			public void TestNew()
			{
				var typeMapper = CreateTypeMapper();

				var newExpression = typeMapper.MapExpression(() => new SampleClass(55, 77));
				
				var newLambda = Expression.Lambda<Func<Dynamic.SampleClass>>(newExpression);
				var instance = newLambda.Compile()();

				Assert.That(instance.Id, Is.EqualTo(55));
				Assert.That(instance.Value, Is.EqualTo(77));
			}

			[Test]
			public void TestMemberInit()
			{
				var typeMapper = CreateTypeMapper();

				var newMemberInit = typeMapper.MapExpression(() => new SampleClass(55, 77) {StrValue = "Str"});
				var memberInitLambda = Expression.Lambda<Func<Dynamic.SampleClass>>(newMemberInit);

				var instance = memberInitLambda.Compile()();

				Assert.That(instance.Id, Is.EqualTo(55));
				Assert.That(instance.Value, Is.EqualTo(77));
				Assert.That(instance.StrValue, Is.EqualTo("Str"));
			}

			[Test]
			public void TestMapFunc()
			{
				var typeMapper = CreateTypeMapper();

				var newMemberInit = typeMapper.MapLambda((int i) => new SampleClass(i + 55, i + 77) {StrValue = "Str"});
				var func = typeMapper.BuildFunc<byte, object>(newMemberInit);
				
				var instance = (Dynamic.SampleClass)func(1);

				Assert.That(instance.Id, Is.EqualTo(56));
				Assert.That(instance.Value, Is.EqualTo(78));
				Assert.That(instance.StrValue, Is.EqualTo("Str"));
			}

			[Test]
			public void TesWrapper()
			{
				var typeMapper = CreateTypeMapper();

				var wrapper = typeMapper.CreateAndWrap(() => new SampleClass(1, 2));

				wrapper.SomeAction();
				Assert.That(wrapper.Value, Is.EqualTo(3));
			}

			[Test]
			public void TesCollection()
			{
				var typeMapper = CreateTypeMapper();

				var collection = typeMapper.CreateAndWrap(() => new CollectionSample());
				var obj = typeMapper.CreateAndWrap(() => new SampleClass(1, 2));

				var same = collection.Add(obj);

				Assert.That(same.Id,    Is.EqualTo(1));
				Assert.That(same.Value, Is.EqualTo(2));
			}

			[Test]
			public void TestEvents()
			{
				var typeMapper = CreateTypeMapper();
				var wrapper  = typeMapper.CreateAndWrap(() => new SampleClass(1, 2));
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

			[Test]
			public void TestEnums()
			{
				var typeMapper = CreateTypeMapper();

				var wrapper  = typeMapper.CreateAndWrap(() => new SampleClass(1, 2));

				// test in methods
				//
				// non-flags enum mapping
				Assert.AreEqual(RegularEnum.One,   wrapper.GetRegularEnum(1));
				Assert.AreEqual(RegularEnum.Two,   wrapper.GetRegularEnum(2));
				Assert.AreEqual(RegularEnum.Three, wrapper.GetRegularEnum(3));
				Assert.AreEqual(1, wrapper.SetRegularEnum(RegularEnum.One));
				Assert.AreEqual(2, wrapper.SetRegularEnum(RegularEnum.Two));
				Assert.AreEqual(3, wrapper.SetRegularEnum(RegularEnum.Three));

				// flags enum mapping
				Assert.AreEqual(FlagsEnum.Bit1,                  wrapper.GetFlagsEnum(1));
				Assert.AreEqual(FlagsEnum.Bit3,                  wrapper.GetFlagsEnum(4));
				Assert.AreEqual(FlagsEnum.Bits24,                wrapper.GetFlagsEnum(10));
				Assert.AreEqual(FlagsEnum.Bit1 | FlagsEnum.Bit3, wrapper.GetFlagsEnum(5));
				Assert.AreEqual(1,  wrapper.SetFlagsEnum(FlagsEnum.Bit1));
				Assert.AreEqual(4,  wrapper.SetFlagsEnum(FlagsEnum.Bit3));
				Assert.AreEqual(10, wrapper.SetFlagsEnum(FlagsEnum.Bits24));
				Assert.AreEqual(5,  wrapper.SetFlagsEnum(FlagsEnum.Bit1 | FlagsEnum.Bit3));


				// test in properties
				//
				// non-flags enum mapping
				Assert.AreEqual(RegularEnum.Two, wrapper.RegularEnumProperty);
				wrapper.RegularEnumProperty = RegularEnum.One;
				Assert.AreEqual(RegularEnum.One, wrapper.RegularEnumProperty);

				// flags enum mapping
				Assert.AreEqual(FlagsEnum.Bit3, wrapper.FlagsEnumProperty);
				wrapper.FlagsEnumProperty = FlagsEnum.Bits24;
				Assert.AreEqual(FlagsEnum.Bits24, wrapper.FlagsEnumProperty);

				// using setters/getters
				var typeBuilder = typeMapper.Type<SampleClass>();
				var regularEnumBuilder = typeBuilder.Member(p => p.RegularEnumProperty);
				var flagsEnumBuilder   = typeBuilder.Member(p => p.FlagsEnumProperty);

				var regularSetter = regularEnumBuilder.BuildSetter<Dynamic.SampleClass>();
				var regularGetter = regularEnumBuilder.BuildGetter<Dynamic.SampleClass>();

				var flagsSetter = flagsEnumBuilder.BuildSetter<Dynamic.SampleClass>();
				var flagsGetter = flagsEnumBuilder.BuildGetter<Dynamic.SampleClass>();

				// reset instance
				wrapper = typeMapper.CreateAndWrap(() => new SampleClass(1, 2));
				var instance = (Dynamic.SampleClass)wrapper.instance_;

				// non-flags enum mapping
				Assert.AreEqual(RegularEnum.Two, regularGetter(instance));
				regularSetter(instance, RegularEnum.One);
				Assert.AreEqual(RegularEnum.One, regularGetter(instance));

				// flags enum mapping
				Assert.AreEqual(FlagsEnum.Bit3, flagsGetter(instance));
				flagsSetter(instance, FlagsEnum.Bits24);
				Assert.AreEqual(FlagsEnum.Bits24, flagsGetter(instance));
			}
		}
	}
}
