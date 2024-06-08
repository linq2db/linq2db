#if NETFRAMEWORK1

using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	public class BatchTests: TestBase
	{
		[Test]
		public void Test([IncludeDataSources(ProviderName.SqlServer)] string context)
		{
			using (var db = (TestServiceModelDataContext)GetDataContext(context + LinqServiceSuffix))
			{
				db.BeginBatch();

				var tbl = db.CreateTableTemporarily("temp",
					from p in db.Parent
					where p.ParentID > 100
					select new
					{
						ID    = p.ParentID,
						Value = p.Children.Count == 1 ? Math.Sqrt(p.Value1!.Value) : Math.Exp(p.Value1!.Value)
					},
					tableOptions : TableOptions.IsTemporary | TableOptions.CreateIfNotExists);

				db.Parent.Insert(() => new Parent { ParentID = 778 });
				db.Parent.Delete(t => t.ParentID == 778);

				(
					from p in db.Parent
					join t in tbl on p.ParentID equals t.ID
					where p.ParentID > 100
					select new { p, t }
				)
				.Update(p => p.p, p => new Parent { Value1 = (int)p.t.Value });

				tbl.Drop();

				db.CommitBatch();
			}
		}
	}
}

#endif
