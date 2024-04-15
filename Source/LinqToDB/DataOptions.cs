using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Common;
	using Common.Internal;
	using Data;
	using Data.RetryPolicy;
	using Interceptors;

	/// <summary>
	/// Immutable context configuration object.
	/// </summary>
	public sealed class DataOptions : OptionsContainer<DataOptions>, IConfigurationID, IEquatable<DataOptions>, ICloneable
	{
		public DataOptions()
		{
		}

		IReadOnlyCollection<IInterceptor>? _interceptors;

		/// <summary>
		/// Contains registered interceptors.
		/// </summary>
		public IEnumerable<IInterceptor> Interceptors => _interceptors ?? Enumerable.Empty<IInterceptor>();

		public DataOptions(ConnectionOptions connectionOptions)
		{
			_connectionOptions = connectionOptions;
		}

		DataOptions(DataOptions options) : base(options)
		{
			_linqOptions        = options._linqOptions;
			_retryPolicyOptions = options._retryPolicyOptions;
			_connectionOptions  = options._connectionOptions;
			_dataContextOptions = options._dataContextOptions;
			_bulkCopyOptions    = options._bulkCopyOptions;
			_sqlOptions         = options._sqlOptions;
			_interceptors       = options._interceptors;
		}

		protected override DataOptions Clone()
		{
			return new(this);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		[Pure]
		public override DataOptions WithOptions(IOptionSet options)
		{
			switch (options)
			{
				case LinqOptions        lo  : return ReferenceEquals(_linqOptions,        lo)  ? this : new(this) { _linqOptions        = lo  };
				case ConnectionOptions  co  : return ReferenceEquals(_connectionOptions,  co)  ? this : new(this) { _connectionOptions  = co  };
				case DataContextOptions dco : return ReferenceEquals(_dataContextOptions, dco) ? this : new(this) { _dataContextOptions = dco };
				case SqlOptions         so  : return ReferenceEquals(_sqlOptions,         so)  ? this : new(this) { _sqlOptions         = so  };
				case BulkCopyOptions    bco : return ReferenceEquals(_bulkCopyOptions,    bco) ? this : new(this) { _bulkCopyOptions    = bco };
				case RetryPolicyOptions rp  : return ReferenceEquals(_retryPolicyOptions, rp)  ? this : new(this) { _retryPolicyOptions = rp  };
				default                     : return base.WithOptions(options);
			}
		}

		LinqOptions?        _linqOptions;
		RetryPolicyOptions? _retryPolicyOptions;
		ConnectionOptions?  _connectionOptions;
		DataContextOptions? _dataContextOptions;
		BulkCopyOptions?    _bulkCopyOptions;
		SqlOptions?         _sqlOptions;

		public LinqOptions        LinqOptions        => _linqOptions        ??= Common.Configuration.Linq.Options;
		public RetryPolicyOptions RetryPolicyOptions => _retryPolicyOptions ??= Common.Configuration.RetryPolicy.Options;
		public ConnectionOptions  ConnectionOptions  => _connectionOptions  ??= DataConnection.DefaultDataOptions.ConnectionOptions;
		public DataContextOptions DataContextOptions => _dataContextOptions ??= DataContextOptions.Empty;
		public BulkCopyOptions    BulkCopyOptions    => _bulkCopyOptions    ??= BulkCopyOptions.Empty;
		public SqlOptions         SqlOptions         => _sqlOptions         ??= Common.Configuration.Sql.Options;

		public override IEnumerable<IOptionSet> OptionSets
		{
			get
			{
				yield return LinqOptions;
				yield return RetryPolicyOptions;
				yield return ConnectionOptions;
				yield return SqlOptions;

				if (_dataContextOptions != null)
					yield return _dataContextOptions;

				if (_bulkCopyOptions != null)
					yield return _bulkCopyOptions;

				foreach (var item in base.OptionSets)
					yield return item;
			}
		}

		public DataOptions AddInterceptor(IInterceptor interceptor)
		{
			var options = new DataOptions(this) { _interceptors = new List<IInterceptor>(Interceptors) { interceptor } };
			return options;
		}

		public DataOptions AddInterceptors(IEnumerable<IInterceptor> interceptors)
		{
			var options = new DataOptions(this) { _interceptors = new List<IInterceptor>(Interceptors.Concat(interceptors)) };
			return options;
		}

		public DataOptions RemoveInterceptor(IInterceptor interceptor)
		{
			if (_interceptors == null)
				return this;

			var options = new DataOptions(this) { _interceptors = new List<IInterceptor>(Interceptors.Except(new[] { interceptor })) };
			return options;
		}

		[Pure]
		public override TSet? Find<TSet>()
			where TSet : class
		{
			var type = typeof(TSet);

			if (type == typeof(LinqOptions))        return (TSet?)(IOptionSet?)LinqOptions;
			if (type == typeof(RetryPolicyOptions)) return (TSet?)(IOptionSet?)RetryPolicyOptions;
			if (type == typeof(ConnectionOptions))  return (TSet?)(IOptionSet?)ConnectionOptions;
			if (type == typeof(DataContextOptions)) return (TSet?)(IOptionSet?)_dataContextOptions;
			if (type == typeof(BulkCopyOptions))    return (TSet?)(IOptionSet?)_bulkCopyOptions;
			if (type == typeof(SqlOptions))         return (TSet?)(IOptionSet?)_sqlOptions;

			return base.Find<TSet>();
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID
		{
			get
			{
				if (_configurationID == null)
				{
					using var idBuilder = new IdentifierBuilder();
					_configurationID = idBuilder
						.Add(LinqOptions)
						.Add(RetryPolicyOptions)
						.Add(ConnectionOptions)
						.Add(DataContextOptions)
						.Add(BulkCopyOptions)
						.Add(SqlOptions)
						.AddRange(Interceptors)
						.AddRange(base.OptionSets)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		public bool Equals(DataOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID)other).ConfigurationID;
		}

		public override bool Equals(object? obj)
		{
			return obj is DataOptions o && Equals(o);
		}

		public override int GetHashCode()
		{
			return ((IConfigurationID)this).ConfigurationID;
		}

		public static bool operator ==(DataOptions? t1, DataOptions? t2)
		{
			if (ReferenceEquals(t1, t2))
				return true;
			if (t1 is null || t2 is null)
				return false;

			return t1.Equals(t2);
		}

		public static bool operator !=(DataOptions? t1, DataOptions? t2) => !(t1 == t2);
	}
}
