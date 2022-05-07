using System;
using System.Collections.Generic;

namespace LinqToDB
{
	using Common.Internal;
	using Infrastructure;

	/// <summary>
	/// The options to be used by a <see cref="IDataContext" />.
	/// </summary>
	public abstract class DataContextOptions : IDataContextOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataContextOptions" /> class. You normally use a <see cref="DataContextOptionsBuilder" />
		/// to create instances of this class and it is not designed to be directly constructed in your application code.
		/// </summary>
		/// <param name="extensions"> The extensions that store the configured options. </param>
		protected DataContextOptions(IReadOnlyDictionary<Type,IDataContextOptionsExtension> extensions)
		{
			_extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
		}

		readonly IReadOnlyDictionary<Type,IDataContextOptionsExtension> _extensions;

		/// <summary>
		/// Gets the extensions that store the configured options.
		/// </summary>
		public virtual IEnumerable<IDataContextOptionsExtension> Extensions => _extensions.Values;

		/// <summary>
		/// Gets the extension of the specified type. Returns null if no extension of the specified type is configured.
		/// </summary>
		/// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
		/// <returns> The extension, or null if none was found. </returns>
		public virtual TExtension? FindExtension<TExtension>()
			where TExtension : class, IDataContextOptionsExtension
		{
			return _extensions.TryGetValue(typeof(TExtension), out var extension) ? (TExtension?)extension : null;
		}

		/// <summary>
		/// Gets the extension of the specified type. Throws if no extension of the specified type is configured.
		/// </summary>
		/// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
		/// <returns> The extension. </returns>
		public virtual TExtension GetExtension<TExtension>()
			where TExtension : class, IDataContextOptionsExtension
		{
			var extension = FindExtension<TExtension>();
			return extension ?? throw new InvalidOperationException($"Options extension of type '{typeof(TExtension).ShortDisplayName()}' not found.");
		}

		/// <summary>
		/// Adds the given extension to the underlying options and creates a new
		/// <see cref="DataContextOptions" /> with the extension added.
		/// </summary>
		/// <typeparam name="TExtension"> The type of extension to be added. </typeparam>
		/// <param name="extension"> The extension to be added. </param>
		/// <returns> The new options instance with the given extension added. </returns>
		public abstract DataContextOptions WithExtension<TExtension>(TExtension extension)
			where TExtension : class, IDataContextOptionsExtension;

		/// <summary>
		/// The type of context that these options are for. Will return <see cref="IDataContext" /> if the
		/// options are not built for a specific derived context.
		/// </summary>
		public abstract Type ContextType { get; }

		/// <summary>
		/// Specifies that no further configuration of this options object should occur.
		/// </summary>
		public virtual void Freeze() => IsFrozen = true;

		/// <summary>
		/// Returns true if <see cref="Freeze" />. has been called.
		/// </summary>
		public virtual bool IsFrozen { get; private set; }

		public abstract bool IsValidForDataContext(Type contextType);
	}
}
