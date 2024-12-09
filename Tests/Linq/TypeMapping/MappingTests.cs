using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Internal;

using NUnit.Framework;

namespace Tests.TypeMapping
{
	namespace Dynamic
	{
		public delegate void        SimpleDelegate              (string? input);
		public delegate void        SimpleDelegateWithMapping   (SampleClass input);
		public delegate string      ReturningDelegate           (string input);
		public delegate SampleClass ReturningDelegateWithMapping(SampleClass input);

		public interface ISampleClass
		{
		}

		public class SampleClass : ISampleClass
		{
			public int     Id       { get; set; }
			public int     Value    { get; set; }
			public string? StrValue { get; set; }

			public OtherClass GetOther       (int idx) => new () { OtherStrProp = "OtherStrValue" + idx        };
			public OtherClass GetOtherAnother(int idx) => new () { OtherStrProp = "OtherAnotherStrValue" + idx };

			public void SomeAction() => ++Value;

			public event SimpleDelegate?               SimpleDelegateEvent;
			public event SimpleDelegateWithMapping?    SimpleDelegateWithMappingEvent;
			// just test-case, nobody in their mind should use returning events
			public event ReturningDelegate?            ReturningDelegateEvent;
			public event ReturningDelegateWithMapping? ReturningDelegateWithMappingEvent;

			public void DebugFire()
			{
				SimpleDelegateEvent?.Invoke(null);
			}

			public void Fire(bool withHandlers)
			{
				// https://www.youtube.com/watch?v=r32LcBqiv7I
				SimpleDelegateEvent?.Invoke("param1");
				SimpleDelegateWithMappingEvent?.Invoke(new SampleClass() { Id = 5 });
				var strResult  = ReturningDelegateEvent?.Invoke("event1");
				var thisResult = ReturningDelegateWithMappingEvent?.Invoke(this);

				if (withHandlers)
				{
					Assert.Multiple(() =>
					{
						Assert.That(strResult, Is.EqualTo("event1"));
						Assert.That(thisResult, Is.EqualTo(this));
					});
				}
				else
				{
					Assert.Multiple(() =>
					{
						Assert.That(strResult, Is.Null);
						Assert.That(thisResult, Is.Null);
					});
				}
			}

			public SampleClass()
			{
			}

			public SampleClass(int id, int value)
			{
				Id    = id;
				Value = value;
			}

			public Task<SampleClass> GetSelfAsync(CancellationToken cancellationToken) => Task.FromResult(this);

			public RegularEnum1 GetRegularEnum1(int raw) => (RegularEnum1)raw;
			public RegularEnum2 GetRegularEnum2(int raw) => (RegularEnum2)raw;
			public FlagsEnum    GetFlagsEnum   (int raw) => (FlagsEnum   )raw;

			public int SetRegularEnum1(RegularEnum1 val) => (int)val;
			public int SetRegularEnum2(RegularEnum2 val) => (int)val;
			public int SetFlagsEnum   (FlagsEnum    val) => (int)val;

			public RegularEnum1 RegularEnum1Property { get; set; } = RegularEnum1.Two;
			public RegularEnum2 RegularEnum2Property { get; set; } = RegularEnum2.Two;
			public FlagsEnum   FlagsEnumProperty     { get; set; } = FlagsEnum   .Bit3;

			public string MethodWithRemappedName(string value) => value;
			public int MethodWithWrongReturnType(string value) => value.Length;

			public string ReturnTypeMapper(string value) => value;
		}

		public static class SampleClassExtensions
		{
			public static string? GetOtherStr(this SampleClass sc, int idx)
			{
				return sc.GetOther(idx).OtherStrProp;
			}
		}

		public class OtherClass
		{
			public string? OtherStrProp { get; set; }
		}

		public class CollectionSample : CollectionBase
		{
			public SampleClass Add(SampleClass sample)
			{
				return sample;
			}
		}

		public enum RegularEnum1
		{
			One   = 1,
			Two   = 2,
			Three = 3
		}

		public enum RegularEnum2
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

