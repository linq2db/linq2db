#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CA1046 // Do not overload equality operator on reference types
using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class OperatorsTests : TestBase
	{
		// 1. remote disabled as we don't have serialization configured for custom types
		// 2. tests don't put operator mappings on custom types directly to ensure we can map 3rd-party code
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

			public static CustomInt operator -(CustomInt value) => new CustomInt() { Value = -value.Value };
		}

		sealed class CustomIntClass
		{
			public int Value;

			public static implicit operator CustomIntClass(int value) => new CustomIntClass() { Value = value };
			// long conversion needed for sqlite
			public static implicit operator CustomIntClass(long value) => new CustomIntClass() { Value = (int)value };

			public static bool operator ==(CustomIntClass? left, int right) => left?.Value == right;
			public static bool operator !=(CustomIntClass? left, int right) => left?.Value != right;

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
	}
}
