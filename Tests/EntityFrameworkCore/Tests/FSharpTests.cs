#if !NETFRAMEWORK
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore.FSharp;
using EntityFrameworkCore.FSharp;

using NUnit.Framework;
using System;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class FSharpTests : ContextTestBase<FSharpContext.AppDbContext>
	{
		protected override FSharpContext.AppDbContext CreateProviderContext(string provider, DbContextOptions<FSharpContext.AppDbContext> options)
		{
			return new FSharpContext.AppDbContext(options);
		}

		protected override DbContextOptionsBuilder<FSharpContext.AppDbContext> ProviderSetup(string provider, string connectionString, DbContextOptionsBuilder<FSharpContext.AppDbContext> optionsBuilder)
		{
			var builder = base.ProviderSetup(provider, connectionString, optionsBuilder);

			FSharpExtensions.WithFSharp(builder);
			return builder;
		}

		[Test]
		public void TestLeftJoin([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			FSharpTestMethods.TestLeftJoin(ctx);
		}
	}
}
#endif
