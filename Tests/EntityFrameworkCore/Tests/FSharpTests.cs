#if EF_FSHARP
using System;
using System.Linq;

using EntityFrameworkCore.FSharp;

using LinqToDB.EntityFrameworkCore.FSharp;
using LinqToDB.Mapping;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

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

		// Both cases below put an option-typed member through the query pipeline, where EntityFrameworkCore
		// .FSharp's own IMemberTranslator runs. That package's last release is 6.0.7 (2022) and it has not
		// followed EF Core since; TestLeftJoin above stays live because it never reaches a member translator.
		const string OptionTranslatorBroken = "EntityFrameworkCore.FSharp 6.0.7 calls ISqlExpressionFactory.Convert(SqlExpression, Type, RelationalTypeMapping), removed in EF Core 10";

		[ActiveIssue(4646, Details = OptionTranslatorBroken)]
		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/260")]
		public void Issue4646TestLinqToDB([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			FSharpTestMethods.Issue4646TestLinqToDB(ctx);

			var result = db.GetTable<Issue4646Table>().Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Value, Is.Null);
				Assert.That(result.ValueN, Is.Null);
			}
		}

		[ActiveIssue(4646, Details = OptionTranslatorBroken)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4646")]
		public void Issue4646TestEF([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			using var db = ctx.CreateLinqToDBConnection();

			FSharpTestMethods.Issue4646TestEF(ctx);
			ctx.SaveChanges();

			var result = db.GetTable<Issue4646Table>().Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Value, Is.Null);
				Assert.That(result.ValueN, Is.Null);
			}
		}

		#endregion
	}
}

#endif
