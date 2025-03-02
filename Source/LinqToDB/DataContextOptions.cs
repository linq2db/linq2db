using System.Collections.Generic;

using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.Options;
using LinqToDB.Remote;

namespace LinqToDB
{
	/// <param name="CommandTimeout">
	/// The command timeout, or <c>null</c> if none has been set.
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
		: IOptionSet, IApplicable<DataConnection>, IApplicable<DataContext>, IApplicable<RemoteDataContextBase>
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

		#region IEquatable implementation

		public bool Equals(DataContextOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IOptionSet)this).ConfigurationID == ((IOptionSet)other).ConfigurationID;
		}

		public override int GetHashCode()
		{
			return ((IOptionSet)this).ConfigurationID;
		}

		#endregion
	}
}
