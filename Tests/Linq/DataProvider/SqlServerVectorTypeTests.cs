using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Tools;

using Microsoft.Data.SqlTypes;

using NUnit.Framework;

namespace Tests.DataProvider
{
	public sealed  class SqlServerVectorTypeTests : TypeTestsBase
	{
		[Test]
		public async ValueTask VectorDistanceSqlTest([IncludeDataSources(TestProvName.SqlServer2025MS)] string context)
		{
			await using var db = GetDataContext(context);

			var vectors = new[]
			{
				new { Vector = new SqlVector<float>(new[] { 1.0f, 2.0f, 3.0f }.AsMemory()) },
				new { Vector = new SqlVector<float>(new[] { 4.0f, 5.0f, 6.0f }.AsMemory()) }
			};

			await using var tmp = db.CreateTempTable(
				vectors,
				fmb => fmb
					.Property(t => t.Vector)
						.HasLength(3));

			var q =
				from t in tmp
				orderby SqlFn.VectorDistance(SqlFn.DistanceMetric.Cosine, t.Vector, vectors[0].Vector)
				select new
				{
					t,
					Cosine1    = SqlFn.VectorDistance   (SqlFn.DistanceMetric.Cosine, t.Vector, vectors[0].Vector),
					Cosine2    = t.Vector.VectorDistance(SqlFn.DistanceMetric.Cosine, vectors[0].Vector),
					Cosine3    = t.Vector.CosineVectorDistance   (vectors[0].Vector),
					Euclidean1 = t.Vector.VectorDistance(SqlFn.DistanceMetric.Euclidean, vectors[0].Vector),
					Euclidean2 = t.Vector.EuclideanVectorDistance(vectors[0].Vector),
					Dot1       = t.Vector.DotVectorDistance      (vectors[0].Vector),
					Dot2       = t.Vector.VectorDistance(SqlFn.DistanceMetric.Dot, vectors[0].Vector),
				};

			var list = await q.ToListAsync();
			var v    = list[0];

			using (Assert.EnterMultipleScope())
			{
				Assert.That(v.Cosine1,    Is.EqualTo(v.Cosine2).And.EqualTo(v.Cosine3));
				Assert.That(v.Euclidean1, Is.EqualTo(v.Euclidean2));
				Assert.That(v.Dot1,       Is.EqualTo(v.Dot2));
			}

			await TestContext.Out.WriteLineAsync(list.ToDiagnosticString());
		}

		[Test]
		public async ValueTask VectorDistanceFloatTest([IncludeDataSources(TestProvName.SqlServer2025MS)] string context)
		{
			await using var db = GetDataContext(context);

			var vectors = new[]
			{
				new { Vector = new[] { 1.0f, 2.0f, 3.0f } },
				new { Vector = new[] { 4.0f, 5.0f, 6.0f } }
			};

			await using var tmp = db.CreateTempTable(
				vectors,
				fmb => fmb
					.Property(t => t.Vector)
						.HasLength(3));

			var q =
				from t in tmp
				orderby
					SqlFn.VectorDistance   (SqlFn.DistanceMetric.Cosine, t.Vector, vectors[0].Vector),
					t.Vector.VectorDistance(SqlFn.DistanceMetric.Cosine, vectors[0].Vector)
				select new
				{
					t,
					Cosine1    = SqlFn.VectorDistance   (SqlFn.DistanceMetric.Cosine, t.Vector, vectors[0].Vector),
					Cosine2    = t.Vector.VectorDistance(SqlFn.DistanceMetric.Cosine, vectors[0].Vector),
					Cosine3    = t.Vector.CosineVectorDistance   (vectors[0].Vector),
					Euclidean1 = t.Vector.VectorDistance(SqlFn.DistanceMetric.Euclidean, vectors[0].Vector),
					Euclidean2 = t.Vector.EuclideanVectorDistance(vectors[0].Vector),
					Dot1       = t.Vector.DotVectorDistance      (vectors[0].Vector),
					Dot2       = t.Vector.VectorDistance(SqlFn.DistanceMetric.Dot, vectors[0].Vector),
				};

			var list = await q.ToListAsync();
			var v    = list[0];

			using (Assert.EnterMultipleScope())
			{
				Assert.That(v.Cosine1,    Is.EqualTo(v.Cosine2).And.EqualTo(v.Cosine3));
				Assert.That(v.Euclidean1, Is.EqualTo(v.Euclidean2));
				Assert.That(v.Dot1,       Is.EqualTo(v.Dot2));
			}

			await TestContext.Out.WriteLineAsync(list.ToDiagnosticString());
		}

