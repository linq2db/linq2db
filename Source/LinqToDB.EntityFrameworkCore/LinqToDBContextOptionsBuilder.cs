using System;

using LinqToDB.EntityFrameworkCore.Internal;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore
{
	/// <summary>
	/// Linq To DB context options builder
	/// </summary>
	public class LinqToDBContextOptionsBuilder
	{
		private readonly LinqToDBOptionsExtension? _extension;

		/// <summary>
		/// Db context options.
		/// </summary>
		public DbContextOptions DbContextOptions { get; private set; }

		/// <summary>
		/// .ctor
		/// </summary>
		/// <param name="optionsBuilder"></param>
		public LinqToDBContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
		{
			_extension = optionsBuilder.Options.FindExtension<LinqToDBOptionsExtension>();
			DbContextOptions = optionsBuilder.Options;
		}

		/// <summary>
		/// Registers Linq To DB interceptor.
		/// </summary>
		/// <param name="interceptor">The interceptor instance to register.</param>
		/// <returns></returns>
		public LinqToDBContextOptionsBuilder AddInterceptor(IInterceptor interceptor)
		{
			if (_extension != null)
				_extension.Options = _extension.Options.UseInterceptor(interceptor);

			return this;
		}

		/// <summary>
		/// Registers custom Linq To DB MappingSchema.
		/// </summary>
		/// <param name="mappingSchema">The interceptor instance to register.</param>
		/// <returns></returns>
		public LinqToDBContextOptionsBuilder AddMappingSchema(MappingSchema mappingSchema)
		{
			if (_extension != null)
				_extension.Options = _extension.Options.UseMappingSchema(mappingSchema);

			return this;
		}

		/// <summary>
		/// Registers custom Linq To DB options.
		/// </summary>
		/// <param name="optionsSetter">Function to setup custom Linq To DB options.</param>
		/// <returns></returns>
		public LinqToDBContextOptionsBuilder AddCustomOptions(Func<DataOptions, DataOptions> optionsSetter)
		{
			if (_extension != null)
				_extension.Options = optionsSetter(_extension.Options);

			return this;
		}
	}
}
