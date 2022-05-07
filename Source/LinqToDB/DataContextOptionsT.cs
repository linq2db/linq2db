using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB
{
	using Infrastructure;

	/// <summary>
	/// The options to be used by a <see cref="IDataContext" />.
	/// to create instances of this class and it is not designed to be directly constructed in your application code.
	/// </summary>
	/// <typeparam name="TContext"> The type of the context these options apply to. </typeparam>
	public class DataContextOptions<TContext> : DataContextOptions
		where TContext : IDataContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataContextOptions{TContext}" /> class.
		/// to create instances of this class and it is not designed to be directly constructed in your application code.
		/// </summary>
		public DataContextOptions()
			: base(new Dictionary<Type, IDataContextOptionsExtension>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataContextOptions{TContext}" /> class.
		/// </summary>
		/// <param name="extensions"> The extensions that store the configured options. </param>
		public DataContextOptions(IReadOnlyDictionary<Type,IDataContextOptionsExtension> extensions)
			: base(extensions)
		{
		}

		/// <summary>
		/// Adds the given extension to the underlying options and creates a new <see cref="DataContextOptions" /> with the extension added.
		/// </summary>
		/// <typeparam name="TExtension"> The type of extension to be added. </typeparam>
		/// <param name="extension"> The extension to be added. </param>
		/// <returns> The new options instance with the given extension added. </returns>
		public override DataContextOptions WithExtension<TExtension>(TExtension extension)
		{
			if (extension == null)
				throw new ArgumentNullException(nameof(extension));

			var extensions = Extensions.ToDictionary(p => p.GetType(), p => p);

			extensions[typeof(TExtension)] = extension;

			return new DataContextOptions<TContext>(extensions);
		}

		/// <summary>
		/// The type of context that these options are for (<typeparamref name="TContext" />).
		/// </summary>
		public override Type ContextType => typeof(TContext);

		public override bool IsValidForDataContext(Type contextType)
		{
			return typeof(TContext).IsAssignableFrom(contextType);
		}
	}
}
