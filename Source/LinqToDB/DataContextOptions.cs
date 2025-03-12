using System;
using System.Collections.Generic;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Linq.Translation;
using LinqToDB.Remote;

namespace LinqToDB
{
	/// <param name="CommandTimeout">
	/// The command timeout in seconds, or <c>null</c> if none has been set.
	/// Negative timeout value means that default timeout will be used.
	/// 0 timeout value corresponds to infinite timeout.
	/// By default, timeout is not set and default value for current provider used.
	/// </param>
	/// <param name="Interceptors">
	/// Gets Interceptors to use with <see cref="DataConnection"/> instance.
	/// </param>
	public sealed record DataContextOptions
	(
		int?                              CommandTimeout    = default,
		IReadOnlyList<IInterceptor>?      Interceptors      = default,
		IReadOnlyList<IMemberTranslator>? MemberTranslators = default

		// If you add another parameter here, don't forget to update
		// DataContextOptions copy constructor and IConfigurationID.ConfigurationID.
	)
		: IOptionSet,
			IApplicable<DataConnection>, IApplicable<DataContext>, IApplicable<RemoteDataContextBase>,
			IReapplicable<DataConnection>, IReapplicable<DataContext>, IReapplicable<RemoteDataContextBase>
	{
		public DataContextOptions() : this((int?)default)
		{
		}

		DataContextOptions(DataContextOptions original)
		{
			CommandTimeout    = original.CommandTimeout;
			Interceptors      = original.Interceptors;
			MemberTranslators = original.MemberTranslators;
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
						.Add(CommandTimeout)
						.AddTypes(Interceptors)
						.AddRange(MemberTranslators)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		public static readonly DataContextOptions Empty = new();

		IOptionSet IOptionSet.Default => Empty;

		#region IApplicable implementation

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
			DataConnection.ConfigurationApplier.Apply(obj, this);
		}

		void IApplicable<DataContext>.Apply(DataContext obj)
		{
			DataContext.ConfigurationApplier.Apply(obj, this);
		}

		void IApplicable<RemoteDataContextBase>.Apply(RemoteDataContextBase obj)
		{
			RemoteDataContextBase.ConfigurationApplier.Apply(obj, this);
		}

		#endregion

		#region IReapplicable implementation

		Action? IReapplicable<DataConnection>.Apply(DataConnection obj, object? previousObject)
		{
			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID?)previousObject)?.ConfigurationID
				? null
				: DataConnection.ConfigurationApplier.Reapply(obj, this, (DataContextOptions?)previousObject);
		}

		Action? IReapplicable<DataContext>.Apply(DataContext obj, object? previousObject)
		{
			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID?)previousObject)?.ConfigurationID
				? null
				: DataContext.ConfigurationApplier.Reapply(obj, this, (DataContextOptions?)previousObject);
		}

		Action? IReapplicable<RemoteDataContextBase>.Apply(RemoteDataContextBase obj, object? previousObject)
		{
			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID?)previousObject)?.ConfigurationID
				? null
				: RemoteDataContextBase.ConfigurationApplier.Reapply(obj, this, (DataContextOptions?)previousObject);
		}

		#endregion

		public override int GetHashCode()
		{
			return ((IConfigurationID)this).ConfigurationID;
		}

		#region IEquatable implementation

		public bool Equals(DataContextOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID)other).ConfigurationID;
		}

		#endregion
	}
}
