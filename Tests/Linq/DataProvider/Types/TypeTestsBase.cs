using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.DataProvider
{
	// base fixture for database-specific type tests
	// provides test method TestType to test specific type configuration
	[TestFixture]
	public abstract class TypeTestsBase : TestBase
	{
		// Mapping for single type configuration testing for both nullable and non-nullable database columns
		private sealed class TypeTable<TType, TNullableType>
		{
			public TType          Column         { get; set; } = default!;
			public TNullableType? ColumnNullable { get; set; }
		}

		/// <summary>
		/// Returns default switch for disable parameters testing for database provider.
		/// Used by TestType method when testParameters parameter is not set (or <c>null</c>).
		/// </summary>
		protected virtual bool TestParameters => true;

		/// <summary>
		/// Performs single type configuration testing:
		/// <list type="bullet">
		/// <item>column type generation (CreateTable API)</item>
		/// <item>nullable/non-nullable behavior</item>
		/// <item>parameters</item>
		/// <item>literals</item>
		/// <item>all bulk copy methods</item>
		/// <item>test extreme values support (e.g. min/max values allowed by type)</item>
		/// </list>
		///
		/// Passing various values to method allows to test extreme values support. Recommended values to test:
		/// <list type="bullet">
		/// <item>default/null values</item>
		/// <item>min/max values for ranged types</item>
		/// <item>type specific special values. E.g. Epsilon and NaN/Inf for floating point types</item>
		/// </list>
		///
		/// Parameter <paramref name="dbType"/> allows testing of various mapping configurations and should include:
		/// <list type="bullet">
		/// <item>default type mapping for tested .NET type (if exists)</item>
		/// <item>default type mapping for <see cref="DataType"/> value, used for tested database type</item>
		/// <item>test of various precision/scale values when applicable</item>
		/// <item>additional .NET types testing if type could be mapped to different types</item>
		/// </list>
		/// </summary>
		/// <typeparam name="TType">Non-nullable mapping column type.</typeparam>
		/// <typeparam name="TNullableType">Nullable mapping column type.</typeparam>
		/// <param name="context">Database context name.</param>
		/// <param name="dbType">Type descriptor to apply to columns configuration.</param>
		/// <param name="value">Value to test for non-nullable column.</param>
		/// <param name="nullableValue">Value to test for nullable column.</param>
		/// <param name="testParameters">Enable/disable parameters testing (default: <see cref="TestParameters"/>).</param>
		/// <param name="skipNullable">Enable/disable nullable column testing (default: <c>false</c>).</param>
		/// <param name="filterByValue">Enable/disable selection filter by non-nullable column (default: <c>true</c>).</param>
		/// <param name="filterByNullableValue">Enable/disable selection filter by nullable column (default: <c>true</c>).</param>
		/// <param name="getExpectedValue">Optional expected non-nullable value provider to use when value doesn't roundtrip.</param>
		/// <param name="getExpectedNullableValue">Optional expected nullable value provider to use when value doesn't roundtrip.</param>
		/// <param name="isExpectedValue">Optional custom check that non-nullable value returned by server is correct.Takes priority over <paramref name="getExpectedValue"/>.</param>
		/// <param name="isExpectedNullableValue">Optional custom check that nullable value returned by server is correct.Takes priority over <paramref name="getExpectedNullableValue"/>.</param>
		/// <param name="optionsBuilder">Optional ooptions builder to configure connection.</param>
		protected async ValueTask TestType<TType, TNullableType>(
			string context,
			DbDataType dbType,
			TType value,
			TNullableType? nullableValue,
			bool? testParameters = null,
			bool skipNullable = false,
			bool filterByValue = true,
			bool filterByNullableValue = true,
			Func<TType, TType>? getExpectedValue = null,
			Func<TNullableType?, TNullableType?>? getExpectedNullableValue = null,
			Func<TType, bool>? isExpectedValue = null,
			Func<TNullableType?, bool>? isExpectedNullableValue = null,
			Func<DataOptions, DataOptions>? optionsBuilder = null,
			Func<BulkCopyType, bool>? testBulkCopyType = null)
		{
			testParameters ??= TestParameters;

			// setup test table mapping
			var ms = new MappingSchema();

			var ent = new FluentMappingBuilder(ms).Entity<TypeTable<TType, TNullableType>>();

			var prop = ent.Property(e => e.Column).IsNullable(false);

			if (dbType.DataType != DataType.Undefined) prop.HasDataType(dbType.DataType);
			if (dbType.DbType != null) prop.HasDbType(dbType.DbType);
			if (dbType.Precision != null) prop.HasPrecision(dbType.Precision.Value);
			if (dbType.Scale != null) prop.HasScale(dbType.Scale.Value);
			if (dbType.Length != null) prop.HasLength(dbType.Length.Value);

			if (!skipNullable)
			{
				var propN = ent.Property(e => e.ColumnNullable).IsNullable(true);

				if (dbType.DataType != DataType.Undefined) propN.HasDataType(dbType.DataType);
				if (dbType.DbType != null) propN.HasDbType($"Nullable({dbType.DbType})");
				if (dbType.Precision != null) propN.HasPrecision(dbType.Precision.Value);
				if (dbType.Scale != null) propN.HasScale(dbType.Scale.Value);
				if (dbType.Length != null) propN.HasLength(dbType.Length.Value);
			}
			else
				ent.Property(e => e.ColumnNullable).IsNotColumn();

			ent.Build();

			// start testing
			using var db = GetDataConnection(context, o => optionsBuilder == null ? o.UseMappingSchema(ms) : optionsBuilder(o.UseMappingSchema(ms)));

			var data = new[] { new TypeTable<TType, TNullableType> { Column = value, ColumnNullable = nullableValue } };
			using var table = db.CreateLocalTable(data);

			// test parameter
			if (testParameters == true)
			{
				db.InlineParameters = false;

				var expectedParamCount = (filterByValue ? 1 : 0) + (filterByNullableValue && nullableValue != null ? 1 : 0);

				db.OnNextCommandInitialized((_, cmd) =>
				{
					Assert.That(cmd.Parameters, Has.Count.EqualTo(expectedParamCount));
					return cmd;
				});

				AssertData(table, value, nullableValue, skipNullable, filterByValue, filterByNullableValue, getExpectedValue, getExpectedNullableValue, isExpectedValue, isExpectedNullableValue);
			}

			// test literal
			{
				db.InlineParameters = true;
				db.OnNextCommandInitialized((_, cmd) =>
				{
					Assert.That(cmd.Parameters, Is.Empty);
					return cmd;
				});

				AssertData(table, value, nullableValue, skipNullable, filterByValue, filterByNullableValue, getExpectedValue, getExpectedNullableValue, isExpectedValue, isExpectedNullableValue);
				db.InlineParameters = false;
			}

			var options = GetDefaultBulkCopyOptions(context);

			// test bulk copy modes
			if (testBulkCopyType?.Invoke(BulkCopyType.RowByRow) != false)
			{
				options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.RowByRow };
				table.Delete();
				db.BulkCopy(options, data);
				AssertData(table, value, nullableValue, skipNullable, filterByValue, filterByNullableValue, getExpectedValue, getExpectedNullableValue, isExpectedValue, isExpectedNullableValue);
			}

			if (testBulkCopyType?.Invoke(BulkCopyType.MultipleRows) != false)
			{
				options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.MultipleRows };
				table.Delete();
				db.BulkCopy(options, data);
				AssertData(table, value, nullableValue, skipNullable, filterByValue, filterByNullableValue, getExpectedValue, getExpectedNullableValue, isExpectedValue, isExpectedNullableValue);
			}

			if (testBulkCopyType?.Invoke(BulkCopyType.ProviderSpecific) != false)
			{
				options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.ProviderSpecific };
				table.Delete();
				db.BulkCopy(options, data);
				AssertData(table, value, nullableValue, skipNullable, filterByValue, filterByNullableValue, getExpectedValue, getExpectedNullableValue, isExpectedValue, isExpectedNullableValue);

				// test async provider-specific bulk copy as it often has own implementation
				options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.ProviderSpecific };
				await table.DeleteAsync();
				await db.BulkCopyAsync(options, data);
				AssertData(table, value, nullableValue, skipNullable, filterByValue, filterByNullableValue, getExpectedValue, getExpectedNullableValue, isExpectedValue, isExpectedNullableValue);
			}
		}

		/// <summary>
		/// Assert test data:
		/// <list type="bullet">
		/// <item>select data row from table using both values in filter (to test literal/parameter filtering)</item>
		/// <item>assert received data to match inserted data</item>
		/// </list>
		/// </summary>
		/// <typeparam name="TType">Non-nullable mapping column type.</typeparam>
		/// <typeparam name="TNullableType">Nullable mapping column type.</typeparam>
		/// <param name="table">Test table.</param>
		/// <param name="value">Value to test for non-nullable column.</param>
		/// <param name="nullableValue">Value to test for nullable column.</param>
		/// <param name="skipNullable">Enable/disable nullable column testing (default: <c>false</c>).</param>
		/// <param name="filterByValue">Enable/disable selection filter by non-nullable column (default: <c>true</c>).</param>
		/// <param name="filterByNullableValue">Enable/disable selection filter by nullable column (default: <c>true</c>).</param>
		/// <param name="getExpectedValue">Optional expected non-nullable value provider to use when value doesn't roundtrip.</param>
		/// <param name="getExpectedNullableValue">Optional expected nullable value provider to use when value doesn't roundtrip.</param>
		private static void AssertData<TType, TNullableType>(
			TempTable<TypeTable<TType, TNullableType>> table,
			TType                                      value,
			TNullableType?                             nullableValue,
			bool                                       skipNullable,
			bool                                       filterByValue,
			bool                                       filterByNullableValue,
			Func<TType, TType>?                        getExpectedValue,
			Func<TNullableType?, TNullableType?>?      getExpectedNullableValue,
			Func<TType, bool>?                         isExpectedValue,
			Func<TNullableType?, bool>?                isExpectedNullableValue)
		{
			filterByNullableValue = filterByNullableValue && !skipNullable;

			// build query filter and read data
			// cast to object used to make comparison not complain in C# due to missing operator== implementation on tested types
			// and at the same time preserve value type inference and = operator generation in SQL
			var records =
					filterByValue && filterByNullableValue
					? table.Where(r => (object)r.Column! == (object)value! && (object?)r.ColumnNullable == (object?)nullableValue).ToArray()
					: filterByValue
						? table.Where(r => (object)r.Column! == (object)value!).ToArray()
						: filterByNullableValue
							? table.Where(r => (object?)r.ColumnNullable == (object?)nullableValue).ToArray()
							: table.ToArray();

			// assert data
			Assert.That(records, Has.Length.EqualTo(1));

			var record = records[0];
			if (isExpectedValue != null)
				Assert.That(isExpectedValue(record.Column), Is.True);
			else
				Assert.That(record.Column, Is.EqualTo(getExpectedValue != null ? getExpectedValue(value) : value));

			if (!skipNullable)
			{
				if (isExpectedNullableValue != null)
					Assert.That(isExpectedNullableValue(record.ColumnNullable), Is.True);
				else
					Assert.That(record.ColumnNullable, Is.EqualTo(getExpectedNullableValue != null ? getExpectedNullableValue(nullableValue) : nullableValue));
			}
		}
	}
}
