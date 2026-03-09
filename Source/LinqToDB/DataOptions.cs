using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Options;
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
			ConnectionOptions = connectionOptions;
		}

		DataOptions(DataOptions options) : base(options)
		{
			LinqOptions        = options.LinqOptions;
			RetryPolicyOptions = options.RetryPolicyOptions;
			ConnectionOptions  = options.ConnectionOptions;
			DataContextOptions = options.DataContextOptions;
			BulkCopyOptions    = options.BulkCopyOptions;
			SqlOptions         = options.SqlOptions;
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
			return options switch
			{
				LinqOptions        lo  => ReferenceEquals(LinqOptions,        lo)  ? this : new(this) { LinqOptions        = lo  },
				ConnectionOptions  co  => ReferenceEquals(ConnectionOptions,  co)  ? this : new(this) { ConnectionOptions  = co  },
				DataContextOptions dco => ReferenceEquals(DataContextOptions, dco) ? this : new(this) { DataContextOptions = dco },
				SqlOptions         so  => ReferenceEquals(SqlOptions,         so)  ? this : new(this) { SqlOptions         = so  },
				BulkCopyOptions    bco => ReferenceEquals(BulkCopyOptions,    bco) ? this : new(this) { BulkCopyOptions    = bco },
				RetryPolicyOptions rp  => ReferenceEquals(RetryPolicyOptions, rp)  ? this : new(this) { RetryPolicyOptions = rp  },
				_                      => base.WithOptions(options),
			};
		}

		public LinqOptions        LinqOptions        { get => field ??= LinqOptions.       Default; private set; }
		public RetryPolicyOptions RetryPolicyOptions { get => field ??= RetryPolicyOptions.Default; private set; }
		public ConnectionOptions  ConnectionOptions  { get => field ??= ConnectionOptions. Default; private set; }
		public DataContextOptions DataContextOptions { get => field ??= DataContextOptions.Default; private set; }
		public BulkCopyOptions    BulkCopyOptions    { get => field ??= BulkCopyOptions.   Default; private set; }
		public SqlOptions         SqlOptions         { get => field ??= SqlOptions.        Default; private set; }

		public override IEnumerable<IOptionSet> OptionSets
		{
			get
			{
				yield return LinqOptions;
				yield return RetryPolicyOptions;
				yield return ConnectionOptions;
				yield return SqlOptions;

				if (DataContextOptions != null)
					yield return DataContextOptions;

				if (BulkCopyOptions != null)
					yield return BulkCopyOptions;

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

			if (DataContextOptions is IApplicable<DataConnection> a)
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

			if (!ReferenceEquals(DataContextOptions, previousOptions.DataContextOptions))
			{
				if (DataContextOptions is IReapplicable<DataConnection> a)
				{
					Add(a.Apply(dataConnection, previousOptions.DataContextOptions));
				}
				else if (previousOptions.DataContextOptions is not null)
				{
					Add(((IReapplicable<DataConnection>)DataContextOptions.Default).Apply(dataConnection, previousOptions.DataContextOptions));
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

			if (DataContextOptions is IApplicable<DataContext> a)
				a.Apply(dataContext);

			base.Apply(dataContext);
		}

		public Action? Reapply(DataContext dataContext, DataOptions previousOptions)
		{
			Action? actions = null;

			if (!ReferenceEquals(ConnectionOptions, previousOptions.ConnectionOptions))
				Add(((IReapplicable<DataContext>)ConnectionOptions). Apply(dataContext, previousOptions.ConnectionOptions));

			if (!ReferenceEquals(DataContextOptions, previousOptions.DataContextOptions))
			{
				if (DataContextOptions is IReapplicable<DataContext> a)
				{
					Add(a.Apply(dataContext, previousOptions.DataContextOptions));
				}
				else if (previousOptions.DataContextOptions is not null)
				{
					Add(((IReapplicable<DataContext>)DataContextOptions.Default).Apply(dataContext, previousOptions.DataContextOptions));
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

			if (DataContextOptions is IApplicable<RemoteDataContextBase> a)
				a.Apply(dataContext);

			base.Apply(dataContext);
		}

		public Action? Reapply(RemoteDataContextBase dataContext, DataOptions previousOptions)
		{
			Action? actions = null;

			if (!ReferenceEquals(ConnectionOptions, previousOptions.ConnectionOptions))
				Add(((IReapplicable<RemoteDataContextBase>)ConnectionOptions). Apply(dataContext, previousOptions.ConnectionOptions));

			if (!ReferenceEquals(DataContextOptions, previousOptions.DataContextOptions))
			{
				if (DataContextOptions is IReapplicable<RemoteDataContextBase> a)
				{
					Add(a.Apply(dataContext, previousOptions.DataContextOptions));
				}
				else if (previousOptions.DataContextOptions is not null)
				{
					Add(((IReapplicable<RemoteDataContextBase>)DataContextOptions.Default).Apply(dataContext, previousOptions.DataContextOptions));
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
