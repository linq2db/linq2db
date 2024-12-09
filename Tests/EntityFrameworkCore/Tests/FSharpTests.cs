#if !NETFRAMEWORK && !NET9_0
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore.FSharp;
using EntityFrameworkCore.FSharp;

using NUnit.Framework;
using System;
using System.Linq;
using LinqToDB.Mapping;
using Tests;

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

		#region Issue 260

		[Table]
		public class Issue4646Table
		{
			[Identity]
			public int Id { get; set; }
			[Column]
			public int? Value { get; set; }
			[Column]
			public int? ValueN { get; set; }
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/260")]
		public void Issue4646TestLinqToDB([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			FSharpTestMethods.Issue4646TestLinqToDB(ctx);

			var result = db.GetTable<Issue4646Table>().Single();

			Assert.Multiple(() =>
			{
				Assert.That(result.Value, Is.Null);
				Assert.That(result.ValueN, Is.Null);
			});
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4646")]
		public void Issue4646TestEF([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			FSharpTestMethods.Issue4646TestEF(ctx);
			ctx.SaveChanges();

			var result = db.GetTable<Issue4646Table>().Single();

			Assert.Multiple(() =>
			{
				Assert.That(result.Value, Is.Null);
				Assert.That(result.ValueN, Is.Null);
			});
		}

		#endregion
	}
}

#endif
