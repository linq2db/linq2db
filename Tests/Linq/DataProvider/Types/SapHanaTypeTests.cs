using System.Globalization;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

using Sap.Data.Hana;

namespace Tests.DataProvider
{
	// TODO: add more types
	// https://help.sap.com/docs/SAP_HANA_PLATFORM/4fe29514fd584807ac9f2a04f6754767/20a1569875191014b507cf392724b7eb.html
	//
	// Issues:
	// - RealVector type not tested as it requires cloud hana version
	// - HanaDecimal no supported in native bulk copy in 2.23 provider
	public sealed class SapHanaTypeTests : TypeTestsBase
	{
		sealed class SapHanaDataSourcesAttribute : IncludeDataSourcesAttribute
		{
			public SapHanaDataSourcesAttribute()
				: this(true)
			{
			}

			public SapHanaDataSourcesAttribute(bool includeLinqService)
				: base(includeLinqService, TestProvName.AllSapHana)
			{
			}
		}

		[Test]
		public async ValueTask TestSmallDecimal([SapHanaDataSources(false)] string context)
		{
			var min = 9999999999999999m;
			var max = -9999999999999999m;

			var dataType = new DbDataType(typeof(decimal), DataType.SmallDecFloat);

			await TestType<decimal, decimal?>(context, dataType, default, default);
			await TestType<decimal, decimal?>(context, dataType, min, max);
			await TestType<decimal, decimal?>(context, dataType, min / 100000m, max / 10000m);

			if (context.IsAnyOf(ProviderName.SapHanaNative))
			{
				var dataTypeCustom = new DbDataType(typeof(HanaDecimal), DataType.SmallDecFloat);
				var customMin = new HanaDecimal("-" + new string('9', 16));
				var customMax = new HanaDecimal("-" + new string('9', 16));
				await TestType<HanaDecimal, HanaDecimal?>(context, dataTypeCustom, new HanaDecimal(0), null, testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);
				await TestType<HanaDecimal, HanaDecimal?>(context, dataTypeCustom, new HanaDecimal(1), default(HanaDecimal), testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);
				await TestType<HanaDecimal, HanaDecimal?>(context, dataTypeCustom, customMax, customMin, testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);

				await TestType<HanaDecimal, HanaDecimal?>(context, dataTypeCustom, new HanaDecimal("0.9999999999999999E-349"), new HanaDecimal("-9.999999999999999E348"), testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);
			}
		}

		[Test]
		public async ValueTask TestDecimal([SapHanaDataSources(false)] string context)
		{
			var defaultMax = 7922816251426433759.3543950335M;
			var defaultMin = -7922816251426433759.3543950335M;

			var dataType = new DbDataType(typeof(decimal), DataType.DecFloat);

			await TestType<decimal, decimal?>(context, dataType, default, default);
			await TestType<decimal, decimal?>(context, dataType, defaultMin, defaultMax);

			if (context.IsAnyOf(ProviderName.SapHanaNative))
			{
				var dataTypeCustom = new DbDataType(typeof(HanaDecimal), DataType.DecFloat);
				var customMin = new HanaDecimal("-" + new string('9', 34));
				var customMax = new HanaDecimal(new string('9', 34));
				await TestType<HanaDecimal, HanaDecimal?>(context, dataTypeCustom, new HanaDecimal(0), null, testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);
				await TestType<HanaDecimal, HanaDecimal?>(context, dataTypeCustom, new HanaDecimal(1), default(HanaDecimal), testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);
				await TestType<HanaDecimal, HanaDecimal?>(context, dataTypeCustom, customMax, customMin, testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);
				await TestType<HanaDecimal, HanaDecimal?>(context, dataTypeCustom, new HanaDecimal("99999999999999999999999999.99999999E-5000"), new HanaDecimal("-99999999999999999.99999999999999999E5000"), testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific, getExpectedValue: _ => new HanaDecimal("0.9999999999999999999999999999999999E-4974"), getExpectedNullableValue: _ => new HanaDecimal("-9.999999999999999999999999999999999E5016"));
			}
		}

