using System;

namespace LinqToDB
{
	/// <summary>
	/// <para>
	/// Provides a simple API surface for configuring <see cref="DataContextOptions{TContext}" />. Databases (and other extensions)
	/// typically define extension methods on this object that allow you to configure the database connection (and other
	/// options) to be used for a context.
	/// </para>
	/// </summary>
	/// <typeparam name="TContext">The type of context to be configured.</typeparam>
	public class DataContextOptionsBuilder<TContext> : DataContextOptionsBuilder
		where TContext : IDataContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataContextOptionsBuilder{TContext}" /> class with no options set.
		/// </summary>
		public DataContextOptionsBuilder()
			: this(new DataContextOptions<TContext>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataContextOptionsBuilder{TContext}" /> class to further configure
		/// a given <see cref="DataContextOptions" />.
		/// </summary>
		/// <param name="options">The options to be configured.</param>
		public DataContextOptionsBuilder(DataContextOptions<TContext> options)
			: base(options)
		{
		}

		/// <summary>
		/// Gets the options being configured.
		/// </summary>
		public new virtual DataContextOptions<TContext> Options => (DataContextOptions<TContext>)base.Options;

		/// <summary>
		/// <para>
		/// Replaces the internal linq2db implementation of a service contract with a different implementation.
		/// </para>
		/// <para>
		/// The replacement service gets the same scope as the EF service that it is replacing.
		/// </para>
		/// </summary>
		/// <typeparam name="TService">The type (usually an interface) that defines the contract of the service to replace.</typeparam>
		/// <typeparam name="TImplementation"> The new implementation type for the service.</typeparam>
		/// <returns> The same builder instance so that multiple calls can be chained. </returns>
		public new virtual DataContextOptionsBuilder<TContext> ReplaceService<TService,TImplementation>()
			where TImplementation : TService
		{
			return (DataContextOptionsBuilder<TContext>)base.ReplaceService<TService,TImplementation>();
		}
	}
}
