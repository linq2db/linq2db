using System;

using LinqToDB.Data;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Concrete <see cref="ITempTableBulkCopyOptionsBuilder"/> + nested
	/// <see cref="ITempTableBulkCopyTypeConfigBuilder"/>. Outer entry methods select the
	/// <see cref="BulkCopyType"/>; the inner config evolves the same <see cref="BulkCopyOptions"/>
	/// instance via <c>with</c>-expressions and is shared across all three outer types — calling
	/// <c>UseX</c> twice is a configuration error (the resulting spec would have ambiguous
	/// bulk-copy-type intent).
	/// </summary>
	sealed class TempTableBulkCopyOptionsBuilderImpl : ITempTableBulkCopyOptionsBuilder, ITempTableBulkCopyTypeConfigBuilder
	{
		BulkCopyOptions _options = BulkCopyOptions.Default;
		bool            _bulkCopyTypeSet;

		public ITempTableBulkCopyOptionsBuilder UseRowByRow(Func<ITempTableBulkCopyTypeConfigBuilder, ITempTableBulkCopyTypeConfigBuilder>? configure = null)
			=> Use(BulkCopyType.RowByRow, configure);

		public ITempTableBulkCopyOptionsBuilder UseMultiRows(Func<ITempTableBulkCopyTypeConfigBuilder, ITempTableBulkCopyTypeConfigBuilder>? configure = null)
			=> Use(BulkCopyType.MultipleRows, configure);

		public ITempTableBulkCopyOptionsBuilder UseProviderSpecific(Func<ITempTableBulkCopyTypeConfigBuilder, ITempTableBulkCopyTypeConfigBuilder>? configure = null)
			=> Use(BulkCopyType.ProviderSpecific, configure);

		ITempTableBulkCopyOptionsBuilder Use(BulkCopyType type, Func<ITempTableBulkCopyTypeConfigBuilder, ITempTableBulkCopyTypeConfigBuilder>? configure)
		{
			if (_bulkCopyTypeSet)
				throw new LinqToDBException("AsQueryable configure: ConfigureBulkCopy(...) may select at most one UseRowByRow / UseMultiRows / UseProviderSpecific option.");

			_bulkCopyTypeSet = true;
			_options         = _options with { BulkCopyType = type };

			configure?.Invoke(this);

			return this;
		}

		ITempTableBulkCopyTypeConfigBuilder ITempTableBulkCopyTypeConfigBuilder.WithMaxBatchSize(int value)
		{
			_options = _options with { MaxBatchSize = value };
			return this;
		}

		ITempTableBulkCopyTypeConfigBuilder ITempTableBulkCopyTypeConfigBuilder.WithBulkCopyTimeout(int value)
		{
			_options = _options with { BulkCopyTimeout = value };
			return this;
		}

		public BulkCopyOptions Build() => _options;
	}
}
