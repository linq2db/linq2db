using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.Infrastructure
{
	using LinqToDB.Common.Internal;
	using Interceptors;

	/// <summary>
	/// <para>
	/// Represents options managed by the core of linq2db, as opposed to those managed
	/// by database providers or extensions. These options are set using <see cref="DataContextOptionsBuilder" />.
	/// </para>
	/// <para>
	/// Instances of this class are designed to be immutable. To change an option, call one of the 'With...'
	/// methods to obtain a new instance with the option changed.
	/// </para>
	/// </summary>
	public class CoreDataContextOptionsExtension : IDataContextOptionsExtension
	{
		/// <summary>
		/// Creates a new set of options with everything set to default values.
		/// </summary>
		public CoreDataContextOptionsExtension()
		{
		}

		/// <summary>
		/// Called by a derived class constructor when implementing the <see cref="Clone" /> method.
		/// </summary>
		/// <param name="copyFrom">The instance that is being cloned.</param>
		protected CoreDataContextOptionsExtension(CoreDataContextOptionsExtension copyFrom)
		{
			_interceptors = copyFrom.Interceptors?.ToList();

			if (copyFrom._replacedServices != null)
				_replacedServices = new Dictionary<Type,Type>(copyFrom._replacedServices);
		}

		DataContextOptionsExtensionInfo? _info;

		/// <summary>
		/// Information/metadata about the extension.
		/// </summary>
		public virtual DataContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

		/// <summary>
		/// Override this method in a derived class to ensure that any clone created is also of that class.
		/// </summary>
		/// <returns> A clone of this instance, which can be modified before being returned as immutable. </returns>
		protected virtual CoreDataContextOptionsExtension Clone() => new CoreDataContextOptionsExtension(this);

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="serviceType"> The service contract. </param>
		/// <param name="implementationType"> The implementation type to use for the service. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual CoreDataContextOptionsExtension WithReplacedService(Type serviceType, Type implementationType)
		{
			var clone = Clone();

			clone._replacedServices ??= new Dictionary<Type, Type>();
			clone._replacedServices[serviceType] = implementationType;

			return clone;
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="interceptors"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual CoreDataContextOptionsExtension WithInterceptors(IEnumerable<IInterceptor> interceptors)
		{
			if (interceptors == null)
				throw new ArgumentNullException(nameof(interceptors));

			var clone = Clone();

			clone._interceptors = _interceptors == null
				? interceptors
				: _interceptors.Concat(interceptors);

			return clone;
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="interceptor"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual CoreDataContextOptionsExtension WithInterceptor(IInterceptor interceptor)
		{
			if (interceptor == null)
				throw new ArgumentNullException(nameof(interceptor));

			var clone = Clone();

			clone._interceptors = _interceptors == null
				? new[] { interceptor }
				: _interceptors.Concat(new[] { interceptor });

			return clone;
		}

		IDictionary<Type,Type>? _replacedServices;

		/// <summary>
		/// The options set from the <see cref="DataContextOptionsBuilder.ReplaceService{TService,TImplementation}" /> method.
		/// </summary>
		public virtual IReadOnlyDictionary<Type,Type>? ReplacedServices => (IReadOnlyDictionary<Type,Type>?)_replacedServices;

		IEnumerable<IInterceptor>? _interceptors;

		public virtual IEnumerable<IInterceptor>? Interceptors => _interceptors;

		public void ApplyServices()
		{
		}

		/// <summary>
		/// Gives the extension a chance to validate that all options in the extension are valid.
		/// If options are invalid, then an exception will be thrown.
		/// </summary>
		/// <param name="options"> The options being validated. </param>
		public virtual void Validate(IDataContextOptions options)
		{
		}

		sealed class ExtensionInfo : DataContextOptionsExtensionInfo
		{
			public ExtensionInfo(CoreDataContextOptionsExtension extension)
				: base(extension)
			{
			}

			new CoreDataContextOptionsExtension Extension => (CoreDataContextOptionsExtension)base.Extension;

			public override bool IsDatabaseProvider => false;

			string? _logFragment;

			public override string LogFragment
			{
				get
				{
					if (_logFragment == null)
					{
						var builder = new StringBuilder();

						_logFragment = builder.ToString();
					}

					return _logFragment;
				}
			}

			public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
			{
				if (debugInfo == null)
					throw new ArgumentNullException(nameof(debugInfo));

				if (Extension._replacedServices != null)
				{
					foreach (var replacedService in Extension._replacedServices)
					{
						debugInfo["Core:" + nameof(DataContextOptionsBuilder.ReplaceService) + ":" + replacedService.Key.DisplayName()] =
							replacedService.Value.GetHashCode().ToString(CultureInfo.InvariantCulture);
					}
				}
			}

			long? _serviceProviderHash;

			public override long GetServiceProviderHashCode()
			{
				if (_serviceProviderHash == null)
				{
					var hashCode = 0L;

					if (Extension._replacedServices != null)
					{
						hashCode = Extension._replacedServices.Aggregate(hashCode, (t, e) => (t * 397) ^ e.Value.GetHashCode());
					}

					_serviceProviderHash = hashCode;
				}

				return _serviceProviderHash.Value;
			}
		}
	}
}
