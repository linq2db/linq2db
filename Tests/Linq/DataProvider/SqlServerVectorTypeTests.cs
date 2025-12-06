using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.DataProvider.SqlServer;
using LinqToDB.Mapping;

using Microsoft.Data.SqlTypes;

using NUnit.Framework;

namespace Tests.DataProvider
{
	public sealed  class SqlServerVectorTypeTests : TestBase
	{
		sealed class SqlVectorTable
		{
			[Column(Length = 3, DataType = DataType.Vector32)]
			public SqlVector<float> Vector { get; set; }

			public static readonly SqlVectorTable[] Data =
			[
				new() { Vector = new SqlVector<float>(new[] { 1.0f, 2.0f, 3.0f }.AsMemory()) },
				new() { Vector = new SqlVector<float>(new[] { 4.0f, 5.0f, 6.0f }.AsMemory()) }
			];
		}

		sealed class VectorTable
		{
			[Column(Length = 3)]
			public float[] Vector { get; set; } = null!;

			public static readonly VectorTable[] Data =
			[
				new() { Vector = new[] { 1.0f, 2.0f, 3.0f } },
				new() { Vector = new[] { 4.0f, 5.0f, 6.0f } }
			];
		}

		[Test]
		public async ValueTask VectorDistanceSqlTest([IncludeDataSources(true, TestProvName.AllSqlServer2025PlusMS)] string context)
		{
			var vectors = SqlVectorTable.Data;

			// TODO: instead of passing schema explicitly in future we need to implement out-of-box support for custom serialization
			await using var db = GetDataContext(context, SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema);
			await using var tb = db.CreateLocalTable(vectors);

			var q =
				from t in tb
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
		}

		[Test]
		public async ValueTask VectorDistanceFloatTest([IncludeDataSources(true, TestProvName.AllSqlServer2025Plus)] string context)
		{
			var vectors = VectorTable.Data;

			await using var db = GetDataContext(context);
			await using var tb = db.CreateLocalTable(vectors);

			var q =
				from t in tb
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
		}

		[Test]
		public async ValueTask VectorNormSqlTest([IncludeDataSources(true, TestProvName.AllSqlServer2025PlusMS)] string context)
		{
			var vectors = SqlVectorTable.Data;

			// TODO: instead of passing schema explicitly in future we need to implement out-of-box support for custom serialization
			await using var db = GetDataContext(context, SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema);
			await using var tb = db.CreateLocalTable(vectors);

			var q =
				from t in tb
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
		}

		[Test]
		public async ValueTask VectorNormFloatTest([IncludeDataSources(true, TestProvName.AllSqlServer2025Plus)] string context)
		{
			var vectors = VectorTable.Data;

			await using var db = GetDataContext(context);
			await using var tb = db.CreateLocalTable(vectors);

			var q =
				from t in tb
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
		}

		[Test]
		public async ValueTask VectorNormalizeSqlTest([IncludeDataSources(true, TestProvName.AllSqlServer2025PlusMS)] string context)
		{
			var vectors = SqlVectorTable.Data;

			// TODO: instead of passing schema explicitly in future we need to implement out-of-box support for custom serialization
			await using var db = GetDataContext(context, SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema);
			await using var tb = db.CreateLocalTable(vectors);

			var q =
				from t in tb
				orderby t.Vector.CosineVectorDistance(vectors[0].Vector)
				select new
				{
					t,
					Norm11   = t.Vector.VectorNormalize(SqlFn.NormType.Norm1),
					Norm12   = t.Vector.VectorNormalize1(),
					Norm21   = t.Vector.VectorNormalize(SqlFn.NormType.Norm2),
					Norm22   = t.Vector.VectorNormalize2(),
					NormInf1 = t.Vector.VectorNormalize(SqlFn.NormType.NormInf),
					NormInf2 = t.Vector.VectorNormalizeInf(),
				};

			var list = await q.ToListAsync();
			var v    = list[0];

			using (Assert.EnterMultipleScope())
			{
				Assert.That(v.Norm11.  Memory.ToArray(), Is.EquivalentTo(v.Norm12.  Memory.ToArray()));
				Assert.That(v.Norm21.  Memory.ToArray(), Is.EquivalentTo(v.Norm22.  Memory.ToArray()));
				Assert.That(v.NormInf1.Memory.ToArray(), Is.EquivalentTo(v.NormInf2.Memory.ToArray()));
			}
		}