		internal sealed class SqlError
		{
			public SqlError()
			{
			}
		}

		[Wrapper]
		internal sealed class SqlErrorCollection : IEnumerable
		{
			private List<object> _errors = new ()
			{
				new SqlError(),
				new SqlError()
			};

			public IEnumerator GetEnumerator()
			{
				return _errors.GetEnumerator();
			}
		}
	}
	
	namespace Wrappers
	{
		[Wrapper] delegate void        SimpleDelegate              (string input);
		[Wrapper] delegate void        SimpleDelegateWithMapping   (SampleClass input);
		[Wrapper] delegate string      ReturningDelegate           (string input);
		[Wrapper] delegate SampleClass ReturningDelegateWithMapping(SampleClass input);

		sealed class StringToIntMapper : ICustomMapper
		{
			bool ICustomMapper.CanMap(Expression expression)
			{
				return expression.Type == typeof(string);
			}

			Expression ICustomMapper.Map(Expression expression)
			{
				return Expression.Property(expression, "Length");
			}
		}

		sealed class SampleClass : TypeWrapper
		{
			private static object[] Wrappers { get; }
				= new object[]
			{
				// [0]: get Id
				(Expression<Func<SampleClass, int>>)((SampleClass this_) => this_.Id),
				// [1]: get Value
				(Expression<Func<SampleClass, int>>)((SampleClass this_) => this_.Value),
				// [2]: GetOtherAnother
				(Expression<Func<SampleClass, int, OtherClass>>)((SampleClass this_, int idx) => this_.GetOtherAnother(idx)),
				// [3]: SomeAction
				(Expression<Action<SampleClass>>)((SampleClass this_) => this_.SomeAction()),
				// [4]: GetRegularEnum1
				(Expression<Func<SampleClass, int, RegularEnum1>>)((SampleClass this_, int raw) => this_.GetRegularEnum1(raw)),
				// [5]: GetFlagsEnum
				(Expression<Func<SampleClass, int, FlagsEnum>>)((SampleClass this_, int raw) => this_.GetFlagsEnum(raw)),
				// [6]: SetRegularEnum1
				(Expression<Func<SampleClass, RegularEnum1, int>>)((SampleClass this_, RegularEnum1 val) => this_.SetRegularEnum1(val)),
				// [7]: SetFlagsEnum
				(Expression<Func<SampleClass, FlagsEnum, int>>)((SampleClass this_, FlagsEnum val) => this_.SetFlagsEnum(val)),
				// [8]: get RegularEnum1Property
				(Expression<Func<SampleClass, RegularEnum1>>)((SampleClass this_) => this_.RegularEnum1Property),
				// [9]: get FlagsEnumProperty
				(Expression<Func<SampleClass, FlagsEnum>>)((SampleClass this_) => this_.FlagsEnumProperty),
				// [10]: Fire
				(Expression<Action<SampleClass, bool>>)((SampleClass this_,bool withHandlers) => this_.Fire(withHandlers)),
				// [11]: set RegularEnum1Property
				PropertySetter((SampleClass this_) => this_.RegularEnum1Property),
				// [12]: set FlagsEnumProperty
				PropertySetter((SampleClass this_) => this_.FlagsEnumProperty),
				// [13]: GetRegularEnum2
				(Expression<Func<SampleClass, int, RegularEnum2>>)((SampleClass this_, int raw) => this_.GetRegularEnum2(raw)),
				// [14]: SetRegularEnum2
				(Expression<Func<SampleClass, RegularEnum2, int>>)((SampleClass this_, RegularEnum2 val) => this_.SetRegularEnum2(val)),
				// [15]: get RegularEnum2Property
				(Expression<Func<SampleClass, RegularEnum2>>)((SampleClass this_) => this_.RegularEnum2Property),
				// [16]: set RegularEnum2Property
				PropertySetter((SampleClass this_) => this_.RegularEnum2Property),
				// [17]: set MethodWithRemappedName
				new Tuple<LambdaExpression, bool>
					((Expression<Func<SampleClass, string, string>>     )((SampleClass this_, string value) => this_.MethodWithRemappedName2(value)), true),
				// [18]: set MethodWithWrongReturnType
				new Tuple<LambdaExpression, bool>
					((Expression<Func<SampleClass, string, string>>     )((SampleClass this_, string value) => this_.MethodWithWrongReturnType(value)), true),
				// [19]: set ReturnTypeMapper
				new Tuple<LambdaExpression, bool>
					((Expression<Func<SampleClass, string, int>>        )((SampleClass this_, string value) => this_.ReturnTypeMapper(value)), true),
			};

