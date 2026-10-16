using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.Common
{
	[TestFixture]
	public class FormattableStringHelperTests
	{
		[TestCase("{0} x {1:N2} {{2}}", "{10} x {11:N2} {{2}}")]
		[TestCase("{0,5}",              "{10,5}"              )]
		[TestCase("{{{0}}}",            "{{{10}}}"            )]
		[TestCase("{1}{0}",             "{11}{10}"            )]
		[TestCase("{a}",                "{a}"                 )]
		public void TransformExpressionIndexes(string format, string expected)
		{
			QueryHelper.TransformExpressionIndexes(0, format, static (_, i) => i + 10).ShouldBe(expected);
		}

		[Test]
		public void ConvertFormatToConcatenation_Items()
		{
			var p0 = new SqlValue(typeof(int), 1);

			var result = QueryHelper.ConvertFormatToConcatenation("a{0}b{{c}}", [p0]).ShouldBeOfType<SqlConcatExpression>();

			result.Expressions.Length.ShouldBe(3);
			result.Expressions[0].ShouldBeOfType<SqlValue>().Value.ShouldBe("a");
			result.Expressions[1].ShouldBeSameAs(p0);
			result.Expressions[2].ShouldBeOfType<SqlValue>().Value.ShouldBe("b{c}");
		}

		[Test]
		public void ConvertFormatToConcatenation_Alignment()
		{
			var p0 = new SqlValue(typeof(int), 1);

			QueryHelper.ConvertFormatToConcatenation("{0,5}", [p0]).ShouldBeSameAs(p0);
		}

		[Test]
		public void ConvertFormatToConcatenation_NoItems()
		{
			QueryHelper.ConvertFormatToConcatenation("a{{b}}", []).ShouldBeOfType<SqlValue>().Value.ShouldBe("a{b}");
		}

		[TestCase("a {0} b",     new[] { 0 })]
		[TestCase("{{0}}",       new int[0])]
		[TestCase("{{{0}}}",     new[] { 0 })]
		[TestCase("{0,5}",       new[] { 0 })]
		[TestCase("{0:N2}",      new[] { 0 })]
		[TestCase("{0 ,-3:x}",   new[] { 0 })]
		[TestCase("{1}{0}",      new[] { 1, 0 })]
		[TestCase("{12}",        new[] { 12 })]
		public void ParseFormatItems(string format, int[] indexes)
		{
			var items = FormattableStringHelper.ParseFormatItems(format).ShouldNotBeNull();

			items.Select(i => i.Index).ShouldBe(indexes);

			foreach (var item in items)
			{
				format[item.Start].ShouldBe('{');
				format[item.Start + item.Length - 1].ShouldBe('}');
				format.Substring(item.IndexStart, item.IndexLength).ShouldBe(item.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		[TestCase("{0}",        false)]
		[TestCase("{0,5}",      true )]
		[TestCase("{0 ,-3:x}",  true )]
		[TestCase("{0:x,y}",    false)]
		public void ParseFormatItems_HasAlignment(string format, bool hasAlignment)
		{
			FormattableStringHelper.ParseFormatItems(format).ShouldNotBeNull().Single().HasAlignment.ShouldBe(hasAlignment);
		}

		[TestCase("{a}")]
		[TestCase("{0")]
		[TestCase("{")]
		[TestCase("a}b")]
		[TestCase("{}")]
		public void ParseFormatItems_Invalid(string format)
		{
			FormattableStringHelper.ParseFormatItems(format).ShouldBeNull();
		}

		[Test]
		public void GetReferencedArguments()
		{
			FormattableStringHelper.GetReferencedArguments("{1} {5}", 3).ShouldBe([false, true, false]);
			FormattableStringHelper.GetReferencedArguments("{a}",     3).ShouldBeNull();
		}

		[Test]
		public void TrySplit()
		{
			var arguments = Expression.NewArrayInit(typeof(object), Expression.Constant(1, typeof(object)));
			var create    = FormattableStringHelper.CreateExpression("x {0}", arguments);

			FormattableStringHelper.TrySplit(create, out var format, out var split).ShouldBeTrue();
			format.ShouldBeOfType<ConstantExpression>().Value.ShouldBe("x {0}");
			split.ShouldBeSameAs(arguments);

			var array = Expression.Parameter(typeof(object[]));
			FormattableStringHelper.TrySplit(FormattableStringHelper.CreateExpression("x", array), out _, out _).ShouldBeFalse();
			FormattableStringHelper.TrySplit(Expression.Constant(FormattableStringFactory.Create("x")), out _, out _).ShouldBeFalse();
		}

		[Test]
		public void CreateExpression_RoundTrip()
		{
			var value  = FormattableStringFactory.Create("{0} {1}", 1, "a");
			var result = Expression.Lambda<Func<FormattableString>>(FormattableStringHelper.CreateExpression(value)).Compile()();

			FormattableStringHelper.AreEqual(value, result, static (a, b) => ((object?[])a!).SequenceEqual((object?[])b!)).ShouldBeTrue();
		}

		[Test]
		public void AreEqual_ComputeHashCode()
		{
			static bool ArgumentsEqual(object? a, object? b) => ((object?[])a!).SequenceEqual((object?[])b!);
			static int  ArgumentsHash (object? a)            => ((object?[])a!).Aggregate(17, (h, v) => h * 31 + (v?.GetHashCode() ?? 0));

			var value = FormattableStringFactory.Create("{0}", 1);

			FormattableStringHelper.AreEqual(value, FormattableStringFactory.Create("{0}",   1), ArgumentsEqual).ShouldBeTrue();
			FormattableStringHelper.AreEqual(value, FormattableStringFactory.Create("{0} ",  1), ArgumentsEqual).ShouldBeFalse();
			FormattableStringHelper.AreEqual(value, FormattableStringFactory.Create("{0}",   2), ArgumentsEqual).ShouldBeFalse();

			FormattableStringHelper.ComputeHashCode(value, ArgumentsHash)
				.ShouldBe(FormattableStringHelper.ComputeHashCode(FormattableStringFactory.Create("{0}", 1), ArgumentsHash));
		}

		sealed class DerivedDataParameter : DataParameter
		{
			public DerivedDataParameter() : base("p", 2) { }
		}

		static Expression Element(object? value, Type type)
		{
			var constant = Expression.Constant(value, type);
			return type == typeof(object) ? constant : Expression.Convert(constant, typeof(object));
		}

		static Expression[] Prepare(string format, bool keepUnreferenced, params Expression[] elements)
		{
			var formattable = FormattableStringHelper.CreateExpression(format, Expression.NewArrayInit(typeof(object), elements));

			FormattableStringHelper.PrepareRawSqlArguments(formattable, null, keepUnreferenced, out var resultFormat, out var arguments);

			resultFormat.ShouldBe(format);

			return arguments.ToArray();
		}

		static bool IsDropped(Expression argument) => argument is ConstantExpression { Value: null } c && c.Type == typeof(object);

		[Test]
		public void PrepareRawSqlArguments_UnreferencedValueIsDropped()
		{
			var arguments = Prepare("{0}", false, Element(1, typeof(int)), Element(99, typeof(int)));

			IsDropped(arguments[0]).ShouldBeFalse();
			IsDropped(arguments[1]).ShouldBeTrue();
		}

		[Test]
		public void PrepareRawSqlArguments_UnreferencedIsKept()
		{
			var parameter = new DataParameter("p", 2);
			var derived   = new DerivedDataParameter();

			var arguments = Prepare("{0}", false,
				Element(1,                          typeof(int)),
				Element(parameter,                  typeof(DataParameter)),
				Element(derived,                    typeof(DerivedDataParameter)),
				Element(99,                         typeof(object)),
				Element(new SqlValue(typeof(int), 1), typeof(SqlValue)));

			arguments.Skip(1).ShouldAllBe(a => !IsDropped(a));
		}

		[Test]
		public void PrepareRawSqlArguments_KeepUnreferenced()
		{
			Prepare("{0}", true, Element(1, typeof(int)), Element(99, typeof(int))).ShouldAllBe(a => !IsDropped(a));
		}

		[Test]
		public void PrepareRawSqlArguments_NoFormatItems()
		{
			Prepare("x = ?", false, Element(2, typeof(int))).ShouldAllBe(a => !IsDropped(a));
		}

		[Test]
		public void PrepareRawSqlArguments_InvalidFormat()
		{
			Prepare("{a}", false, Element(1, typeof(int)), Element(99, typeof(int))).ShouldAllBe(a => !IsDropped(a));
		}
	}
}
