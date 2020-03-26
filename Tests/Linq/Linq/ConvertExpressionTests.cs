using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ConvertExpressionTests : TestBase
	{
		[Test]
		public void Select1([DataSources] string context)
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

		[Test]
		public void Select2([DataSources] string context)
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

		[Test]
		public void Select3([DataSources] string context)
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

		[Test]
		public void Select4([DataSources] string context)
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

		[Test]
		public void Where1([DataSources] string context)
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

		[Test]
		public void Where2([DataSources] string context)
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

		[Test]
		public void Where3([DataSources] string context)
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

		//[Test]
		public void Where4([DataSources] string context)
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

		//[Test]
		public void Where5([DataSources] string context)
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

		//[Test]
		public void Where6([DataSources] string context)
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

		[Test]
		public void Any1([DataSources] string context)
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

		[Test]
		public void Any2([DataSources] string context)
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

		[Test]
		public void Any3([DataSources] string context)
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

		//[Test]
		public void Any4([DataSources] string context)
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

		[Test]
		public void LetTest1([DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
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

		[Test]
		public void LetTest2([DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
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

		[Test]
		public void LetTest3([DataSources(TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
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

		[Test]
		public void LetTest4([DataSources(TestProvName.AllInformix, TestProvName.AllSapHana)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
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
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					});
			}
		}

		[Test]
		public void LetTest41([DataSources(TestProvName.AllInformix, TestProvName.AllSapHana)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID).OrderBy(c => c.ChildID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					});
			}
		}

		[Test]
		public void LetTest5([DataSources(TestProvName.AllOracle11, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
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
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					});
			}
		}

		[Test]
		public void LetTest6([DataSources(TestProvName.AllOracle11, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
		{
			//LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var expected = (from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						p.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					})
					.Where(t => t.ParentID > 0)
					.ToArray();

				var actual = (
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						p.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					}
				)
					.Where(t => t.ParentID > 0)
					.ToArray();

				// Access has different order in result set
				if (!context.StartsWith(ProviderName.Access))
					AreEqual(expected, actual);
			}
		}

		[Test]
		public void LetTest61([DataSources(TestProvName.AllOracle11, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
		{
			//LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var expected = (from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						p.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					})
					.Where(t => t.ParentID > 0)
					.ToArray();

				var actual = (
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID).OrderBy(c => c.ChildID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						p.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					}
				)
					.Where(t => t.ParentID > 0)
					.ToArray();

				// Access has different order in result set
				if (!context.StartsWith(ProviderName.Access))
					AreEqual(expected, actual);
			}
		}

		[ActiveIssue(Configuration = ProviderName.PostgreSQL92, Details = "Uses 3 queries and we join results in wrong order. See LetTest71 with explicit sort")]
		[Test]
		public void LetTest7([DataSources(TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana, ProviderName.Access)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
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

		[Test]
		public void LetTest71([DataSources(TestProvName.AllOracle11, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana, ProviderName.Access)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
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
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID).OrderBy(c => c.ChildID)
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

		[Test]
		public void LetTest8([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					let ch3	= ch2.OrderBy(c => c.ChildID).FirstOrDefault(c => c.ParentID > 0)
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
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					let ch3	= ch2.OrderBy(c => c.ChildID).FirstOrDefault(c => c.ParentID > 0)
					select new
					{
						First1 = ch3 == null ? 0 : ch3.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First2 = ch2.FirstOrDefault()
					});
		}

		[Test]
		public void LetTest9([DataSources(TestProvName.AllSybase)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.OrderBy(c => c.ChildID).Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Take(10)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.OrderBy(c => c.ChildID).Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Take(10));
		}

		[Test]
		public void LetTest10([DataSources] string context)
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "SELECT * query",
			Configurations = new[]
			{
				ProviderName.Access,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				ProviderName.PostgreSQL92,
				TestProvName.AllSQLite,
				TestProvName.AllSapHana,
				ProviderName.SqlServer2000,
				TestProvName.AllSybase
			})]
		[Test]
		public void LetTest11([DataSources] string context)
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
