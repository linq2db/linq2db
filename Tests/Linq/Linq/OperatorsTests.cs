#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CA1046 // Do not overload equality operator on reference types
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Linq;
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
	}
}
