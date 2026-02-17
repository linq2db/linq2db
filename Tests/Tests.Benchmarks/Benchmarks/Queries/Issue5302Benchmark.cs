using System;
using System.Data;
using System.Data.Common;
using System.Linq;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Queries
{
	public class Issue5302Benchmark
	{
		private DataConnection _db     = null!;
		private DbConnection   _cn     = null!;

		[GlobalSetup]
		public void Setup()
		{
			_cn = new MockDbConnection(new QueryResult() { Return = 1 }, ConnectionState.Open);
			_db = new DataConnection(new DataOptions().UseConnection(PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v18), _cn));
		}

		[Benchmark]
		public void Select()
		{
			var sandbox = WhFlags.Value;
			var availableClearingWarehouseIds = new int[] {1, 2 };
			var unavailableClearingWarehouseIds = new int[] {3, 4 };
			var defaultCountryCode = 4;
			var typeId1 = new int[] {1, 3 };
			var typeId2 = new int[] {1, 4 };
			var typeId3 = new int[] {1, 5 };
			var typeId4 = new int[] {1, 6 };

			var query = _db.GetTable<Warehouse>()
				.InnerJoin(_db.GetTable<Country>(), (w, c) => w.CountryCode == c.Code, (w, c) => new { w, c })
				.Where(r => r.w.IsActive
					&&  Sql.Ext.PostgreSQL().Contains(
						FlagToFlagidxArray(BitFlipEnum(r.w.Flags)),
						FlagToFlagidxArray(sandbox))
					&& (Sql.Ext.PostgreSQL().ValueIsEqualToAny(r.w.ClearingId, availableClearingWarehouseIds)
						|| (Sql.Ext.PostgreSQL().ValueIsNotEqualToAny(r.w.ClearingId, unavailableClearingWarehouseIds)
							&& r.w.CountryCode == defaultCountryCode))
					&& ((Sql.Ext.PostgreSQL().ValueIsEqualToAny(r.w.WmsTypeId, typeId1)
							&& Sql.Ext.PostgreSQL().ValueIsEqualToAny(r.w.TypeId, typeId2))
						|| (Sql.Ext.PostgreSQL().ValueIsEqualToAny(r.w.WmsTypeId, typeId3)
							&& Sql.Ext.PostgreSQL().ValueIsEqualToAny(r.w.TypeId, typeId4))));

			_ = query.ToSqlQuery();
		}

		[Sql.Expression("~({0})", ServerSideOnly = true)]
		static T BitFlipEnum<T>([ExprParameter] T flags)
			where T : Enum
			=> throw new ServerSideOnlyException(nameof(BitFlipEnum));

		[Sql.Expression("flag_to_flagidx_array({0})", ServerSideOnly = true)]
		static int[] FlagToFlagidxArray<T>([ExprParameter] T flags)
			where T : Enum
			=> throw new ServerSideOnlyException(nameof(FlagToFlagidxArray));

		enum WhFlags
		{
			Value
		}

		[Table]
		sealed class Warehouse
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int CountryCode { get; set; }
			[Column] public int ClearingId { get; set; }
			[Column] public int WmsTypeId { get; set; }
			[Column] public int TypeId { get; set; }
			[Column] public bool IsActive { get; set; }
			[Column] public WhFlags Flags { get; set; }
		}

		[Table]
		sealed class Country
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int Code { get; set; }
		}
	}
}