			private static string[] Events { get; }
				= new[]
			{
				nameof(SimpleDelegateEvent),
				nameof(SimpleDelegateWithMappingEvent),
				nameof(ReturningDelegateEvent),
				nameof(ReturningDelegateWithMappingEvent),
			};

			public int     Id    => ((Func<SampleClass, int>)CompiledWrappers[0])(this);
			public int     Value => ((Func<SampleClass, int>)CompiledWrappers[1])(this);
			public string? StrValue { get; set; }

			public OtherClass GetOther       (int idx) => throw new NotImplementedException();
			public OtherClass GetOtherAnother(int idx) => ((Func<SampleClass, int, OtherClass>)CompiledWrappers[2])(this, idx);

			public void SomeAction() => ((Action<SampleClass>)CompiledWrappers[3])(this);

			public RegularEnum1 GetRegularEnum1(int raw) => ((Func<SampleClass, int, RegularEnum1>)CompiledWrappers[4])(this, raw);
			public RegularEnum2 GetRegularEnum2(int raw) => ((Func<SampleClass, int, RegularEnum2>)CompiledWrappers[13])(this, raw);
			public FlagsEnum    GetFlagsEnum   (int raw) => ((Func<SampleClass, int, FlagsEnum>)CompiledWrappers[5])(this, raw);

			public int SetRegularEnum1(RegularEnum1 val) => ((Func<SampleClass, RegularEnum1, int>)CompiledWrappers[6])(this, val);
			public int SetRegularEnum2(RegularEnum2 val) => ((Func<SampleClass, RegularEnum2, int>)CompiledWrappers[14])(this, val);
			public int SetFlagsEnum   (FlagsEnum    val) => ((Func<SampleClass, FlagsEnum, int>)CompiledWrappers[7])(this, val);

			[TypeWrapperName("MethodWithRemappedName")]
			public string MethodWithRemappedName2(string value) => ((Func<SampleClass, string, string>)CompiledWrappers[17])(this, value);
			public string MethodWithWrongReturnType(string value) => ((Func<SampleClass, string, string>)CompiledWrappers[18])(this, value);

			public bool HasMethodWithWrongReturnType => CompiledWrappers[18] != null;

			[return: CustomMapper(typeof(StringToIntMapper))]
			public int ReturnTypeMapper(string value) => ((Func<SampleClass, string, int>)CompiledWrappers[19])(this, value);

			public Task<SampleClass> GetSelfAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

			public RegularEnum1 RegularEnum1Property
			{
				get => ((Func<SampleClass, RegularEnum1  >)CompiledWrappers[8])(this);
				set => ((Action<SampleClass, RegularEnum1>)CompiledWrappers[11])(this, value);
			}

			public RegularEnum2 RegularEnum2Property
			{
				get => ((Func<SampleClass, RegularEnum2  >)CompiledWrappers[15])(this);
				set => ((Action<SampleClass, RegularEnum2>)CompiledWrappers[16])(this, value);
			}

			public FlagsEnum FlagsEnumProperty
			{
				get => ((Func<SampleClass, FlagsEnum  >)CompiledWrappers[9])(this);
				set => ((Action<SampleClass, FlagsEnum>)CompiledWrappers[12])(this, value);
			}

			public void Fire(bool withHandlers) => ((Action<SampleClass, bool>)CompiledWrappers[10])(this, withHandlers);