		[Test]
		public async ValueTask TestDecimalPS([SapHanaDataSources(false)] string context)
		{
			// default mapping
			var defaultMax = 7922816251426433759.3543950335M;
			var defaultMin = -7922816251426433759.3543950335M;

			// DECIMAL
			await TestType<decimal, decimal?>(context, new(typeof(decimal)), default, default);
			await TestType<decimal, decimal?>(context, new(typeof(decimal)), defaultMax, defaultMin);

			if (context.IsAnyOf(ProviderName.SapHanaNative))
			{
				var customMin = new HanaDecimal($"-{new string('9', 28)}.{new string('9', 10)}");
				var customMax = new HanaDecimal($"{new string('9', 28)}.{new string('9', 10)}");
				await TestType<HanaDecimal, HanaDecimal?>(context, new(typeof(HanaDecimal)), new HanaDecimal(0), null, testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific, getExpectedValue: _ => new HanaDecimal("0.0000000000"));
				// TODO: default(HanaDecimal) requires aditional configuration
				//await TestType<HanaDecimal, HanaDecimal?>(context, new(typeof(HanaDecimal)), new HanaDecimal(1), default(HanaDecimal), testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific, getExpectedValue: _ => new HanaDecimal("1.0000000000"));
				await TestType<HanaDecimal, HanaDecimal?>(context, new(typeof(HanaDecimal)), customMax, customMin, testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);
			}

			// testing of all combinations is not feasible
			// we will test only several precisions and first/last two scales for each tested precision
			var precisions = new int[] { 1, 2, 10, 17, 20, 28, 29, 30, 37, 38 };
			foreach (var p in precisions)
			{
				var skipBasicTypes = p >= 29 && context.IsAnyOf(ProviderName.ClickHouseClient);

				for (var s = 0; s <= p; s++)
				{
					if (s > 1 && s < p - 1)
						continue;

					var decimalType = new DbDataType(typeof(decimal), DataType.Decimal, null, null, p, s);

					var maxString = new string('9', p);
					if (s > 0)
						maxString = maxString.Substring(0, p - s) + '.' + maxString.Substring(p - s);
					if (maxString[0] == '.')
						maxString = $"0{maxString}";
					var minString = $"-{maxString}";

					decimal minDecimal;
					decimal maxDecimal;
					if (p >= 29)
					{
						maxDecimal = decimal.MaxValue;
						minDecimal = decimal.MinValue;

						for (var i = 0; i < s; i++)
						{
							maxDecimal /= 10;
							minDecimal /= 10;
						}
					}
					else
					{
						maxDecimal = decimal.Parse(maxString, CultureInfo.InvariantCulture);
						minDecimal = -maxDecimal;
					}

					await TestType<decimal, decimal?>(context, decimalType, default, default);
					await TestType<decimal, decimal?>(context, decimalType, minDecimal, maxDecimal);

					if (context.IsAnyOf(ProviderName.SapHanaNative))
					{
						var customDecimalType = new DbDataType(typeof(HanaDecimal), DataType.Decimal, null, null, p, s);

						var strValue = new string('9', p - s);
						if (strValue.Length == 0)
							strValue = "0";
						if (s != 0)
							strValue += $".{new string('9', s)}";

						var customMin = new HanaDecimal("-" + strValue);
						var customMax = new HanaDecimal(strValue);

						await TestType<HanaDecimal, HanaDecimal?>(context, customDecimalType, customMin, customMax, testBulkCopyType: t => t != LinqToDB.Data.BulkCopyType.ProviderSpecific);
					}
				}
			}
		}

		[ActiveIssue("Type supported only on cloud version and requires column table")]
		[Test(Description = "https://help.sap.com/docs/hana-cloud-database/sap-hana-cloud-sap-hana-database-vector-engine-guide/real-vector-data-type")]
		public async ValueTask TestRealVector([SapHanaDataSources(false)] string context)
		{
			var min = new float[] { 1.1f };
			var max = new float[65000];
			for (var i = 0; i < max.Length; i++)
				max[i] = -1.1f * i;

			await TestType<float[], float[]?>(context, new(typeof(float[])), [], null);
			await TestType<float[], float[]?>(context, new(typeof(float[])), min, max);

			var limitedType = new DbDataType(typeof(float[]), DataType.Undefined, dbType: null, length: 10);
			await TestType<float[], float[]?>(context, limitedType, new float[] { 1.1f, -2.2f }, new float[] { 1.1f, -2.2f, 3.3f });
		}
	}
}
