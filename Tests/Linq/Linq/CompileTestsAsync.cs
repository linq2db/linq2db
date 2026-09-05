using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools.EntityServices;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class CompileTestsAsync : TestBase
	{
		sealed class AsyncDataTable
		{
			[PrimaryKey]
			public int Id { get; set; }
		}

		sealed class AsyncDataProjection
		{
			public int Id { get; set; }
			public int Value { get; set; }

			private bool Equals(AsyncDataProjection other)
			{
				return Id == other.Id && Value == other.Value;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((AsyncDataProjection)obj);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Id, Value);
			}
		}

		static IEnumerable<AsyncDataTable> GenerateData()
		{
			return Enumerable.Range(1, 10).Select(i => new AsyncDataTable { Id = i });
		}

		[Test]
		public async Task FirstAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection>>((db, id, token) =>
				(from c in db.GetTable<AsyncDataTable>()
				where c.Id == id
				select new AsyncDataProjection
				{
					Id = id,
					Value = c.Id
				}).FirstAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.Id, Is.EqualTo(2));
					Assert.That(result.Value, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task FirstPredicateAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection>>((db, id, token) =>
				(from c in db.GetTable<AsyncDataTable>()
				where c.Id == id
				select new AsyncDataProjection
				{
					Id = id,
					Value = c.Id
				}).FirstAsync(c => c.Id == id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.Id, Is.EqualTo(2));
					Assert.That(result.Value, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task FirstOrDefaultAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection?>>((db, id, token) =>
				(from c in db.GetTable<AsyncDataTable>()
				where c.Id == id
				select new AsyncDataProjection
				{
					Id = id,
					Value = c.Id
				}).FirstOrDefaultAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = (await query(db, 2, CancellationToken.None))!;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.Id, Is.EqualTo(2));
					Assert.That(result.Value, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task FirstOrDefaultPredicateAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection?>>((db, id, token) =>
				(from c in db.GetTable<AsyncDataTable>()
				where c.Id == id
				select new AsyncDataProjection
				{
					Id = id,
					Value = c.Id
				}).FirstOrDefaultAsync(c => c.Id == id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = (await query(db, 2, CancellationToken.None))!;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.Id, Is.EqualTo(2));
					Assert.That(result.Value, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task SingleAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection>>((db, id, token) =>
				(from c in db.GetTable<AsyncDataTable>()
				where c.Id == id
				select new AsyncDataProjection
				{
					Id = id,
					Value = c.Id
				}).SingleAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.Id, Is.EqualTo(2));
					Assert.That(result.Value, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task SinglePredicateAsync([DataSources] string context)
		{
			using var db = GetDataContext(context, new MappingSchema());
			using var lt = db.CreateLocalTable(GenerateData());
			new FluentMappingBuilder(db.MappingSchema)
				.Entity<AsyncDataTable>()
					.HasTableName(lt.TableName)
				.Build();

			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection>>(
				(bd, id, token) =>
					(
						from c in bd.GetTable<AsyncDataTable>()
						where c.Id == id
						select new AsyncDataProjection
						{
							Id = id,
							Value = c.Id
						}
					).SingleAsync(c => c.Id == id, token));

			var result = await query(db, 2, CancellationToken.None);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Id, Is.EqualTo(2));
				Assert.That(result.Value, Is.EqualTo(2));
			}
		}

		[Test]
		public async Task SingleOrDefaultAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection?>>((db, id, token) =>
				(from c in db.GetTable<AsyncDataTable>()
				where c.Id == id
				select new AsyncDataProjection
				{
					Id = id,
					Value = c.Id
				}).SingleOrDefaultAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = (await query(db, 2, CancellationToken.None))!;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.Id, Is.EqualTo(2));
					Assert.That(result.Value, Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async Task SingleOrDefaultPredicateAsync([DataSources] string context)
		{
			using var db = GetDataContext(context, new MappingSchema());
			using var lt = db.CreateLocalTable(GenerateData());
			new FluentMappingBuilder(db.MappingSchema)
				.Entity<AsyncDataTable>()
					.HasTableName(lt.TableName)
				.Build();

			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection?>>(
					(bd, id, token) =>
					(
						from c in bd.GetTable<AsyncDataTable>()
						where c.Id == id
						select new AsyncDataProjection
						{
							Id = id,
							Value = c.Id
						}
					).SingleOrDefaultAsync(c => c.Id == id, token));

			var result = (await query(db, 2, CancellationToken.None))!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Id, Is.EqualTo(2));
				Assert.That(result.Value, Is.EqualTo(2));
			}
		}

		[Test]
		public async Task AnyAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<bool>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id == id).AnyAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.True);
			}
		}

		[Test]
		public async Task AnyPredicateAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<bool>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().AnyAsync(c => c.Id == id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.True);
			}
		}

		[Test]
		public async Task CountAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id == id).CountAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public async Task CountPredicateAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().CountAsync(c => c.Id == id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public async Task LongCountAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id == id).LongCountAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public async Task LongCountPredicateAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().LongCountAsync(c => c.Id == id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public async Task MinAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, new MappingSchema());
			using var lt = db.CreateLocalTable(GenerateData());
			new FluentMappingBuilder(db.MappingSchema)
				.Entity<AsyncDataTable>()
					.HasTableName(lt.TableName)
				.Build();

			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>(
					(bd, id, token) =>
						bd.GetTable<AsyncDataTable>().Where(c => c.Id > id).Select(c => c.Id).MinAsync(token));

			var result = await query(db, 2, CancellationToken.None);
			Assert.That(result, Is.EqualTo(3));
		}

		[Test]
		public async Task MinSelectorAsync([DataSources] string context)
		{
			using var db = GetDataContext(context, new MappingSchema());
			using var lt = db.CreateLocalTable(GenerateData());
			new FluentMappingBuilder(db.MappingSchema)
				.Entity<AsyncDataTable>()
					.HasTableName(lt.TableName)
				.Build();

			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>(
					(bd, id, token) =>
						bd.GetTable<AsyncDataTable>().Where(c => c.Id > id).MinAsync(c => c.Id, token));

			var result = await query(db, 2, CancellationToken.None);
			Assert.That(result, Is.EqualTo(3));
		}

		[Test]
		public async Task MaxAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id > id).Select(c => c.Id).MaxAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task MaxSelectorAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id > id).MaxAsync(c => c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task AllAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<bool>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().AllAsync(c => c.Id == id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public async Task ContainsAsync([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<bool>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Select(c => c.Id).ContainsAsync(id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.That(result, Is.True);
			}
		}

		#region SumAsync

		[Test]
		public async Task SumAsyncInt([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (int)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncIntN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (int?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncLong([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (long)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncLongN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (long?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncFloat([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<float>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (float)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncFloatN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<float?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (float?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncDouble([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (double)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncDoubleN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (double?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncDecimal([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<decimal>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (decimal)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncDecimalN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<decimal?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (decimal?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}
		#endregion

		#region SumAsyncSelector

		[Test]
		public async Task SumAsyncSelectorInt([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (int)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorIntN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (int?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorLong([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (long)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorLongN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (long?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorFloat([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<float>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (float)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorFloatN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<float?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (float?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorDouble([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (double)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorDoubleN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (double?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorDecimal([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<decimal>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (decimal)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		[Test]
		public async Task SumAsyncSelectorDecimalN([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<decimal?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (decimal?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(10));
			}
		}

		#endregion

		#region Average

		[Test]
		public async Task AverageAsyncLong([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (long)c.Id).AverageAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.That(result, Is.EqualTo(2.5d));
			}
		}

		#endregion

		[Test]
		public async Task IDataContext_CompiledQueryTest([DataSources(false)] string context)
		{
			await using var db  = new TestDataConnection(context);
			using       var map = new IdentityMap(db);

			var query = CompiledQuery.Compile<TestDataConnection,CancellationToken,Task<List<Person>>>(static (db, ct) => db.Person.Where(p => p.ID == 1).ToListAsync(ct));

			var result1 = await query(db, default);
			var result2 = await query(db, default);

			Assert.That(result2[0], Is.SameAs(result1[0]));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4365")]
		public async Task CustomContext_CompiledQueryCustomTest([DataSources(false)] string context)
		{
			await using var db  = new TestDataCustomConnection(context);
			using       var map = new IdentityMap(db);

			var query = CompiledQuery.Compile<TestDataCustomConnection,CancellationToken,Task<List<Person>>>(static (db, ct) => db.Person.Where(p => p.ID == 1).ToListAsync(ct));

			var result1 = await query(db, default);
			var result2 = await query(db, default);

			Assert.That(result2[0], Is.SameAs(result1[0]));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5844#issuecomment-5538235961")]
		public async Task ClosureValueSurvivesRebalancedPredicateAsyncTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// GetForEachAsync takes the same cached-Info shortcut as the synchronous enumeration, so the
			// async path needs its own guard that the table was handed the exposed tree.
			var id    = 2;
			var query = CompiledQuery.Compile<ITestDataContext,IQueryable<Parent>>(d =>
				d.Parent.Where(p => p.ParentID == id && p.ParentID > 0 && p.ParentID < 1000 && p.ParentID != -1));

			var parents = await query(db).ToListAsync();

			Assert.That(parents.Select(p => p.ParentID).ToList(), Is.EqualTo(new[] { 2 }));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		// Sybase excluded by https://github.com/linq2db/linq2db/issues/5865 - the element-form eager-load
		// preamble joins a derived table carrying TOP, which SybaseDataProvider already declares invalid
		// through IsJoinDerivedTableWithTakeInvalid, but the preamble reaches the provider without that
		// check running, so only the first detail row comes back.
		public async Task ElementFormLoadWithTest([DataSources(TestProvName.AllSybase)] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,CancellationToken,Task<Parent>>(static (db, id, token) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.FirstAsync(token));

			using var db = GetDataContext(context);

			var parent = await query(db, 1, default);
			var other  = await query(db, 2, default);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(parent.ParentID, Is.EqualTo(1));
				Assert.That(parent.Children, Has.Count.EqualTo(1));
				Assert.That(other.ParentID,  Is.EqualTo(2));
				Assert.That(other.Children,  Has.Count.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		public async Task ElementFormLoadWithOpensLoadTransaction([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var queryable = CompiledQuery.Compile<ITestDataContext,int,CancellationToken,Task<List<Parent>>>(static (db, id, token) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.ToListAsync(token));

			var element = CompiledQuery.Compile<ITestDataContext,int,CancellationToken,Task<Parent>>(static (db, id, token) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.FirstAsync(token));

			var transactions = 0;

			using var db = GetDataContext(context, o => o.UseTracing(e =>
			{
				if (e.TraceInfoStep == TraceInfoStep.BeforeExecute && e.Operation == TraceOperation.BeginTransaction)
					transactions++;
			}));

			var viaQueryable   = (await queryable(db, 1, default))[0];
			var afterQueryable = transactions;
			var viaElement     = await element(db, 1, default);
			var afterElement   = transactions;
			var viaElement2    = await element(db, 2, default);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(viaQueryable.Children, Has.Count.EqualTo(1));
				Assert.That(viaElement.Children,   Has.Count.EqualTo(1));
				Assert.That(viaElement2.Children,  Has.Count.EqualTo(2));

				Assert.That(afterQueryable, Is.EqualTo(1), "queryable form must open the implicit eager-loading transaction");
				Assert.That(afterElement,   Is.EqualTo(2), "element form must open one as well");
				Assert.That(transactions,   Is.EqualTo(3), "and must dispose it - a leak leaves the connection inside it, so the next call opens none");
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5842")]
		public void ElementFormLoadWithHonorsCancellation([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var eager = CompiledQuery.Compile<ITestDataContext,int,CancellationToken,Task<Parent>>(static (db, id, token) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.LoadWith(p => p.Children)
					.FirstAsync(token));

			// No LoadWith means no preamble and so no implicit transaction, which makes GetElementAsync the
			// first await able to observe the token - the site that used to receive default.
			var plain = CompiledQuery.Compile<ITestDataContext,int,CancellationToken,Task<Parent>>(static (db, id, token) =>
				db.Parent
					.Where(p => p.ParentID == id)
					.FirstAsync(token));

			using var cts = new CancellationTokenSource();
			cts.Cancel();

			using var db = GetDataContext(context);

			Assert.ThrowsAsync<OperationCanceledException>(async () =>
			{
				try
				{
					await eager(db, 1, cts.Token);
				}
				catch (OperationCanceledException)
				{
					// normalizes TaskCanceledException, which the assert above would not match
					throw new OperationCanceledException();
				}
			});

			Assert.ThrowsAsync<OperationCanceledException>(async () =>
			{
				try
				{
					await plain(db, 1, cts.Token);
				}
				catch (OperationCanceledException)
				{
					// normalizes TaskCanceledException, which the assert above would not match
					throw new OperationCanceledException();
				}
			});
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3266")]
		public async Task Issue3266Test([DataSources(false)] string context)
		{
			var query = CompiledQuery.Compile(
				(ITestDataContext db, int id) =>  db.Person
					.Where(p => p.ID == id)
					.Set(p => p.LastName, "updated")
					.UpdateAsync(default));

			using var db  = GetDataContext(context);

			await query(db, -1);
		}
	}
}
