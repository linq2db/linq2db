// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LinqToDB.Common.Internal;
using LinqToDB.Interceptors;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         Represents options managed by the core of Entity Framework, as opposed to those managed
    ///         by database providers or extensions. These options are set using <see cref="DbContextOptionsBuilder" />.
    ///     </para>
    ///     <para>
    ///         Instances of this class are designed to be immutable. To change an option, call one of the 'With...'
    ///         methods to obtain a new instance with the option changed.
    ///     </para>
    /// </summary>
    public class CoreOptionsExtension : IDbContextOptionsExtension
    {
        private IDictionary<Type, Type>?       _replacedServices;
        private DbContextOptionsExtensionInfo? _info;
        private IEnumerable<IInterceptor>?     _interceptors;

        /// <summary>
        ///     Creates a new set of options with everything set to default values.
        /// </summary>
        public CoreOptionsExtension()
        {
        }

        /// <summary>
        ///     Called by a derived class constructor when implementing the <see cref="Clone" /> method.
        /// </summary>
        /// <param name="copyFrom"> The instance that is being cloned. </param>
        protected CoreOptionsExtension(CoreOptionsExtension copyFrom)
        {
            _interceptors = copyFrom.Interceptors?.ToList();

            if (copyFrom._replacedServices != null)
            {
                _replacedServices = new Dictionary<Type, Type>(copyFrom._replacedServices);
            }
        }

        /// <summary>
        ///     Information/metadata about the extension.
        /// </summary>
        public virtual DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        /// <summary>
        ///     Override this method in a derived class to ensure that any clone created is also of that class.
        /// </summary>
        /// <returns> A clone of this instance, which can be modified before being returned as immutable. </returns>
        protected virtual CoreOptionsExtension Clone() => new CoreOptionsExtension(this);

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DbContextOptionsBuilder" />.
        /// </summary>
        /// <param name="serviceType"> The service contract. </param>
        /// <param name="implementationType"> The implementation type to use for the service. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithReplacedService(Type serviceType, Type implementationType)
        {
            var clone = Clone();

            if (clone._replacedServices == null)
            {
                clone._replacedServices = new Dictionary<Type, Type>();
            }

            clone._replacedServices[serviceType] = implementationType;

            return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DbContextOptionsBuilder" />.
        /// </summary>
        /// <param name="interceptors"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithInterceptors(IEnumerable<IInterceptor> interceptors)
        {
	        if (interceptors == null)
	        {
		        throw new ArgumentNullException(nameof(interceptors));
	        }

	        var clone = Clone();

            clone._interceptors = _interceptors == null
                ? interceptors
                : _interceptors.Concat(interceptors);

            return clone;
        }

        /// <summary>
        ///     The options set from the <see cref="DbContextOptionsBuilder.ReplaceService{TService,TImplementation}" /> method.
        /// </summary>
        public virtual IReadOnlyDictionary<Type, Type>? ReplacedServices => (IReadOnlyDictionary<Type, Type>?)_replacedServices;

        public virtual IEnumerable<IInterceptor>? Interceptors => _interceptors;

        public void ApplyServices()
        {
        }

        /// <summary>
        ///     Gives the extension a chance to validate that all options in the extension are valid.
        ///     If options are invalid, then an exception will be thrown.
        /// </summary>
        /// <param name="options"> The options being validated. </param>
        public virtual void Validate(IDbContextOptions options)
        {
        }

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private long?   _serviceProviderHash;
            private string? _logFragment;

            public ExtensionInfo(CoreOptionsExtension extension)
                : base(extension)
            {
            }

            private new CoreOptionsExtension Extension
                => (CoreOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => false;

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
	            {
		            throw new ArgumentNullException(nameof(debugInfo));
	            }

                if (Extension._replacedServices != null)
                {
                    foreach (var replacedService in Extension._replacedServices)
                    {
                        debugInfo["Core:" + nameof(DbContextOptionsBuilder.ReplaceService) + ":" + replacedService.Key.DisplayName()]
                            = replacedService.Value.GetHashCode().ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

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
