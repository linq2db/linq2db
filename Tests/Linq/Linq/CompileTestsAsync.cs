using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
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
			using (var db = GetDataContext(context, new MappingSchema()))
			using (var lt = db.CreateLocalTable(GenerateData()))
			{
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
			using (var db = GetDataContext(context, new MappingSchema()))
			using (var lt = db.CreateLocalTable(GenerateData()))
			{
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
			using (var db = GetDataContext(context, new MappingSchema()))
			using (var lt = db.CreateLocalTable(GenerateData()))
			{
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
		}

		[Test]
		public async Task MinSelectorAsync([DataSources] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema()))
			using (var lt = db.CreateLocalTable(GenerateData()))
			{
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

		[ActiveIssue]
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
