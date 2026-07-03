using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Mapping
{
	[TestFixture]
	public class ValueConverterColumnDbTypeTests : TestBase
	{
		sealed class Money
		{
			public decimal Amount { get; set; }
		}

		sealed class MoneyToDecimalConverter() : ValueConverter<Money, decimal>(
			m => m.Amount, d => new Money { Amount = d }, false);

		sealed class PriceEntity
		{
			[Column]
			public int Id { get; set; }

			// No explicit DataType: the column's DB type must be resolved from the value converter's
			// provider type (decimal) against the active mapping schema, not from the member type (Money,
			// which has no DB type of its own).
			[Column, ValueConverter(ConverterType = typeof(MoneyToDecimalConverter))]
			public Money Price { get; set; } = null!;
		}

		[Test]
		public void ConverterColumn_ResolvesDbTypeFromProviderType()
		{
			var ms  = new MappingSchema();
			var ed  = ms.GetEntityDescriptor(typeof(PriceEntity));
			var col = ed.Columns.Single(c => c.MemberName == nameof(PriceEntity.Price));

			var dbType = col.GetDbDataType(true);

			// Without the fix this is DataType.Undefined: the type is resolved from Money (the member
			// type), which has no registered DB type, instead of from the converter's provider type.
			Assert.That(dbType.DataType, Is.EqualTo(DataType.Decimal));
		}

		[Test]
		public void ConverterColumn_PropagatesProviderPrecisionAndScale()
		{
			var ms = new MappingSchema();
			// Provider-faithful decimal facets registered on the active schema for the element type.
			ms.SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, typeof(decimal), 18, 10));

			var ed  = ms.GetEntityDescriptor(typeof(PriceEntity));
			var col = ed.Columns.Single(c => c.MemberName == nameof(PriceEntity.Price));

			var dbType = col.GetDbDataType(true);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(dbType.DataType,  Is.EqualTo(DataType.Decimal));
				Assert.That(dbType.Precision, Is.EqualTo(18));
				Assert.That(dbType.Scale,     Is.EqualTo(10));
			}
		}
	}
}
