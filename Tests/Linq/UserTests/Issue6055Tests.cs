using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// A typed <see cref="SqlBinaryExpression"/> must keep its <see cref="DbDataType"/> on the remote path, so the server
	/// folds the same non-mandatory casts as the direct path.
	/// https://github.com/linq2db/linq2db/issues/6055
	/// </summary>
	[TestFixture]
	public class Issue6055Tests : TestBase
	{
		[Table]
		sealed class DecimalTable
		{
			[Column, PrimaryKey               ] public int     Id { get; set; }
			[Column(Precision = 10, Scale = 2)] public decimal A  { get; set; }
			[Column(Precision = 10, Scale = 2)] public decimal B  { get; set; }
		}

		// derived from the provider's decimal mapping: ClickHouse has no generic DataType.Decimal type name
		static DbDataType Decimal10_2(MappingSchema mappingSchema) => mappingSchema.GetDbDataType(typeof(decimal)).WithPrecisionScale(10, 2);

		// typed sum cast to its own type: the cast is redundant and must be folded
		sealed class SameTypeCastBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var type = Decimal10_2(builder.Mapping);
				var sum  = new SqlBinaryExpression(type, builder.GetExpression(0)!, "+", builder.GetExpression(1)!, Precedence.Additive);

				builder.ResultExpression = new SqlCastExpression(sum, type, null);
			}
		}

		// typed sum cast to the mapping-schema default decimal: the cast changes the type and must be kept
		sealed class DefaultTypeCastBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var sum = new SqlBinaryExpression(Decimal10_2(builder.Mapping), builder.GetExpression(0)!, "+", builder.GetExpression(1)!, Precedence.Additive);

				builder.ResultExpression = new SqlCastExpression(sum, builder.Mapping.GetDbDataType(typeof(decimal)), null);
			}
		}

		[Sql.Extension("", BuilderType = typeof(SameTypeCastBuilder), ServerSideOnly = true)]
		static decimal SameTypeCast(decimal a, decimal b) => a + b;

		[Sql.Extension("", BuilderType = typeof(DefaultTypeCastBuilder), ServerSideOnly = true)]
		static decimal DefaultTypeCast(decimal a, decimal b) => a + b;

		static readonly DecimalTable[] Data = [new DecimalTable { Id = 1, A = 1.25m, B = 2.5m }];

		// CAST(...), PostgreSQL `::`, DB2 DECIMAL(x, p, s), ClickHouse toDecimalNN(x, s)
		const string CastPattern = @"(?i)\bCAST\b|::|\bDecimal\(|\btoDecimal\d+\(";

		[Test]
		public void RedundantCastIsFolded([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Data);

			table
				.Select(r => SameTypeCast(r.A, r.B))
				.Single()
				.ShouldBe(3.75m);

			var sql = GetCurrentBaselines();

			sql.ShouldContain("+");
			sql.ShouldNotMatch(CastPattern);
		}

		// Access drops a cast to a decimal type on both paths (AccessSqlExpressionConvertVisitor.ConvertConversion),
		// so there is no cast to lose
		[Test]
		public void NarrowingCastIsKept([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Data);

			table
				.Select(r => DefaultTypeCast(r.A, r.B))
				.Single()
				.ShouldBe(3.75m);

			GetCurrentBaselines().ShouldMatch(CastPattern);
		}
	}
}
