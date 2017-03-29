namespace Tests.Linq
{
	using System.Linq;
	using LinqToDB;
	using NUnit.Framework;

	[TestFixture]
	public class AnalyticTests : TestBase
	{
		[Test, DataContextSource(ProviderName.Access, ProviderName.SQLite, ProviderName.SapHana, ProviderName.MySql, ProviderName.SqlCe)]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID 
					select new
					{
						Rank1     = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).Rank(),
						Rank2     = Sql.Over.OrderBy(p.Value1).Rank(),
						DenseRank = Sql.Over.PartitionBy(p.Value1, c.ChildID).OrderBy(p.Value1).DenseRank(),
						Count1    = Sql.Over.PartitionBy(p.Value1).OrderBy(p.Value1).Range.Between.UnboundedPreceding.And.CurrentRow.Count(p.ParentID, Sql.AggregateModifier.All),
						Count2    = Sql.Over.Count(p.Value1, Sql.AggregateModifier.All)
					};
				 Assert.IsNotEmpty(q.ToArray());
			}
		}
	}
}