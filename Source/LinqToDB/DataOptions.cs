using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.Remote;

namespace LinqToDB
{
	/// <summary>
	/// Immutable context configuration object.
	/// </summary>
	public sealed class DataOptions : OptionsContainer<DataOptions>, IConfigurationID, IEquatable<DataOptions>, ICloneable
	{
		public DataOptions()
		{
		}

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

		public LinqOptions        LinqOptions        => _linqOptions        ??= LinqOptions.       Default;
		public RetryPolicyOptions RetryPolicyOptions => _retryPolicyOptions ??= RetryPolicyOptions.Default;
		public ConnectionOptions  ConnectionOptions  => _connectionOptions  ??= ConnectionOptions. Default;
		public DataContextOptions DataContextOptions => _dataContextOptions ??= DataContextOptions.Default;
		public BulkCopyOptions    BulkCopyOptions    => _bulkCopyOptions    ??= BulkCopyOptions.   Default;
		public SqlOptions         SqlOptions         => _sqlOptions         ??= SqlOptions.        Default;

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

		[Pure]
		public override TSet? Find<TSet>()
			where TSet : class
		{
			var type = typeof(TSet);

			if (type == typeof(LinqOptions))        return (TSet?)(IOptionSet?)LinqOptions;
			if (type == typeof(RetryPolicyOptions)) return (TSet?)(IOptionSet?)RetryPolicyOptions;
			if (type == typeof(ConnectionOptions))  return (TSet?)(IOptionSet?)ConnectionOptions;
			if (type == typeof(DataContextOptions)) return (TSet?)(IOptionSet?)DataContextOptions;
			if (type == typeof(BulkCopyOptions))    return (TSet?)(IOptionSet?)BulkCopyOptions;
			if (type == typeof(SqlOptions))         return (TSet?)(IOptionSet?)SqlOptions;

			return base.Find<TSet>();
		}

		public void Apply(DataConnection dataConnection)
		{
			((IApplicable<DataConnection>)ConnectionOptions). Apply(dataConnection);
			((IApplicable<DataConnection>)RetryPolicyOptions).Apply(dataConnection);

			if (_dataContextOptions is IApplicable<DataConnection> a)
				a.Apply(dataConnection);

			base.Apply(dataConnection);
		}

		public Action? Reapply(DataConnection dataConnection, DataOptions previousOptions)
		{
			Action? actions = null;

			if (!ReferenceEquals(ConnectionOptions, previousOptions.ConnectionOptions))
				Add(((IReapplicable<DataConnection>)ConnectionOptions). Apply(dataConnection, previousOptions.ConnectionOptions));

			if (!ReferenceEquals(RetryPolicyOptions, previousOptions.RetryPolicyOptions))
				Add(((IReapplicable<DataConnection>)RetryPolicyOptions).Apply(dataConnection, previousOptions.RetryPolicyOptions));

			if (!ReferenceEquals(_dataContextOptions, previousOptions._dataContextOptions))
			{
				if (_dataContextOptions is IReapplicable<DataConnection> a)
				{
					Add(a.Apply(dataConnection, previousOptions._dataContextOptions));
				}
				else if (previousOptions._dataContextOptions is not null)
				{
					Add(((IReapplicable<DataConnection>)DataContextOptions.Default).Apply(dataConnection, previousOptions._dataContextOptions));
				}
			}

			Add(base.Reapply(dataConnection, previousOptions));

			return actions;

			void Add(Action? action)
			{
				actions += action;
			}
		}

		public void Apply(DataContext dataContext)
		{
			((IApplicable<DataContext>)ConnectionOptions).Apply(dataContext);

			if (_dataContextOptions is IApplicable<DataContext> a)
				a.Apply(dataContext);

			base.Apply(dataContext);
		}

		public Action? Reapply(DataContext dataContext, DataOptions previousOptions)
		{
			Action? actions = null;

			if (!ReferenceEquals(ConnectionOptions, previousOptions.ConnectionOptions))
				Add(((IReapplicable<DataContext>)ConnectionOptions). Apply(dataContext, previousOptions.ConnectionOptions));

			if (!ReferenceEquals(_dataContextOptions, previousOptions._dataContextOptions))
			{
				if (_dataContextOptions is IReapplicable<DataContext> a)
				{
					Add(a.Apply(dataContext, previousOptions._dataContextOptions));
				}
				else if (previousOptions._dataContextOptions is not null)
				{
					Add(((IReapplicable<DataContext>)DataContextOptions.Default).Apply(dataContext, previousOptions._dataContextOptions));
				}
			}

			Add(base.Reapply(dataContext, previousOptions));

			return actions;

			void Add(Action? action)
			{
				actions += action;
			}
		}

		public void Apply(RemoteDataContextBase dataContext)
		{
			((IApplicable<RemoteDataContextBase>)ConnectionOptions).Apply(dataContext);

			if (_dataContextOptions is IApplicable<RemoteDataContextBase> a)
				a.Apply(dataContext);

			base.Apply(dataContext);
		}

		public Action? Reapply(RemoteDataContextBase dataContext, DataOptions previousOptions)
		{
			Action? actions = null;

			if (!ReferenceEquals(ConnectionOptions, previousOptions.ConnectionOptions))
				Add(((IReapplicable<RemoteDataContextBase>)ConnectionOptions). Apply(dataContext, previousOptions.ConnectionOptions));

			if (!ReferenceEquals(_dataContextOptions, previousOptions._dataContextOptions))
			{
				if (_dataContextOptions is IReapplicable<RemoteDataContextBase> a)
				{
					Add(a.Apply(dataContext, previousOptions._dataContextOptions));
				}
				else if (previousOptions._dataContextOptions is not null)
				{
					Add(((IReapplicable<RemoteDataContextBase>)DataContextOptions.Default).Apply(dataContext, previousOptions._dataContextOptions));
				}
			}

			Add(base.Reapply(dataContext, previousOptions));

			return actions;

			void Add(Action? action)
			{
				actions += action;
			}
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
