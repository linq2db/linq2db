using System;
using System.Linq;
using System.Text;

namespace LinqToDB.Infrastructure
{
	using DataProvider;

	/// <summary>
	/// <para>
	/// Represents options managed by the relational database providers.
	/// These options are set using <see cref="DataContextOptionsBuilder" />.
	/// </para>
	/// <para>
	/// Instances of this class are designed to be immutable. To change an option, call one of the 'With...'
	/// methods to obtain a new instance with the option changed.
	/// </para>
	/// </summary>
	public abstract class RelationalOptionsExtension : IDataContextOptionsExtension
	{
		// NB: When adding new options, make sure to update the copy constructor below.

		/// <summary>
		/// Creates a new set of options with everything set to default values.
		/// </summary>
		protected RelationalOptionsExtension()
		{
		}

		/// <summary>
		/// Called by a derived class constructor when implementing the <see cref="Clone" /> method.
		/// </summary>
		/// <param name="copyFrom">The instance that is being cloned.</param>
		protected RelationalOptionsExtension(RelationalOptionsExtension copyFrom)
		{
			if (copyFrom == null)
				throw new ArgumentNullException(nameof(copyFrom));
		}

		/// <summary>
		///     Information/metadata about the extension.
		/// </summary>
		public abstract DataContextOptionsExtensionInfo Info { get; }

		/// <summary>
		/// Override this method in a derived class to ensure that any clone created is also of that class.
		/// </summary>
		/// <returns>A clone of this instance, which can be modified before being returned as immutable.</returns>
		protected abstract RelationalOptionsExtension Clone();

		/// <summary>
		/// Finds an existing <see cref="RelationalOptionsExtension" /> registered on the given options
		/// or throws if none has been registered. This is typically used to find some relational
		/// configuration when it is known that a relational provider is being used.
		/// </summary>
		/// <param name="options">The context options to look in.</param>
		/// <returns>The extension.</returns>
		public static RelationalOptionsExtension Extract(IDataContextOptions options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));

			var relationalOptionsExtensions = options.Extensions.OfType<RelationalOptionsExtension>().ToList();

			if (relationalOptionsExtensions.Count == 0)
				throw new InvalidOperationException("No provider configured.");

			if (relationalOptionsExtensions.Count > 1)
				throw new InvalidOperationException("Multiple providers configured.");

			return relationalOptionsExtensions[0];
		}

		/// <summary>
		/// Adds the services required to make the selected options work. This is used when there
		/// is no external <see cref="IServiceProvider" /> and EF is maintaining its own service
		/// provider internally. This allows database providers (and other extensions) to register their
		/// required services when EF is creating an service provider.
		/// </summary>
		public abstract void ApplyServices();

		/// <summary>
		/// Gives the extension a chance to validate that all options in the extension are valid.
		/// Most extensions do not have invalid combinations and so this will be a no-op.
		/// If options are invalid, then an exception should be thrown.
		/// </summary>
		/// <param name="options"> The options being validated. </param>
		public virtual void Validate(IDataContextOptions options)
		{
		}

		/// <summary>
		/// Returns specific to options DataProvider.
		/// </summary>
		public abstract IDataProvider GetDataProvider(DataContextOptionsExtension dbOptions);

		/// <summary>
		/// Information/metadata for a <see cref="RelationalOptionsExtension" />.
		/// </summary>
		protected abstract class RelationalExtensionInfo : DataContextOptionsExtensionInfo
		{
			string? _logFragment;

			/// <summary>
			/// Creates a new <see cref="RelationalExtensionInfo" /> instance containing info/metadata for the given extension.
			/// </summary>
			/// <param name="extension"> The extension. </param>
			protected RelationalExtensionInfo(IDataContextOptionsExtension extension)
				: base(extension)
			{
			}

			/// <summary>
			/// The extension for which this instance contains metadata.
			/// </summary>
			public new virtual RelationalOptionsExtension Extension
				=> (RelationalOptionsExtension)base.Extension;

			/// <summary>
			/// True, since this is a database provider base class.
			/// </summary>
			public override bool IsDatabaseProvider => true;

			/// <summary>
			/// Returns a hash code created from any options that would cause a new <see cref="IServiceProvider" />
			/// to be needed. Most extensions do not have any such options and should return zero.
			/// </summary>
			/// <returns> A hash over options that require a new service provider when changed. </returns>
			public override long GetServiceProviderHashCode() => 0;

			/// <summary>
			/// A message fragment for logging typically containing information about
			/// any useful non-default options that have been configured.
			/// </summary>
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
		}
	}
}
