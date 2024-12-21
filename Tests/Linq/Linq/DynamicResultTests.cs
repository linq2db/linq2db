using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class DynamicResultTests : TestBase
	{
		[Table]
		sealed class RawDynamicData
		{
			[Column] public int AId { get; set; }
			[Column] public int AValue { get; set; }

			[Column] public int BId { get; set; }
			[Column] public int BValue { get; set; }

			public static RawDynamicData[] Seed()
			{
				return Enumerable.Range(1, 20)
					.Select(i => new RawDynamicData { AId = i, BId = i * 100, AValue = i * 2, BValue = i * 100 * 2 })
					.ToArray();
			}
		}

		[Test]
		public void DynamicQueryViaDynamic([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServerSequentialAccess)] string context, [Values(1, 10)] int param)
		{
			var data = RawDynamicData.Seed();

			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable(data))
			{
				var result = db.Query<dynamic>("select * from RawDynamicData where AId >= @param", new {param = param}).ToList();

				var casted = result.Select(x =>
						new RawDynamicData
						{
							AId = (int)x.AId,
							AValue = (int)x.AValue,
							BId = (int)x.BId,
							BValue = (int)x.BValue
						})
					.ToArray();

				AreEqualWithComparer(data.Where(x => x.AId >= param), casted);
			}
		}

		[Test]
		public void DynamicQueryViaObject([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServerSequentialAccess)] string context, [Values(1, 10)] int param)
		{
			var data = RawDynamicData.Seed();

			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable(data))
			{
				var result = db.Query<object>("select * from RawDynamicData where AId >= @param", new {param = param}).ToList();

				var casted = result.OfType<dynamic>().Select(x =>
						new RawDynamicData
						{
							AId = (int)x.AId,
							AValue = (int)x.AValue,
							BId = (int)x.BId,
							BValue = (int)x.BValue
						})
					.ToArray();

				AreEqualWithComparer(data.Where(x => x.AId >= param), casted);
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3520")]
		public void Issue3520Test([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServerSequentialAccess)] string context, [Values(1, 10)] int param)
		{
			var data = RawDynamicData.Seed();

			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable(data))
			{
				var result = db.Query<MyObject>("select * from RawDynamicData where AId >= @param", new {param = param}).ToList();

				var casted = result.Cast<dynamic>().Select(x =>
						new RawDynamicData
						{
							AId = (int)x.AId,
							AValue = (int)x.AValue,
							BId = (int)x.BId,
							BValue = (int)x.BValue
						})
					.ToArray();

				AreEqualWithComparer(data.Where(x => x.AId >= param), casted);
			}
		}

		class MyObject : DynamicObject
		{
			private Dictionary<string, object?> _data = new();
			public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
			{
				return _data.TryGetValue(binder.Name, out result);
			}
			public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
			{
				return base.TryInvoke(binder, args, out result);
			}
			public override bool TrySetMember(SetMemberBinder binder, object? value)
			{
#if NETFRAMEWORK
				if (_data.ContainsKey(binder.Name))
					return false;
				_data.Add(binder.Name, value);
				return true;
#else
				return _data.TryAdd(binder.Name, value);
#endif
			}
			public override bool TryGetMember(GetMemberBinder binder, out object? result)
			{
				return _data.TryGetValue(binder.Name, out result);
			}
		}
	}
}
