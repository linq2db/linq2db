using System;
using System.Linq;

using NUnit.Framework;

namespace Data.Linq
{
	[TestFixture]
	public class ConvertExpression : TestBase
	{
		[Test]
		public void Select1()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				let children = p.Children.Where(c => c.ParentID > 1)
				select children.Sum(c => c.ChildID),
				from p in db.Parent
				let children = p.Children.Where(c => c.ParentID > 1)
				select children.Sum(c => c.ChildID)));
		}

		[Test]
		public void Select2()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				let children1 = p.Children.Where(c => c.ParentID > 1)
				let children2 = children1.Where(c => c.ParentID < 10)
				select children2.Sum(c => c.ChildID),
				from p in db.Parent
				let children1 = p.Children.Where(c => c.ParentID > 1)
				let children2 = children1.Where(c => c.ParentID < 10)
				select children2.Sum(c => c.ChildID)));
		}

		[Test]
		public void Select3()
		{
			ForEachProvider(db => AreEqual(
				Parent
					.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
					.Select(t => new { children2 = t.children1.Where(c => c.ParentID < 10) })
					.Select(t => t.children2.Sum(c => c.ChildID)),
				db.Parent
					.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
					.Select(t => new { children2 = t.children1.Where(c => c.ParentID < 10) })
					.Select(t => t.children2.Sum(c => c.ChildID))));
		}

		[Test]
		public void Select4()
		{
			ForEachProvider(db => AreEqual(
				Parent
					.Select(p => p.Children. Where(c => c.ParentID > 1))
					.Select(t => t.Where(c => c.ParentID < 10))
					.Select(t => t.Sum(c => c.ChildID)),
				db.Parent
					.Select(p => p.Children. Where(c => c.ParentID > 1))
					.Select(t => t.Where(c => c.ParentID < 10))
					.Select(t => t.Sum(c => c.ChildID))));
		}

		[Test]
		public void Where1()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				let children1 = p.Children.Where(c => c.ParentID > 1)
				let children2 = children1.Where(c => c.ParentID < 10)
				where children1.Any()
				select children2.Sum(c => c.ChildID),
				from p in db.Parent
				let children1 = p.Children.Where(c => c.ParentID > 1)
				let children2 = children1.Where(c => c.ParentID < 10)
				where children1.Any()
				select children2.Sum(c => c.ChildID)));
		}

		[Test]
		public void Where2()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				let children1 = p.Children.Where(c => c.ParentID > 1)
				where children1.Any()
				let children2 = children1.Where(c => c.ParentID < 10)
				select children2.Sum(c => c.ChildID),
				from p in db.Parent
				let children1 = p.Children.Where(c => c.ParentID > 1)
				where children1.Any()
				let children2 = children1.Where(c => c.ParentID < 10)
				select children2.Sum(c => c.ChildID)));
		}

		[Test]
		public void Where3()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				let children1 = p.Children.Where(c => c.ParentID > 1)
				let children2 = children1.Where(c => c.ParentID < 10)
				where children2.Any()
				select children2.Sum(c => c.ChildID),
				from p in db.Parent
				let children1 = p.Children.Where(c => c.ParentID > 1)
				let children2 = children1.Where(c => c.ParentID < 10)
				where children2.Any()
				select children2.Sum(c => c.ChildID)));
		}

		//[Test]
		public void Where4()
		{
			ForEachProvider(db => AreEqual(
				   Parent
					.Select(p => new { p, children1 = p.Children. Where(c => c.ParentID > 1)  })
					.Where (t => t.children1.Any()),
				db.Parent
					.Select(p => new { p, children1 = p.Children. Where(c => c.ParentID > 1)  })
					.Where (t => t.children1.Any())));
		}

		//[Test]
		public void Where5()
		{
			ForEachProvider(db => AreEqual(
				   Parent
					.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
					.Where (t => t.children1.Any()),
				db.Parent
					.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
					.Where (t => t.children1.Any())));
		}

		//[Test]
		public void Where6()
		{
			ForEachProvider(db => AreEqual(
				   Parent
					.Select(p => p.Children. Where(c => c.ParentID > 1))
					.Where (t => t.Any()),
				db.Parent
					.Select(p => p.Children. Where(c => c.ParentID > 1))
					.Where (t => t.Any())));
		}

		[Test]
		public void Any1()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Parent
					.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
					.Any(p => p.children1.Any()),
				db.Parent
					.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
					.Any(p => p.children1.Any())));
		}

		[Test]
		public void Any2()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Parent
					.Select(p => p.Children.Where(c => c.ParentID > 1))
					.Any(p => p.Any()),
				db.Parent
					.Select(p => p.Children.Where(c => c.ParentID > 1))
					.Any(p => p.Any())));
		}

		[Test]
		public void Any3()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Parent
					.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
					.Where(p => p.children1.Any())
					.Any(),
				db.Parent
					.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
					.Where(p => p.children1.Any())
					.Any()));
		}

		//[Test]
		public void Any4()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Parent
					.Select(p => new { children1 = p.Children.Where(c => c.ParentID > 1) })
					.Where(p => p.children1.Any())
					.Any(),
				db.Parent
					.Select(p => new { children1 = p.Children.Where(c => c.ParentID > 1) })
					.Where(p => p.children1.Any())
					.Any()));
		}
	}
}
