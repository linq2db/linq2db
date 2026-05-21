using System;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Concrete <see cref="ITempTableConfigBuilder"/> backing the configure lambdas inside the
	/// per-call AsQueryable chain and the DataOptions extensions. Each setter mutates internal
	/// state and returns the same builder so chaining composes; <see cref="Build"/> snapshots
	/// the accumulated state into an immutable <see cref="TempTableSpec"/>. The lambda is
	/// compiled and invoked once at LINQ-translation time (or DataOptions construction time).
	/// </summary>
	sealed class TempTableConfigBuilderImpl : ITempTableConfigBuilder
	{
		int?                                 _threshold;
		bool                                 _disposeWithConnection;
		TempTableBulkCopyOptionsBuilderImpl? _bulkCopyBuilder;

		public ITempTableConfigBuilder Threshold(int value)
		{
			_threshold = value;
			return this;
		}

		public ITempTableConfigBuilder DisposeWithConnection()
		{
			_disposeWithConnection = true;
			return this;
		}

		public ITempTableConfigBuilder ConfigureBulkCopy(Func<ITempTableBulkCopyOptionsBuilder, ITempTableBulkCopyOptionsBuilder> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);

			_bulkCopyBuilder ??= new TempTableBulkCopyOptionsBuilderImpl();
			configure(_bulkCopyBuilder);
			return this;
		}

		public TempTableSpec Build() => new(
			Threshold:             _threshold,
			DisposeWithConnection: _disposeWithConnection,
			BulkCopyOptions:       _bulkCopyBuilder?.Build());
	}
}
