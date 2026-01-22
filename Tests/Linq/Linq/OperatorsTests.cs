#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CA1046 // Do not overload equality operator on reference types
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class OperatorsTests : TestBase
	{
		// 1. remote disabled as we don't have serialization configured for custom types
		// 2. tests don't put operator mappings on custom types directly to ensure we can map 3rd-party code
		// 3. test different mapping approaches except Expressions.Map* APIs as they are obsoleted
		const bool WITH_REMOTE = false;

		struct CustomInt
		{
			public int Value;

			public static implicit operator CustomInt(int value) => new CustomInt() { Value = value };
			// long conversion needed for sqlite
			public static implicit operator CustomInt(long value) => new CustomInt() { Value = (int)value };

			public static bool operator ==(CustomInt left, int right) => left.Value == right;
			public static bool operator !=(CustomInt left, int right) => left.Value != right;

			public static bool operator ==(CustomInt? left, int right) => left?.Value == right;
			public static bool operator !=(CustomInt? left, int right) => left?.Value != right;

			public static CustomInt operator +(CustomInt left, int right) => new CustomInt() { Value = left.Value + right };
			public static CustomInt operator *(CustomInt left, int right) => new CustomInt() { Value = left.Value * right };

			public static CustomInt operator -(CustomInt value) => new CustomInt() { Value = -value.Value };
			public static CustomInt? operator -(CustomInt? value) => value == null ? null : -value.Value;
		}

		sealed class CustomIntClass
		{
			public int Value;

			public static implicit operator CustomIntClass(int value) => new CustomIntClass() { Value = value };
			// long conversion needed for sqlite
			public static implicit operator CustomIntClass(long value) => new CustomIntClass() { Value = (int)value };

			public static bool operator ==(CustomIntClass? left, int right) => left?.Value == right;
			public static bool operator !=(CustomIntClass? left, int right) => left?.Value != right;

			public static CustomIntClass operator +(CustomIntClass left, int right) => new CustomIntClass() { Value = left.Value + right };
			public static CustomIntClass operator *(CustomIntClass left, int right) => new CustomIntClass() { Value = left.Value * right };

			public static CustomIntClass? operator -(CustomIntClass? value) => value == null ? null : new CustomIntClass() { Value = -value.Value };
		}

		sealed class OperatorTable
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Int32)] public CustomInt       Field      { get; set; }
			[Column(DataType = DataType.Int32)] public CustomInt?      FieldN     { get; set; }
			[Column(DataType = DataType.Int32)] public CustomIntClass? FieldClass { get; set; }

			public static readonly OperatorTable[] Data =
			[
				new() { Id = 1 },
				new() { Id = 2, Field = 2, FieldN = 2, FieldClass = 2 },
			];
		}

		static MappingSchema SetupMapping(Action<FluentMappingBuilder>? additionalMappings = null)
		{
			var fb = new FluentMappingBuilder();

			fb.MappingSchema.SetScalarType(typeof(CustomIntClass));
			fb.MappingSchema.SetConvertExpression<CustomInt, DataParameter>(v => new DataParameter(null, v.Value, DataType.Int32, null));
			fb.MappingSchema.SetConvertExpression<CustomIntClass?, DataParameter>(v => new DataParameter(null, v == null ? null : v.Value, DataType.Int32, null));

			additionalMappings?.Invoke(fb);

			return fb.Build().MappingSchema;
		}

		[Test]
		public void BinaryOperator_Unmapped([DataSources(WITH_REMOTE)] string context)
		{
			using var db = GetDataContext(context, SetupMapping());
			using var tb = db.CreateLocalTable(OperatorTable.Data);

			var value = 2;
			var res = tb.Where(r => r.Field == value && r.FieldN == value && r.FieldClass == value).ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0].Id, Is.EqualTo(2));
		}

		[Test]
		public void UnaryOperator_Unmapped([DataSources(WITH_REMOTE)] string context)
		{
			using var db = GetDataContext(context, SetupMapping());
			using var tb = db.CreateLocalTable(OperatorTable.Data);

			var value = -2;
			var res = tb.Where(r => -r.Field == value && -r.FieldN == value && -r.FieldClass == value).ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0].Id, Is.EqualTo(2));
		}

		[Test]
		public void BinaryOperator_Mapped_SqlExpression([DataSources(WITH_REMOTE)] string context)
		{
			using var db = GetDataContext(context, SetupMapping(fb =>
			{
				// operator== : (L + 3) == R
				fb.Entity<CustomInt>().Member(v => v == 3).HasAttribute(new Sql.ExpressionAttribute("({0} + 3) = {1}") { IsPredicate = true });
				fb.Entity<CustomInt?>().Member(v => v == 3).HasAttribute(new Sql.ExpressionAttribute("({0} + 3) = {1}") { IsPredicate = true });
				fb.Entity<CustomIntClass>().Member(v => v == 3).HasAttribute(new Sql.ExpressionAttribute("({0} + 3) = {1}") { IsPredicate = true });
			}));
			using var tb = db.CreateLocalTable(OperatorTable.Data);

			var value = 5;
			var res = tb.Where(r => r.Field == value && r.FieldN == value && r.FieldClass == value).ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0].Id, Is.EqualTo(2));
		}

		[Test]
		public void UnaryOperator_Mapped_SqlExpression([DataSources(WITH_REMOTE)] string context)
		{
			using var db = GetDataContext(context, SetupMapping(fb =>
			{
				// operator- : v*3
				fb.Entity<CustomInt>().Member(v => -v).HasAttribute(new Sql.ExpressionAttribute("3 * {0}") { Precedence = Precedence.Multiplicative });
				fb.Entity<CustomInt?>().Member(v => -v).HasAttribute(new Sql.ExpressionAttribute("3 * {0}") { Precedence = Precedence.Multiplicative });
				fb.Entity<CustomIntClass>().Member(v => -v).HasAttribute(new Sql.ExpressionAttribute("3 * {0}") { Precedence = Precedence.Multiplicative });
			}));
			using var tb = db.CreateLocalTable(OperatorTable.Data);

			var value = 6;
			var res = tb.Where(r => -r.Field == value && -r.FieldN == value && -r.FieldClass == value).ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0].Id, Is.EqualTo(2));
		}

		[Test]
		public void BinaryOperator_Mapped_MemberExpression([DataSources(WITH_REMOTE)] string context)
		{
			using var db = GetDataContext(context, SetupMapping(fb =>
			{
				// operator== : (L + 3) == R
				// Equals used intead of == to avoid infinite recursion
				fb.Entity<CustomInt>().Member(v => v == 3).HasAttribute(new ExpressionMethodAttribute((Expression<Func<CustomInt, int, bool>>)((l, r) => (l + 3).Equals(r))));
				fb.Entity<CustomInt?>().Member(v => v == 3).HasAttribute(new ExpressionMethodAttribute((Expression<Func<CustomInt?, int, bool>>)((l, r) => l != null && ((l + 3).Equals(r)))));
				fb.Entity<CustomIntClass>().Member(v => v == 3).HasAttribute(new ExpressionMethodAttribute((Expression<Func<CustomIntClass, int, bool>>)((l, r) => (l + 3).Equals(r))));
			}));
			using var tb = db.CreateLocalTable(OperatorTable.Data);

			var value = 5;
			var res = tb.Where(r => r.Field == value && r.FieldN == value && r.FieldClass == value).ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0].Id, Is.EqualTo(2));
		}

		[Test]
		public void UnaryOperator_Mapped_MemberExpression([DataSources(WITH_REMOTE)] string context)
		{
			using var db = GetDataContext(context, SetupMapping(fb =>
			{
				// operator- : v*3
				fb.Entity<CustomInt>().Member(v => -v).HasAttribute(new ExpressionMethodAttribute((Expression<Func<CustomInt, CustomInt>>)(v => v * 3)));
				fb.Entity<CustomInt?>().Member(v => -v).HasAttribute(new ExpressionMethodAttribute((Expression<Func<CustomInt?, CustomInt?>>)(v => v * 3)));
				fb.Entity<CustomIntClass>().Member(v => -v).HasAttribute(new ExpressionMethodAttribute((Expression<Func<CustomIntClass, CustomIntClass>>)(v => v * 3)));
			}));
			using var tb = db.CreateLocalTable(OperatorTable.Data);

			var value = 6;
			var res = tb.Where(r => -r.Field == value && -r.FieldN == value && -r.FieldClass == value).ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0].Id, Is.EqualTo(2));
		}

		sealed class BinaryOperatorsMemberTranslator : MemberTranslatorBase
		{
			public static readonly IMemberTranslator Instance = new BinaryOperatorsMemberTranslator();

			public BinaryOperatorsMemberTranslator()
			{
				Registration.RegisterBinaryOperator((CustomInt left, int right) => left == right, TranslateEquals);
				Registration.RegisterBinaryOperator((CustomInt? left, int right) => left == right, TranslateEquals);
				Registration.RegisterBinaryOperator((CustomIntClass left, int right) => left == right, TranslateEquals);
			}

			private Expression? TranslateEquals(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
			{
				if (translationContext.CanBeEvaluatedOnClient(binaryExpression.Left) && translationContext.CanBeEvaluatedOnClient(binaryExpression.Right))
					return null;

				if (!translationContext.TranslateToSqlExpression(binaryExpression.Left, out var leftExpr))
					return translationContext.CreateErrorExpression(binaryExpression.Left, type: binaryExpression.Method!.DeclaringType);
				if (!translationContext.TranslateToSqlExpression(binaryExpression.Right, out var rightExpr))
					return translationContext.CreateErrorExpression(binaryExpression.Right, type: binaryExpression.Method!.DeclaringType);

				var factory = translationContext.ExpressionFactory;
				var dbType  = factory.GetDbDataType(typeof(bool));

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, factory.SearchCondition().Add(factory.Equal(factory.Add(factory.GetDbDataType(leftExpr), leftExpr, factory.Value(3)), rightExpr)), binaryExpression);
			}
		}

		sealed class UnaryOperatorsMemberTranslator : MemberTranslatorBase
		{
			public static readonly IMemberTranslator Instance = new UnaryOperatorsMemberTranslator();

			public UnaryOperatorsMemberTranslator()
			{
				Registration.RegisterUnaryOperator((CustomInt value) => -value, TranslateNegate);
				Registration.RegisterUnaryOperator((CustomInt? value) => -value, TranslateNegate);
				Registration.RegisterUnaryOperator((CustomIntClass value) => -value, TranslateNegate);
			}

			private Expression? TranslateNegate(ITranslationContext translationContext, UnaryExpression unaryExpression, TranslationFlags translationFlags)
			{
				if (translationContext.CanBeEvaluatedOnClient(unaryExpression.Operand))
					return null;

				if (!translationContext.TranslateToSqlExpression(unaryExpression.Operand, out var operandExpr))
					return translationContext.CreateErrorExpression(unaryExpression.Operand, type: unaryExpression.Method!.DeclaringType);

				var factory = translationContext.ExpressionFactory;
				var dbType  = factory.GetDbDataType(operandExpr);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, factory.Multiply(dbType, operandExpr, factory.Value(3)), unaryExpression);
			}
		}

		[Test]
		public void BinaryOperator_Mapped_MemberTranslator([DataSources(WITH_REMOTE)] string context)
		{
			using var db = GetDataContext(context, o => o.UseMappingSchema(SetupMapping()).UseMemberTranslator(BinaryOperatorsMemberTranslator.Instance));
			using var tb = db.CreateLocalTable(OperatorTable.Data);

			var value = 5;
			var res = tb.Where(r => r.Field == value && r.FieldN == value && r.FieldClass == value).ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0].Id, Is.EqualTo(2));
		}

		[Test]
		public void UnaryOperator_Mapped_MemberTranslator([DataSources(WITH_REMOTE)] string context)
		{
			using var db = GetDataContext(context, o => o.UseMappingSchema(SetupMapping()).UseMemberTranslator(UnaryOperatorsMemberTranslator.Instance));
			using var tb = db.CreateLocalTable(OperatorTable.Data);

			var value = 6;
			var res = tb.Where(r => -r.Field == value && -r.FieldN == value && -r.FieldClass == value).ToList();

			Assert.That(res, Has.Count.EqualTo(1));
			Assert.That(res[0].Id, Is.EqualTo(2));
		}

		#region Issue 5254

		static class Issue5254Types
		{
			public sealed class Output
			{
				public ShortId Id { get; set; }
			}

			public readonly struct ShortId : IEquatable<ShortId>
			{
				private ShortId(Guid value)
				{
					ShortValue = Encode(value);
					GuidValue = value;
				}
				private ShortId(string value)
				{
					ShortValue = value;
					GuidValue = Decode(value);
				}

				public string ShortValue { get; }
				public Guid GuidValue { get; }

				public static implicit operator string(ShortId shortId) => shortId.ShortValue;
				public static implicit operator Guid(ShortId shortId) => shortId.GuidValue;
				public static implicit operator ShortId(Guid guid) => new(guid);
				public static implicit operator ShortId(string shortGuid) => new(shortGuid);

				private static Guid Decode(string encoded)
				{
					var work = encoded.Replace("_", "/");
					work = work.Replace("-", "+");
					try
					{
						byte[] buffer = Convert.FromBase64String(work + "==");
						return new Guid(buffer);
					}
					catch (Exception e) when (e is ArgumentException or ArgumentNullException or FormatException)
					{
						throw new ArgumentException($"The Id supplied ('{encoded}') is not valid", e);
					}
				}

				private static string Encode(Guid guid)
				{
					string enc = Convert.ToBase64String(guid.ToByteArray());
					enc = enc.Replace("/", "_");
					enc = enc.Replace("+", "-");
					return enc[..22];
				}

				public static bool TryParse(string? input, out ShortId output)
				{
					if (string.IsNullOrEmpty(input))
					{
						output = default;
						return input is null;
					}

					try
					{
						output = new ShortId(input!);
						return true;
					}
					catch (ArgumentException)
					{
						output = default;
						return false;
					}
				}

				public override string ToString() => ShortValue;

				public bool Equals(ShortId other)
				{
					return ShortValue == other.ShortValue && GuidValue.Equals(other.GuidValue);
				}

				public override bool Equals(object? obj)
				{
					return obj is ShortId other && Equals(other);
				}

				public override int GetHashCode()
				{
					return HashCode.Combine(GuidValue);
				}

				public static bool operator ==(ShortId left, ShortId right)
				{
					return left.Equals(right);
				}

				public static bool operator !=(ShortId left, ShortId right)
				{
					return !(left == right);
				}

				public static bool operator ==(ShortId? left, ShortId? right)
				{
					return left?.Equals(right) ?? right is null;
				}

				public static bool operator !=(ShortId? left, ShortId? right)
				{
					return !(left == right);
				}
			}

			public sealed class Tender
			{
				[PrimaryKey]
				public TenderId Id { get; set; }

				[Column]
				public required string Name { get; set; }

				public static Tender[] Data =
				[
					new() { Id = TenderId.From(TestData.Guid1), Name = "TestName" }
				];
			}

			public struct TenderId : IEquatable<TenderId>
			{
				public Guid Value { get; set; }
				public static TenderId From(Guid value) => new TenderId { Value = value };
				public static TenderId? From(Guid? value) => value.HasValue ? new TenderId { Value = value.Value } : null;

				public static bool operator ==(TenderId a, TenderId b) => a.Value == b.Value;
				public static bool operator !=(TenderId a, TenderId b) => !(a == b);
				public static bool operator ==(TenderId a, Guid b) => a.Value == b;
				public static bool operator !=(TenderId a, Guid b) => !(a == b);
				public static bool operator ==(Guid a, TenderId b) => a == b.Value;
				public static bool operator !=(Guid a, TenderId b) => !(a == b);

				public static explicit operator TenderId(Guid value) => new TenderId { Value = value };
				public static explicit operator Guid(TenderId value) => value.Value;

				public bool Equals(TenderId other) => Value.Equals(other.Value);
				public override bool Equals(object? obj) => obj is TenderId other && Equals(other);
				public override int GetHashCode() => Value.GetHashCode();

				internal static void LinqToDbMapping(LinqToDB.Mapping.MappingSchema ms)
				{
					ms.SetConverter<TenderId, Guid>(id => (Guid)id);
					ms.SetConverter<TenderId, Guid?>(id => (Guid?)id);
					ms.SetConverter<TenderId?, Guid>(id => (Guid?)id ?? default);
					ms.SetConverter<TenderId?, Guid?>(id => (Guid?)id);
					ms.SetConverter<Guid, TenderId>(From);
					ms.SetConverter<Guid, TenderId?>(g => From(g));
					ms.SetConverter<Guid?, TenderId>(g => g == null ? default : From((Guid)g));
					ms.SetConverter<Guid?, TenderId?>(From);

					ms.SetConverter<TenderId, LinqToDB.Data.DataParameter>(id => new LinqToDB.Data.DataParameter { DataType = DataType.Guid, Value = (Guid)id });
					ms.SetConverter<TenderId?, LinqToDB.Data.DataParameter>(id => new LinqToDB.Data.DataParameter { DataType = DataType.Guid, Value = (Guid?)id });

					ms.AddScalarType(typeof(TenderId), DataType.Guid);
				}
			}
		}

		//[ActiveIssue(
		//	Configurations = [TestProvName.AllSapHana, TestProvName.AllSybase, ProviderName.SQLiteMS, TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllOracle, ProviderName.ClickHouseMySql],
		//	Details = "Reader expressions configuration weakness: we ask for reader for TenderId but get reader for byte[], without conversion defined between them")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5254")]
		public void ClientConversion_UnmappedOperators([DataSources(false)] string context)
		{
			var ms = new MappingSchema();
			Issue5254Types.TenderId.LinqToDbMapping(ms);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable(Issue5254Types.Tender.Data);

			// pass
			AssertQuery(tb.Select(i => new Issue5254Types.Output { Id = (Guid)i.Id }));

			// pass
			_ = tb.Where(r => r.Id == Guid.Empty).Select(i => new Issue5254Types.Output { Id = (Guid)i.Id }).FirstOrDefault();

			// bad mapping
			_ = tb.Select(i => new Issue5254Types.Output { Id = (Guid)i.Id }).FirstOrDefault();
		}

		static class Issue5254ServerTypes
		{
			public sealed class Output
			{
				public ShortId Id { get; set; }
			}

			public readonly struct ShortId : IEquatable<ShortId>
			{
				private ShortId(Guid value)
				{
					ShortValue = Encode(value);
					GuidValue = value;
				}

				public string ShortValue { get; }
				public Guid GuidValue { get; }

				//[ExpressionMethod(nameof(GuidToId))]
				public static implicit operator ShortId(Guid guid) => new(guid);
				public static Expression<Func<Guid, ShortId>> GuidToId() => guid => new(guid);

				private static string Encode(Guid guid)
				{
					string enc = Convert.ToBase64String(guid.ToByteArray());
					enc = enc.Replace("/", "_");
					enc = enc.Replace("+", "-");
					return enc[..22];
				}

				public override string ToString() => ShortValue;

				public bool Equals(ShortId other)
				{
					return ShortValue == other.ShortValue && GuidValue.Equals(other.GuidValue);
				}

				public override bool Equals(object? obj)
				{
					return obj is ShortId other && Equals(other);
				}

				public override int GetHashCode()
				{
					return HashCode.Combine(GuidValue);
				}
			}

			public sealed class Tender
			{
				[PrimaryKey]
				public TenderId Id { get; set; }

				[Column]
				public required string Name { get; set; }

				public static Tender[] Data =
				[
					new() { Id = TenderId.From(TestData.Guid1), Name = "TestName" }
				];
			}

			public struct TenderId : IEquatable<TenderId>
			{
				public Guid Value { get; set; }
				public static TenderId From(Guid value) => new TenderId { Value = value };

				[ExpressionMethod(nameof(GuidToId))]
				public static explicit operator TenderId(Guid value) => new TenderId { Value = value };
				[ExpressionMethod(nameof(IdToGuid))]
				public static explicit operator Guid(TenderId value) => value.Value;

				public static Expression<Func<Guid, TenderId>> GuidToId() => value => new TenderId { Value = value };
				public static Expression<Func<TenderId, Guid>> IdToGuid() => value => value.Value;

				public bool Equals(TenderId other) => Value.Equals(other.Value);
				public override bool Equals(object? obj) => obj is TenderId other && Equals(other);
				public override int GetHashCode() => Value.GetHashCode();

				internal static void LinqToDbMapping(LinqToDB.Mapping.MappingSchema ms)
				{
					ms.SetConverter<TenderId, LinqToDB.Data.DataParameter>(id => new LinqToDB.Data.DataParameter { DataType = DataType.Guid, Value = (Guid)id });
					ms.SetConverter<TenderId?, LinqToDB.Data.DataParameter>(id => new LinqToDB.Data.DataParameter { DataType = DataType.Guid, Value = (Guid?)id });

					ms.AddScalarType(typeof(TenderId), DataType.Guid);
				}
			}
		}

		//[ActiveIssue(
		//	Configurations = [TestProvName.AllSapHana, TestProvName.AllSybase, ProviderName.SQLiteMS, TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllOracle, ProviderName.ClickHouseMySql],
		//	Details = "Reader expressions configuration weakness: we ask for reader for TenderId but get reader for byte[], without conversion defined between them")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5254")]
		public void ClientConversion_MappedOperators([DataSources(false)] string context)
		{
			var ms = new MappingSchema();
			Issue5254ServerTypes.TenderId.LinqToDbMapping(ms);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable(Issue5254ServerTypes.Tender.Data);

			// pass
			AssertQuery(tb.Select(i => new Issue5254ServerTypes.Output { Id = (Guid)i.Id }));

			// bad mapping
			_ = tb.Select(i => new Issue5254ServerTypes.Output { Id = (Guid)i.Id }).FirstOrDefault();
		}

		#endregion

		#region Prefer client-side
		class ImplicitValue<TData> : IEquatable<ImplicitValue<TData>>
		{
			public TData? Value { get; init; }

			public static implicit operator TData?(ImplicitValue<TData>? value)
			{
				return value != null ? value.Value : default;
			}

			public static implicit operator ImplicitValue<TData>(TData? value)
			{
				return new ImplicitValue<TData> { Value = value };
			}

			public bool Equals(ImplicitValue<TData>? other)
			{
				if (other is null) return false;
				if (ReferenceEquals(this, other)) return true;

				return EqualityComparer<TData?>.Default.Equals(Value, other.Value);
			}

			public override bool Equals(object? obj)
			{
				if (obj is null) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;

				return Equals((ImplicitValue<TData>)obj);
			}

			public override int GetHashCode()
			{
				return Value != null ? Value.GetHashCode() : 0;
			}

			public override string ToString()
			{
				return Value == null ? "Value=null" : $"Value={Value}";
			}

			public static bool operator ==(ImplicitValue<TData>? left, ImplicitValue<TData>? right)
			{
				return Equals(left, right);
			}

			public static bool operator !=(ImplicitValue<TData>? left, ImplicitValue<TData>? right)
			{
				return !Equals(left, right);
			}
		}

		class ImplicitData
		{
			public required ImplicitValue<string?> StringData1 { get; init; }
			public required ImplicitValue<string?> StringData2 { get; init; }
			public required ImplicitValue<int?> IntData1 { get; init; }
			public required ImplicitValue<int?> IntData2 { get; init; }
		}

		[Test]
		public void ImplicitTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			using var t = db.CreateLocalTable(
			[
				new
				{
					StringData1 = "Test1",
					StringData2 = (string?)null,
					IntData1    = 123,
					IntData2    = (int?)null,
				}
			]);

			var result1 = t
				.Select(r => new ImplicitData
				{
					StringData1 = r.StringData1,
					StringData2 = r.StringData2,
					IntData1    = r.IntData1,
					IntData2    = r.IntData2,
				})
				.Single();

			var result = t.Single();

			var result2 = new ImplicitData
			{
				StringData1 = result.StringData1,
				StringData2 = result.StringData2,
				IntData1    = result.IntData1,
				IntData2    = result.IntData2,
			};

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result1.StringData1, Is.EqualTo(result2.StringData1));
				Assert.That(result1.StringData2, Is.EqualTo(result2.StringData2));
				Assert.That(result1.IntData1, Is.EqualTo(result2.IntData1));
				Assert.That(result1.IntData2, Is.EqualTo(result2.IntData2));
			}
		}

		[Test]
		public void ImplicitTest_WithMapping([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var fb = new FluentMappingBuilder();
			fb.Entity<string>().Member(v => (ImplicitValue<string>)v).HasAttribute(new Sql.ExpressionAttribute("{0}"));
			fb.Entity<int?>().Member(v => (ImplicitValue<int?>)v).HasAttribute(new Sql.ExpressionAttribute("{0}"));

			using var db = GetDataContext(context, fb.Build().MappingSchema);

			using var t = db.CreateLocalTable(
			[
				new
				{
					StringData1 = "Test1",
					StringData2 = (string?)null,
					IntData1    = 123,
					IntData2    = (int?)null,
				}
			]);

			var result1 = t
				.Select(r => new ImplicitData
				{
					StringData1 = r.StringData1,
					StringData2 = r.StringData2,
					IntData1    = r.IntData1,
					IntData2    = r.IntData2,
				})
				.Single();

			var result = t.Single();

			var result2 = new ImplicitData
			{
				StringData1 = result.StringData1,
				StringData2 = result.StringData2,
				IntData1    = result.IntData1,
				IntData2    = result.IntData2,
			};

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result1.StringData1, Is.EqualTo(result2.StringData1));
				Assert.That(result1.StringData2, Is.EqualTo(result2.StringData2));
				Assert.That(result1.IntData1, Is.EqualTo(result2.IntData1));
				Assert.That(result1.IntData2, Is.EqualTo(result2.IntData2));
			}
		}
		#endregion
	}
}
