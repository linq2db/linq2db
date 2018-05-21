using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ConvertExpressionTests : TestBase
	{
		[Test, DataContextSource]
		public void Select1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children = p.Children.Where(c => c.ParentID > 1)
					select children.Sum(c => c.ChildID),
					from p in db.Parent
					let children = p.Children.Where(c => c.ParentID > 1)
					select children.Sum(c => c.ChildID));
		}

		[Test, DataContextSource]
		public void Select2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					select children2.Sum(c => c.ChildID),
					from p in db.Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					select children2.Sum(c => c.ChildID));
		}

		[Test, DataContextSource]
		public void Select3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Parent
						.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
						.Select(t => new { children2 = t.children1.Where(c => c.ParentID < 10) })
						.Select(t => t.children2.Sum(c => c.ChildID)),
					db.Parent
						.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
						.Select(t => new { children2 = t.children1.Where(c => c.ParentID < 10) })
						.Select(t => t.children2.Sum(c => c.ChildID)));
		}

		[Test, DataContextSource]
		public void Select4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Parent
						.Select(p => p.Children. Where(c => c.ParentID > 1))
						.Select(t => t.Where(c => c.ParentID < 10))
						.Select(t => t.Sum(c => c.ChildID)),
					db.Parent
						.Select(p => p.Children. Where(c => c.ParentID > 1))
						.Select(t => t.Where(c => c.ParentID < 10))
						.Select(t => t.Sum(c => c.ChildID)));
		}

		[Test, DataContextSource]
		public void Where1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					where children1.Any()
					select children2.Sum(c => c.ChildID),
					from p in db.Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					where children1.Any()
					select children2.Sum(c => c.ChildID));
		}

		[Test, DataContextSource]
		public void Where2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					where children1.Any()
					let children2 = children1.Where(c => c.ParentID < 10)
					select children2.Sum(c => c.ChildID),
					from p in db.Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					where children1.Any()
					let children2 = children1.Where(c => c.ParentID < 10)
					select children2.Sum(c => c.ChildID));
		}

		[Test, DataContextSource]
		public void Where3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					where children2.Any()
					select children2.Sum(c => c.ChildID),
					from p in db.Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					where children2.Any()
					select children2.Sum(c => c.ChildID));
		}

		//[Test, DataContextSource]
		public void Where4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent
						.Select(p => new { p, children1 = p.Children. Where(c => c.ParentID > 1)  })
						.Where (t => t.children1.Any()),
					db.Parent
						.Select(p => new { p, children1 = p.Children. Where(c => c.ParentID > 1)  })
						.Where (t => t.children1.Any()));
		}

		//[Test, DataContextSource]
		public void Where5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent
						.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
						.Where (t => t.children1.Any()),
					db.Parent
						.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
						.Where (t => t.children1.Any()));
		}

		//[Test, DataContextSource]
		public void Where6(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent
						.Select(p => p.Children. Where(c => c.ParentID > 1))
						.Where (t => t.Any()),
					db.Parent
						.Select(p => p.Children. Where(c => c.ParentID > 1))
						.Where (t => t.Any()));
		}

		[Test, DataContextSource]
		public void Any1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent
						.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
						.Any(p => p.children1.Any()),
					db.Parent
						.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
						.Any(p => p.children1.Any()));
		}

		[Test, DataContextSource]
		public void Any2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent
						.Select(p => p.Children.Where(c => c.ParentID > 1))
						.Any(p => p.Any()),
					db.Parent
						.Select(p => p.Children.Where(c => c.ParentID > 1))
						.Any(p => p.Any()));
		}

		[Test, DataContextSource]
		public void Any3(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent
						.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
						.Where(p => p.children1.Any())
						.Any(),
					db.Parent
						.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
						.Where(p => p.children1.Any())
						.Any());
		}

		//[Test, DataContextSource]
		public void Any4(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent
						.Select(p => new { children1 = p.Children.Where(c => c.ParentID > 1) })
						.Where(p => p.children1.Any())
						.Any(),
					db.Parent
						.Select(p => new { children1 = p.Children.Where(c => c.ParentID > 1) })
						.Where(p => p.children1.Any())
						.Any());
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.Informix, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana)]
		public void LetTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select ch.FirstOrDefault().ParentID
					,
					from p in db.Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select ch.FirstOrDefault().ParentID);
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.Informix, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SybaseManaged, ProviderName.SapHana)]
		public void LetTest2(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select p
					,
					from p in db.Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select p);
			}
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana)]
		public void LetTest3(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = Child
					select ch.FirstOrDefault().ParentID
					,
					from p in db.Parent
					let ch = db.Child
					select ch.FirstOrDefault().ParentID);
			}
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.Sybase, ProviderName.SapHana)]
		public void LetTest4(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					});
			}
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana)]
		public void LetTest5(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					});
			}
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana)]
		public void LetTest6(string context)
		{
			//LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0));
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana)]
		public void LetTest7(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0).Take(5000)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0).Take(5000));
		}

		[Test, DataContextSource]
		public void LetTest8(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					let ch3	= ch2.FirstOrDefault(c => c.ParentID > 0)
					select new
					{
						First1 = ch3 == null ? 0 : ch3.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					let ch3	= ch2.FirstOrDefault(c => c.ParentID > 0)
					select new
					{
						First1 = ch3 == null ? 0 : ch3.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First2 = ch2.FirstOrDefault()
					});
		}

		[Test, DataContextSource]
		public void LetTest9(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Take(10)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Take(10));
		}

		[Test, DataContextSource]
		public void LetTest10(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Any()
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Any());
		}

		[Test, DataContextSource]
		public void LetTest11(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let ch1 = Child.FirstOrDefault(c => c.ParentID > 0)
					let ch2 = Child.Where(c => c.ChildID > -100)
					select new
					{
						First1 = ch1 == null ? 0 : ch1.ParentID,
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.OrderBy(c => c.ParentID).FirstOrDefault(c => c.ParentID > 0)
					let ch2 = db.Child.Where(c => c.ChildID > -100)
					select new
					{
						First1 = ch1 == null ? 0 : ch1.ParentID,
						First2 = ch2.FirstOrDefault()
					});
		}
	}
}
