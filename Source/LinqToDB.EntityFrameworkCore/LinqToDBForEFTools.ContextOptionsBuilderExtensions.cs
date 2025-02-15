using System;

using LinqToDB.EntityFrameworkCore.Internal;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LinqToDB.EntityFrameworkCore
{
	public static partial class LinqToDBForEFTools
	{
		/// <summary>
		/// Registers custom options related to LinqToDB provider.
		/// </summary>
		/// <param name="optionsBuilder"></param>
		/// <param name="linq2dbOptionsAction">Custom options action.</param>
		/// <returns></returns>
		public static TContext UseLinqToDB<TContext>(
			this TContext optionsBuilder,
			Action<LinqToDBContextOptionsBuilder>? linq2dbOptionsAction = null)
			where TContext : DbContextOptionsBuilder
		{
			((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
				.AddOrUpdateExtension(GetOrCreateExtension(optionsBuilder));

			linq2dbOptionsAction?.Invoke(new LinqToDBContextOptionsBuilder(optionsBuilder));

			return optionsBuilder;
		}

		private static LinqToDBOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder options)
			=> options.Options.FindExtension<LinqToDBOptionsExtension>()
				?? new LinqToDBOptionsExtension();
	}
}
