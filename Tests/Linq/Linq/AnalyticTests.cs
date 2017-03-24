namespace Tests.Linq
{
	using System.Linq;

	using LinqToDB;

	using NUnit.Framework;

	[TestFixture]
	public class AnalyticTests : TestBase
	{
		public class Entity
		{
			public int IntField;
			public string StringField;
		}

		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from e in db.Parent
					select new
					{
						Rank = Sql.Over(e).PartitionBy(v => v.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Count(v => v.ParentID, Sql.AggregateModifier.Distinct)
					};
				var str = q.ToString();
			}
		}

//				var q = from e in selectQuery
//					select new
//					{
//						e.IntField,
//						e.StringField,
//						c0 = Sql.Over(e).Rank(),
////					c1 = Sql.Over(e).Aggregate(ee => ee.Max(v => v.IntField)),
//						v1 = Sql.Over(e).PartitionBy(v => v.IntField).OrderBy(v => v.StringField).ThenByDesc(v => v.IntField).Rank(),
//						v2 = Sql.Over(e).PartitionBy(v => v.IntField).OrderBy(v => v.StringField).RowNumber(),
//						v3 = Sql.Over(e).PartitionBy(v => v.IntField).OrderBy(v => v.StringField).NTile(v => 4),
//						v4 = Sql.Over(e).PartitionBy(v => v.IntField).OrderBy(v => v.StringField).Range.UnboundedPreceding.Rank(),
//						v5 = Sql.Over(e).PartitionBy(v => v.IntField).OrderBy(v => v.StringField).Range.Between.UnboundedPreceding.And.UnboundedFollowing.Rank(),
//						v6 = Sql.Over(e).PartitionBy(v => v.IntField).OrderBy(v => v.StringField).Range.Between.Value(v=> v.StringField).And.UnboundedFollowing.Rank(),
//						v7 = Sql.Over(e).PartitionBy(v => v.IntField).OrderBy(v => v.StringField).Range.Between.Value(v=> v.StringField).And.UnboundedFollowing.DenseRank(v => 2),
//						v8 = Sql.Over(e).PartitionBy(v => v.IntField).OrderBy(v => v.StringField).Range.Between.Value(v=> v.StringField).And.UnboundedFollowing.Count(v => v.IntField)
//					};
		}		
}