			private      SimpleDelegate? _SimpleDelegateEvent;
			public event SimpleDelegate?  SimpleDelegateEvent
			{
				add    => _SimpleDelegateEvent = (SimpleDelegate?)Delegate.Combine(_SimpleDelegateEvent, value);
				remove => _SimpleDelegateEvent = (SimpleDelegate?)Delegate.Remove (_SimpleDelegateEvent, value);
			}

			private      SimpleDelegateWithMapping? _SimpleDelegateWithMappingEvent;
			public event SimpleDelegateWithMapping?  SimpleDelegateWithMappingEvent
			{
				add    => _SimpleDelegateWithMappingEvent = (SimpleDelegateWithMapping?)Delegate.Combine(_SimpleDelegateWithMappingEvent, value);
				remove => _SimpleDelegateWithMappingEvent = (SimpleDelegateWithMapping?)Delegate.Remove (_SimpleDelegateWithMappingEvent, value);
			}

			private      ReturningDelegate? _ReturningDelegateEvent;
			public event ReturningDelegate?  ReturningDelegateEvent
			{
				add    => _ReturningDelegateEvent = (ReturningDelegate?)Delegate.Combine(_ReturningDelegateEvent, value);
				remove => _ReturningDelegateEvent = (ReturningDelegate?)Delegate.Remove (_ReturningDelegateEvent, value);
			}

			private      ReturningDelegateWithMapping? _ReturningDelegateWithMappingEvent;
			public event ReturningDelegateWithMapping?  ReturningDelegateWithMappingEvent
			{
				add    => _ReturningDelegateWithMappingEvent = (ReturningDelegateWithMapping?)Delegate.Combine(_ReturningDelegateWithMappingEvent, value);
				remove => _ReturningDelegateWithMappingEvent = (ReturningDelegateWithMapping?)Delegate.Remove (_ReturningDelegateWithMappingEvent, value);
			}

			public SampleClass(object instance, Delegate[] delegates) : base(instance, delegates)
			{
			}

			public SampleClass(int id, int value) => throw new NotImplementedException();
		}

		[Wrapper]
		internal static class SampleClassExtensions
		{
			internal static string GetOtherStr(this SampleClass sc, int idx) => throw new NotImplementedException();
		}

		sealed class OtherClass : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get OtherStrProp
				(Expression<Func<OtherClass, string>>)((OtherClass this_) => this_.OtherStrProp),
			};

			public string OtherStrProp => ((Func<OtherClass, string>)CompiledWrappers[0])(this);

