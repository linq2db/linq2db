﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CompileTestsAsync : TestBase
	{
		class AsyncDataTable
		{
			[PrimaryKey]
			public int Id { get; set; }
		}

		class AsyncDataProjection
		{
			public int Id { get; set; }
			public int Value { get; set; }

			protected bool Equals(AsyncDataProjection other)
			{
				return Id == other.Id && Value == other.Value;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((AsyncDataProjection)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Id * 397) ^ Value;
				}
			}
		}

		static IEnumerable<AsyncDataTable> GenerateData()
		{
			return Enumerable.Range(1, 10).Select(i => new AsyncDataTable { Id = i });
		}

		[Test]
		public async Task FirstAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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
				Assert.AreEqual(2, result.Id);
				Assert.AreEqual(2, result.Value);
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
				Assert.AreEqual(2, result.Id);
				Assert.AreEqual(2, result.Value);
			}
		}

		[Test]
		public async Task FirstOrDefaultAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection>>((db, id, token) =>
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
				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(2, result.Id);
				Assert.AreEqual(2, result.Value);
			}
		}

		[Test]
		public async Task FirstOrDefaultPredicateAsync([DataSources] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection>>((db, id, token) =>
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
				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(2, result.Id);
				Assert.AreEqual(2, result.Value);
			}
		}

		[Test]
		public async Task SingleAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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
				Assert.AreEqual(2, result.Id);
				Assert.AreEqual(2, result.Value);
			}
		}

		[Test]
		public async Task SinglePredicateAsync([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var lt = db.CreateLocalTable(GenerateData()))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<AsyncDataTable>()
						.HasTableName(lt.TableName);

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
				Assert.AreEqual(2, result.Id);
				Assert.AreEqual(2, result.Value);
			}
		}

		[Test]
		public async Task SingleOrDefaultAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<AsyncDataProjection>>((db, id, token) =>
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
				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(2, result.Id);
				Assert.AreEqual(2, result.Value);
			}
		}

		[Test]
		public async Task SingleOrDefaultPredicateAsync([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var lt = db.CreateLocalTable(GenerateData()))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<AsyncDataTable>()
						.HasTableName(lt.TableName);

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
					).SingleOrDefaultAsync(c => c.Id == id, token));

				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(2, result.Id);
				Assert.AreEqual(2, result.Value);
			}
		}

		[Test]
		public async Task AnyAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<bool>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id == id).AnyAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.IsTrue(result);
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
				Assert.IsTrue(result);
			}
		}

		[Test]
		public async Task CountAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id == id).CountAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(1, result);
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
				Assert.AreEqual(1, result);
			}
		}

		[Test]
		public async Task LongCountAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id == id).LongCountAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(1, result);
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
				Assert.AreEqual(1, result);
			}
		}


		[Test]
		public async Task MinAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var lt = db.CreateLocalTable(GenerateData()))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<AsyncDataTable>()
						.HasTableName(lt.TableName);

				var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>(
					(bd, id, token) =>
						bd.GetTable<AsyncDataTable>().Where(c => c.Id > id).Select(c => c.Id).MinAsync(token));

				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(3, result);
			}
		}

		[Test]
		public async Task MinSelectorAsync([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var lt = db.CreateLocalTable(GenerateData()))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<AsyncDataTable>()
						.HasTableName(lt.TableName);

				var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>(
					(bd, id, token) =>
						bd.GetTable<AsyncDataTable>().Where(c => c.Id > id).MinAsync(c => c.Id, token));

				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(3, result);
			}
		}

		[Test]
		public async Task MaxAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id > id).Select(c => c.Id).MaxAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.AreEqual(10, result);
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
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task AllAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<bool>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().AllAsync(c => c.Id == id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.IsFalse(result);
			}
		}

		[Test]
		public async Task ContainsAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<bool>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Select(c => c.Id).ContainsAsync(id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 2, CancellationToken.None);
				Assert.IsTrue(result);
			}
		}

		#region SumAsync

		[Test]
		public async Task SumAsyncInt([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (int)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncIntN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (int?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncLong([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (long)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncLongN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (long?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncFloat([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<float>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (float)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncFloatN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<float?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (float?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncDouble([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (double)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncDoubleN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (double?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncDecimal([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<decimal>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (decimal)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncDecimalN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<decimal?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (decimal?)c.Id).SumAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}
		#endregion

		#region SumAsyncSelector

		[Test]
		public async Task SumAsyncSelectorInt([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (int)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorIntN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<int?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (int?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorLong([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (long)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorLongN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<long?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (long?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorFloat([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<float>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (float)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorFloatN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<float?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (float?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorDouble([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (double)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorDoubleN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (double?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorDecimal([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<decimal>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (decimal)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		[Test]
		public async Task SumAsyncSelectorDecimalN([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<decimal?>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).SumAsync(c => (decimal?)c.Id, token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(10, result);
			}
		}

		#endregion

		#region Average

		[Test]
		public async Task AverageAsyncLong([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var query = CompiledQuery.Compile<IDataContext,int,CancellationToken,Task<double>>((db, id, token) =>
				db.GetTable<AsyncDataTable>().Where(c => c.Id < id).Select(c => (long)c.Id).AverageAsync(token));

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(GenerateData()))
			{
				var result = await query(db, 5, CancellationToken.None);
				Assert.AreEqual(2.5d, result);
			}
		}



		#endregion

	}
}
