using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Tests.Playground
{
	[TestFixture]
	public class WeakJoinTests : TestBase
	{
		[Table]
		class SampleTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Table]
		class JointedTable
		{
			[Column] public int Id       { get; set; }
			[Column] public int Value    { get; set; }
			[Column] public int ParentId { get; set; }
		}

		static Tuple<SampleTable[], JointedTable[]> GenerateTestData()
		{
			var items1 = Enumerable.Range(0, 10).Select((e, i) => new SampleTable
			{
				Id = i,
				Value = i * 100
			}).ToArray();
				
			var items2 = Enumerable.Range(0, 10).Select((e, i) => new JointedTable
			{
				Id = i,
				Value = i * 1000,
				ParentId = i
			}).ToArray();

			return Tuple.Create(items1, items2);
		}

		[Test]
		public void WeakInnerJoinTests([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (items, joinedItems) = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(items))
			using (var forJoin = db.CreateLocalTable(joinedItems))
			{
				var query =
					from t in table
					from j1 in forJoin.WeakInnerJoin(j1 => t.Id == j1.ParentId)
					from j2 in forJoin.WeakInnerJoin(j2 => t.Id == j1.Id)
					select new
					{
						T = t,
						J1 = j1,
						J2 = j2
					};

				//var projection1 =
				//	from q in query
				//	select new
				//	{
				//		q.T,
				//		q.J2.Value
				//	};

				//var result1 = projection1.ToArray();

				//var projection2 =
				//	from q in query
				//	select new
				//	{
				//		q.T,
				//		q.J1.Value
				//	};

				//var result2 = projection2.ToArray();

				var projection3 =
					from q in query
					select new
					{
						q.T,
						q.T.Value
					};

				var result3 = projection3.ToArray();
			}
		}
	}
}