			public OtherClass(object instance, Delegate[] delegates) : base(instance, delegates)
			{
			}
		}

		sealed class CollectionSample : TypeWrapper
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

			public CollectionSample(object instance, Delegate[] delegates) : base(instance, delegates)
			{
			}

			public SampleClass Add(SampleClass sample) => ((Func<CollectionSample, SampleClass, SampleClass>)CompiledWrappers[0])(this, sample);
		}

		[Wrapper]
		internal sealed class SqlError : TypeWrapper
		{
			public SqlError(object instance) : base(instance, null)
			{
			}
		}

		[Wrapper]
		internal sealed class SqlErrorCollection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: GetEnumerator
				(Expression<Func<SqlErrorCollection, IEnumerator>>)((SqlErrorCollection this_) => this_.GetEnumerator()),
				// [1]: SqlError wrapper
				(Expression<Func<object, SqlError>>)((object error) => (SqlError)error),
			};

			public SqlErrorCollection()
			{
			}

			public SqlErrorCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public IEnumerator GetEnumerator() => ((Func<SqlErrorCollection, IEnumerator>)CompiledWrappers[0])(this);

			public IEnumerable<SqlError> Errors
			{
				get
				{
					var wrapper = (Func<object, SqlError>)CompiledWrappers[1];
					var e = GetEnumerator();

					while (e.MoveNext())
						yield return wrapper(e.Current!);
				}
			}
		}

		// same values
		[Wrapper]
		public enum RegularEnum1
		{
			One   = 1,
			Two   = 2,
			Three = 3
		}

		// different values
		[Wrapper]
		public enum RegularEnum2
		{
			One   = 3,
			Two   = 1,
			Three = 2
		}

		[Wrapper, Flags]
		public enum FlagsEnum
		{
			Bit1   = 1,
			Bit3   = 4,
			Bits24 = 10
		}

		[TestFixture]
		public class MappingTests : TestBase
		{
			private TypeMapper CreateTypeMapper()
			{
				var typeMapper = new TypeMapper();

				typeMapper.RegisterTypeWrapper<SampleClass                 >(typeof(Dynamic.SampleClass));
				typeMapper.RegisterTypeWrapper<OtherClass                  >(typeof(Dynamic.OtherClass));
				typeMapper.RegisterTypeWrapper<CollectionSample            >(typeof(Dynamic.CollectionSample));
				typeMapper.RegisterTypeWrapper<SimpleDelegate              >(typeof(Dynamic.SimpleDelegate));
				typeMapper.RegisterTypeWrapper<SimpleDelegateWithMapping   >(typeof(Dynamic.SimpleDelegateWithMapping));
				typeMapper.RegisterTypeWrapper<ReturningDelegate           >(typeof(Dynamic.ReturningDelegate));
				typeMapper.RegisterTypeWrapper<ReturningDelegateWithMapping>(typeof(Dynamic.ReturningDelegateWithMapping));
				typeMapper.RegisterTypeWrapper<RegularEnum1                >(typeof(Dynamic.RegularEnum1));
				typeMapper.RegisterTypeWrapper<RegularEnum2                >(typeof(Dynamic.RegularEnum2));
				typeMapper.RegisterTypeWrapper<FlagsEnum                   >(typeof(Dynamic.FlagsEnum));
				typeMapper.RegisterTypeWrapper<SqlError                    >(typeof(Dynamic.SqlError));
				typeMapper.RegisterTypeWrapper<SqlErrorCollection          >(typeof(Dynamic.SqlErrorCollection));
				typeMapper.RegisterTypeWrapper(typeof(SampleClassExtensions), typeof(Dynamic.SampleClassExtensions));

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


				var cl1 = (Func<Dynamic.SampleClass, int>)l1.CompileExpression();
				var cl2 = (Func<Dynamic.SampleClass, int>)l2.CompileExpression();
				var cl3 = (Func<Dynamic.SampleClass, int, Dynamic.OtherClass>)l3.CompileExpression();
				var cl4 = (Func<Dynamic.SampleClass, int, string>)l4.CompileExpression();

				Assert.Multiple(() =>
				{
					Assert.That(cl1(concrete), Is.EqualTo(33));
					Assert.That(cl2(concrete), Is.EqualTo(1));
					Assert.That(cl3(concrete, 11).OtherStrProp, Is.EqualTo("OtherStrValue11"));
					Assert.That(cl4(concrete, 22), Is.EqualTo("OtherStrValue22"));
				});

				var dynamicInstance = (object)concrete;

				var wrapper = typeMapper.Wrap<SampleClass>(dynamicInstance);

				var str1 = wrapper.GetOtherAnother(5).OtherStrProp;
				Assert.That(str1, Is.EqualTo("OtherAnotherStrValue5"));

				Assert.Throws<NotImplementedException>(() => wrapper.GetOther(10));
			}

			[Test]
			public void TestNew()
			{
				var typeMapper = CreateTypeMapper();

				var newExpression = typeMapper.MapExpression(() => new SampleClass(55, 77));
				var newLambda     = Expression.Lambda<Func<Dynamic.SampleClass>>(newExpression);
				var instance      = newLambda.CompileExpression()();

				Assert.Multiple(() =>
				{
					Assert.That(instance.Id, Is.EqualTo(55));
					Assert.That(instance.Value, Is.EqualTo(77));
				});
			}

			[Test]
			public void TestMemberInit()
			{
				var typeMapper = CreateTypeMapper();

				var newMemberInit    = typeMapper.MapExpression(() => new SampleClass(55, 77) {StrValue = "Str"});
				var memberInitLambda = Expression.Lambda<Func<Dynamic.SampleClass>>(newMemberInit);

				var instance = memberInitLambda.CompileExpression()();

				Assert.Multiple(() =>
				{
					Assert.That(instance.Id, Is.EqualTo(55));
					Assert.That(instance.Value, Is.EqualTo(77));
					Assert.That(instance.StrValue, Is.EqualTo("Str"));
				});
			}

			[Test]
			public async Task TestMapTaskMethod()
			{
				var typeMapper = CreateTypeMapper();

				var pInstance = Expression.Parameter(typeof(Dynamic.ISampleClass));
				var pToken    = Expression.Parameter(typeof(CancellationToken));

				var asyncCall = Expression.Lambda<Func<Dynamic.ISampleClass, CancellationToken, Task<SampleClass>>>(
					typeMapper.MapExpression((Dynamic.ISampleClass instance, CancellationToken cancellationToken) => typeMapper.WrapTask<SampleClass>(((SampleClass)(object)instance).GetSelfAsync(cancellationToken), typeof(Dynamic.SampleClass), cancellationToken), pInstance, pToken),
					pInstance, pToken);

				var instance = await asyncCall.CompileExpression()(new Dynamic.SampleClass(55, 77) {StrValue = "Str"}, default);

				Assert.Multiple(() =>
				{
					Assert.That(instance.Id, Is.EqualTo(55));
					Assert.That(instance.Value, Is.EqualTo(77));
					Assert.That(instance.StrValue, Is.Null);
				});
			}

			[Test]
			public void TestMapFunc()
			{
				var typeMapper = CreateTypeMapper();

				var newMemberInit = typeMapper.MapLambda((int i) => new SampleClass(i + 55, i + 77) {StrValue = "Str"});
				var func          = typeMapper.BuildFunc<byte, object>(newMemberInit);
				
				var instance = (Dynamic.SampleClass)func(1);

				Assert.Multiple(() =>
				{
					Assert.That(instance.Id, Is.EqualTo(56));
					Assert.That(instance.Value, Is.EqualTo(78));
					Assert.That(instance.StrValue, Is.EqualTo("Str"));
				});
			}

			[Test]
			public void TesWrapper()
			{
				var typeMapper = CreateTypeMapper();

				var wrapper = typeMapper.BuildWrappedFactory(() => new SampleClass(1, 2))();

				wrapper.SomeAction();
				Assert.That(wrapper.Value, Is.EqualTo(3));
			}

			[Test]
			public void TesCollection()
			{
				var typeMapper = CreateTypeMapper();

				var collection = typeMapper.BuildWrappedFactory(() => new CollectionSample())();
				var obj        = typeMapper.BuildWrappedFactory(() => new SampleClass(1, 2))();

				var same = collection.Add(obj);

				Assert.Multiple(() =>
				{
					Assert.That(same.Id, Is.EqualTo(1));
					Assert.That(same.Value, Is.EqualTo(2));
				});
			}

			[Test]
			public void TestEvents()
			{
				var typeMapper = CreateTypeMapper();
				var wrapper    = typeMapper.BuildWrappedFactory(() => new SampleClass(1, 2))();
				var instance   = (Dynamic.SampleClass)wrapper.instance_;

				// no subscribers
				wrapper.Fire(false);

				// subscribed
				string? strValue1 = null;
				SampleClass? thisValue1 = null;
				wrapper.SimpleDelegateEvent                    += handler1;
				wrapper.SimpleDelegateWithMappingEvent         += handler2;
				wrapper.ReturningDelegateEvent                 += handler3;
				wrapper.ReturningDelegateWithMappingEvent      += handler4;
				wrapper.Fire(true);

				Assert.Multiple(() =>
				{
					Assert.That(strValue1, Is.EqualTo("param1"));
					Assert.That(((Dynamic.SampleClass)thisValue1!.instance_).Id, Is.EqualTo(5));
				});


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

				var wrapper  = typeMapper.BuildWrappedFactory(() => new SampleClass(1, 2))();

				Assert.Multiple(() =>
				{
					// test in methods
					//
					// non-flags enum mapping
					Assert.That(wrapper.GetRegularEnum1(1), Is.EqualTo(RegularEnum1.One));
					Assert.That(wrapper.GetRegularEnum1(2), Is.EqualTo(RegularEnum1.Two));
					Assert.That(wrapper.GetRegularEnum1(3), Is.EqualTo(RegularEnum1.Three));
					Assert.That(wrapper.GetRegularEnum2(1), Is.EqualTo(RegularEnum2.One));
					Assert.That(wrapper.GetRegularEnum2(2), Is.EqualTo(RegularEnum2.Two));
					Assert.That(wrapper.GetRegularEnum2(3), Is.EqualTo(RegularEnum2.Three));
					Assert.That(wrapper.SetRegularEnum1(RegularEnum1.One), Is.EqualTo(1));
					Assert.That(wrapper.SetRegularEnum1(RegularEnum1.Two), Is.EqualTo(2));
					Assert.That(wrapper.SetRegularEnum1(RegularEnum1.Three), Is.EqualTo(3));
					Assert.That(wrapper.SetRegularEnum2(RegularEnum2.One), Is.EqualTo(1));
					Assert.That(wrapper.SetRegularEnum2(RegularEnum2.Two), Is.EqualTo(2));
					Assert.That(wrapper.SetRegularEnum2(RegularEnum2.Three), Is.EqualTo(3));

					// flags enum mapping
					Assert.That(wrapper.GetFlagsEnum(1), Is.EqualTo(FlagsEnum.Bit1));
					Assert.That(wrapper.GetFlagsEnum(4), Is.EqualTo(FlagsEnum.Bit3));
					Assert.That(wrapper.GetFlagsEnum(10), Is.EqualTo(FlagsEnum.Bits24));
					Assert.That(wrapper.GetFlagsEnum(5), Is.EqualTo(FlagsEnum.Bit1 | FlagsEnum.Bit3));
					Assert.That(wrapper.SetFlagsEnum(FlagsEnum.Bit1), Is.EqualTo(1));
					Assert.That(wrapper.SetFlagsEnum(FlagsEnum.Bit3), Is.EqualTo(4));
					Assert.That(wrapper.SetFlagsEnum(FlagsEnum.Bits24), Is.EqualTo(10));
					Assert.That(wrapper.SetFlagsEnum(FlagsEnum.Bit1 | FlagsEnum.Bit3), Is.EqualTo(5));


					// test in properties
					//
					// non-flags enum mapping
					Assert.That(wrapper.RegularEnum1Property, Is.EqualTo(RegularEnum1.Two));
				});
				wrapper.RegularEnum1Property = RegularEnum1.One;
				Assert.Multiple(() =>
				{
					Assert.That(wrapper.RegularEnum1Property, Is.EqualTo(RegularEnum1.One));
					Assert.That(wrapper.RegularEnum2Property, Is.EqualTo(RegularEnum2.Two));
				});
				wrapper.RegularEnum2Property = RegularEnum2.One;
				Assert.Multiple(() =>
				{
					Assert.That(wrapper.RegularEnum2Property, Is.EqualTo(RegularEnum2.One));

					// flags enum mapping
					Assert.That(wrapper.FlagsEnumProperty, Is.EqualTo(FlagsEnum.Bit3));
				});
				wrapper.FlagsEnumProperty = FlagsEnum.Bits24;
				Assert.That(wrapper.FlagsEnumProperty, Is.EqualTo(FlagsEnum.Bits24));

				// using setters/getters
				var typeBuilder         = typeMapper.Type<SampleClass>();
				var regularEnum1Builder = typeBuilder.Member(p => p.RegularEnum1Property);
				var regularEnum2Builder = typeBuilder.Member(p => p.RegularEnum2Property);
				var flagsEnumBuilder    = typeBuilder.Member(p => p.FlagsEnumProperty);

				var regular1Setter = regularEnum1Builder.BuildSetter<Dynamic.SampleClass>();
				var regular1Getter = regularEnum1Builder.BuildGetter<Dynamic.SampleClass>();
				var regular2Setter = regularEnum2Builder.BuildSetter<Dynamic.SampleClass>();
				var regular2Getter = regularEnum2Builder.BuildGetter<Dynamic.SampleClass>();

				var flagsSetter = flagsEnumBuilder.BuildSetter<Dynamic.SampleClass>();
				var flagsGetter = flagsEnumBuilder.BuildGetter<Dynamic.SampleClass>();

				// reset instance
				wrapper      = typeMapper.BuildWrappedFactory(() => new SampleClass(1, 2))();
				var instance = (Dynamic.SampleClass)wrapper.instance_;

				// non-flags enum mapping
				Assert.That(regular1Getter(instance), Is.EqualTo(RegularEnum1.Two));
				regular1Setter(instance, RegularEnum1.One);
				Assert.Multiple(() =>
				{
					Assert.That(regular1Getter(instance), Is.EqualTo(RegularEnum1.One));
					Assert.That(regular2Getter(instance), Is.EqualTo(RegularEnum2.Two));
				});
				regular2Setter(instance, RegularEnum2.One);
				Assert.Multiple(() =>
				{
					Assert.That(regular2Getter(instance), Is.EqualTo(RegularEnum2.One));

					// flags enum mapping
					Assert.That(flagsGetter(instance), Is.EqualTo(FlagsEnum.Bit3));
				});
				flagsSetter(instance, FlagsEnum.Bits24);
				Assert.That(flagsGetter(instance), Is.EqualTo(FlagsEnum.Bits24));
			}

			[Test]
			public void TestSqlErrorCollectionMapping()
			{
				var typeMapper = CreateTypeMapper();

				var wrapped = typeMapper.BuildWrappedFactory(() => new SqlErrorCollection())();
				var errors  = wrapped.Errors.ToArray();

				Assert.That(errors, Has.Length.EqualTo(2));
			}

			[Test]
			public void TestMethodNameAttribute()
			{
				var typeMapper = CreateTypeMapper();

				var wrapped = typeMapper.BuildWrappedFactory(() => new SampleClass(1, 2))();
				var res = wrapped.MethodWithRemappedName2("value");

				Assert.That(res, Is.EqualTo("value"));
			}

			[Test]
			public void TestMethodWithWrongReturnType()
			{
				var typeMapper = CreateTypeMapper();

				var wrapped = typeMapper.BuildWrappedFactory(() => new SampleClass(1, 2))();

				Assert.That(wrapped.HasMethodWithWrongReturnType, Is.False);
			}

			[Test]
			public void TestReturnTypeMapper()
			{
				var typeMapper = CreateTypeMapper();

				var wrapped = typeMapper.BuildWrappedFactory(() => new SampleClass(1, 2))();

				Assert.That(wrapped.ReturnTypeMapper("test"), Is.EqualTo(4));
			}

			[Test]
			public void ValueTaskToTaskMapperTest1()
			{
				var taskExpression = Expression.Constant(new ValueTask<long>());
				var mapper         = new ValueTaskToTaskMapper();
				var result         = ((ICustomMapper)mapper).Map(taskExpression);

				Assert.Multiple(() =>
				{
					Assert.That(result.Type, Is.EqualTo(typeof(Task<long>)));
					Assert.That(typeof(Task<long>).IsAssignableFrom(result.EvaluateExpression()!.GetType()), Is.True);
				});
			}

			[Test]
			public void ValueTaskToTaskMapperTest2()
			{
				var taskExpression = Expression.Constant(new ValueTask());
				var mapper         = new ValueTaskToTaskMapper();
				var result         = ((ICustomMapper)mapper).Map(taskExpression);

				Assert.Multiple(() =>
				{
					Assert.That(result.Type, Is.EqualTo(typeof(Task)));
					Assert.That(typeof(Task).IsAssignableFrom(result.EvaluateExpression()!.GetType()), Is.True);
				});
			}

			[Test]
			public void ValueTaskToTaskMapperTest3()
			{
				var taskExpression = Expression.Constant(0);
				var mapper         = new ValueTaskToTaskMapper();

				Assert.That(((ICustomMapper)mapper).CanMap(taskExpression), Is.False);
			}
		}
	}
}