		[Test]
		public async ValueTask VectorNormalizeFloatTest([IncludeDataSources(true, TestProvName.AllSqlServer2025Plus)] string context)
		{
			var vectors = VectorTable.Data;

			await using var db = GetDataContext(context);
			await using var tb = db.CreateLocalTable(vectors);

			var q =
				from t in tb
				orderby t.Vector.CosineVectorDistance(vectors[0].Vector)
				select new
				{
					t,
					Norm11   = t.Vector.VectorNormalize(SqlFn.NormType.Norm1),
					Norm12   = t.Vector.VectorNormalize1(),
					Norm21   = t.Vector.VectorNormalize(SqlFn.NormType.Norm2),
					Norm22   = t.Vector.VectorNormalize2(),
					NormInf1 = t.Vector.VectorNormalize(SqlFn.NormType.NormInf),
					NormInf2 = t.Vector.VectorNormalizeInf(),
				};

			var list = await q.ToListAsync();
			var v    = list[0];

			using (Assert.EnterMultipleScope())
			{
				Assert.That(v.Norm11,   Is.EquivalentTo(v.Norm12));
				Assert.That(v.Norm21,   Is.EquivalentTo(v.Norm22));
				Assert.That(v.NormInf1, Is.EquivalentTo(v.NormInf2));
			}
		}

		[Test]
		public async ValueTask VectorPropertySqlTest([IncludeDataSources(true, TestProvName.AllSqlServer2025PlusMS)] string context)
		{
			// TODO: instead of passing schema explicitly in future we need to implement out-of-box support for custom serialization
			await using var db = GetDataContext(context, SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema);

			var vector = new SqlVector<float>(new[] { 1.0f, 2.0f, 3.0f }.AsMemory());

			var result = db.Select(() => new
			{
				Dimensions1 = vector.VectorProperty(SqlFn.VectorPropertyType.Dimensions),
				Dimensions2 = vector.VectorDimensionsProperty(),
				BaseType1   = vector.VectorProperty(SqlFn.VectorPropertyType.BaseType),
				BaseType2   = vector.VectorBaseTypeProperty()
			});

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Dimensions1, Is.EqualTo(result.Dimensions2.ToString()));
				Assert.That(result.BaseType1,   Is.EqualTo(result.BaseType2));
			}
		}

		[Test]
		public async ValueTask VectorPropertyFloatTest([IncludeDataSources(true, TestProvName.AllSqlServer2025Plus)] string context)
		{
			await using var db = GetDataContext(context);

			var vector = new[] { 1.0f, 2.0f, 3.0f };

			var result = db.Select(() => new
			{
				Dimensions1 = vector.VectorProperty(SqlFn.VectorPropertyType.Dimensions),
				Dimensions2 = vector.VectorDimensionsProperty(),
				BaseType1   = vector.VectorProperty(SqlFn.VectorPropertyType.BaseType),
				BaseType2   = vector.VectorBaseTypeProperty()
			});

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Dimensions1, Is.EqualTo(result.Dimensions2.ToString()));
				Assert.That(result.BaseType1,   Is.EqualTo(result.BaseType2));
			}
		}

		[Test]
		public async ValueTask VectorPrintParameterSqlTest([IncludeDataSources(true, TestProvName.AllSqlServer2025PlusMS)] string context)
		{
			var maxBinaryLogging = LinqToDB.Common.Configuration.MaxBinaryParameterLengthLogging;

			try
			{
				LinqToDB.Common.Configuration.MaxBinaryParameterLengthLogging = 2;

				// TODO: instead of passing schema explicitly in future we need to implement out-of-box support for custom serialization
				await using var db = GetDataContext(context, SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema);

				var vector = new SqlVector<float>(new[] { 1.0f, 2.0f, 3.0f }.AsMemory());

				_ = db.Select(() => vector.VectorDimensionsProperty());

				Assert.That(LastQuery, Contains.Substring("-- value above truncated for logging"));
			}
			finally
			{
				LinqToDB.Common.Configuration.MaxBinaryParameterLengthLogging = maxBinaryLogging;
			}
		}

		[Test]
		public async ValueTask VectorPrintParameterFloatTest([IncludeDataSources(true, TestProvName.AllSqlServer2025PlusMS)] string context)
		{
			var maxBinaryLogging = LinqToDB.Common.Configuration.MaxBinaryParameterLengthLogging;

			try
			{
				LinqToDB.Common.Configuration.MaxBinaryParameterLengthLogging = 2;

				await using var db = GetDataContext(context);

				var vector = new[] { 1.0f, 2.0f, 3.0f };

				_ = db.Select(() => vector.VectorDimensionsProperty());

				Assert.That(LastQuery, Contains.Substring("-- value above truncated for logging"));
			}
			finally
			{
				LinqToDB.Common.Configuration.MaxBinaryParameterLengthLogging = maxBinaryLogging;
			}
		}
	}
}