		[Test]
		public async ValueTask VectorNormSqlTest([IncludeDataSources(TestProvName.SqlServer2025MS)] string context)
		{
			await using var db = GetDataContext(context);

			var vectors = new[]
			{
				new { Vector = new SqlVector<float>(new[] { 1.0f, 2.0f, 3.0f }.AsMemory()) },
				new { Vector = new SqlVector<float>(new[] { 4.0f, 5.0f, 6.0f }.AsMemory()) }
			};

			await using var tmp = db.CreateTempTable(
				vectors,
				fmb => fmb
					.Property(t => t.Vector)
					.HasLength(3));

			var q =
				from t in tmp
				orderby t.Vector.CosineVectorDistance(vectors[0].Vector)
				select new
				{
					t,
					Norm11   = t.Vector.VectorNorm(SqlFn.NormType.Norm1),
					Norm12   = t.Vector.VectorNorm1(),
					Norm21   = t.Vector.VectorNorm(SqlFn.NormType.Norm2),
					Norm22   = t.Vector.VectorNorm2(),
					NormInf1 = t.Vector.VectorNorm(SqlFn.NormType.NormInf),
					NormInf2 = t.Vector.VectorNormInf(),
				};

			var list = await q.ToListAsync();
			var v    = list[0];

			using (Assert.EnterMultipleScope())
			{
				Assert.That(v.Norm11,   Is.EqualTo(v.Norm12));
				Assert.That(v.Norm21,   Is.EqualTo(v.Norm22));
				Assert.That(v.NormInf1, Is.EqualTo(v.NormInf2));
			}

			await TestContext.Out.WriteLineAsync(list.ToDiagnosticString());
		}

		[Test]
		public async ValueTask VectorNormFloatTest([IncludeDataSources(TestProvName.SqlServer2025MS)] string context)
		{
			await using var db = GetDataContext(context);

			var vectors = new[]
			{
				new { Vector = new[] { 1.0f, 2.0f, 3.0f }},
				new { Vector = new[] { 4.0f, 5.0f, 6.0f }}
			};

			await using var tmp = db.CreateTempTable(
				vectors,
				fmb => fmb
					.Property(t => t.Vector)
					.HasLength(3));

			var q =
				from t in tmp
				orderby t.Vector.CosineVectorDistance(vectors[0].Vector)
				select new
				{
					t,
					Norm11   = t.Vector.VectorNorm(SqlFn.NormType.Norm1),
					Norm12   = t.Vector.VectorNorm1(),
					Norm21   = t.Vector.VectorNorm(SqlFn.NormType.Norm2),
					Norm22   = t.Vector.VectorNorm2(),
					NormInf1 = t.Vector.VectorNorm(SqlFn.NormType.NormInf),
					NormInf2 = t.Vector.VectorNormInf(),
				};

			var list = await q.ToListAsync();
			var v    = list[0];

			using (Assert.EnterMultipleScope())
			{
				Assert.That(v.Norm11,   Is.EqualTo(v.Norm12));
				Assert.That(v.Norm21,   Is.EqualTo(v.Norm22));
				Assert.That(v.NormInf1, Is.EqualTo(v.NormInf2));
			}

			await TestContext.Out.WriteLineAsync(list.ToDiagnosticString());
		}
	}
}
