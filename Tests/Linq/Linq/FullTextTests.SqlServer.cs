using NUnit.Framework;
using LinqToDB.DataProvider.SqlServer;
using Tests.Model;
using System.Linq;

namespace Tests.Linq
{
	[TestFixture]
	public partial class FullTextTests : TestBase
	{
		#region Issue 386 Tests
		[Test, Category("FreeText")]
		public void Issue386InnerJoinWithExpression([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from t in db.Product
					join c in db.Category.FreeTextTable<Northwind.Category, int>(c => c.Description, "sweetest candy bread and dry meat") on t.CategoryID equals c.Key
					orderby t.ProductName descending
					select t;
				var list = q.ToList();
				Assert.That(list.Count, Is.GreaterThan(0));

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'sweetest candy bread and dry meat')"));
			}
		}

		[Test, Category("FreeText")]
		public void Issue386LeftJoinWithExpression([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q
					= from t in db.Product
					  from c in db.Category.FreeTextTable<Northwind.Category, int>(c => c.Description, "sweetest candy bread and dry meat").Where(f => f.Key == t.CategoryID).DefaultIfEmpty()
					  orderby t.ProductName descending
					  select t;
				var list = q.ToList();
				Assert.That(list.Count, Is.GreaterThan(0));

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'sweetest candy bread and dry meat')"));
			}
		}
		#endregion

		#region FreeTextTable
		[Test, Category("FreeText")]
		public void FreeTextTableByColumn([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => t.Description, "sweetest candy bread and dry meat")
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(3, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(7, results[2].CategoryID);
				Assert.AreEqual(5, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'sweetest candy bread and dry meat')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => t.CategoryName, "meat", "Turkish", null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName]), N'meat', LANGUAGE N'Turkish')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnLanguageNameTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => t.Description, "food", "Thai", 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'food', LANGUAGE N'Thai', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => t.CategoryName, "sweetest candy bread and dry meat", 2057, null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName]), N'sweetest candy bread and dry meat', LANGUAGE 2057)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnLanguageCodeTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => t.Description, "sweetest candy bread and dry meat", 1045, 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'sweetest candy bread and dry meat', LANGUAGE 1045, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => t.Description, "sweetest candy bread and dry meat", top: 4)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'sweetest candy bread and dry meat', 4)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAll([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>("seafood bread")
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(3, results.Count);
				Assert.AreEqual(3, results[0].CategoryID);
				Assert.AreEqual(5, results[1].CategoryID);
				Assert.AreEqual(8, results[2].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>("seafood bread", "Russian", null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread', LANGUAGE N'Russian')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllLanguageNameTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>("seafood bread", "English", 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(2, results.Count);
				Assert.AreEqual(3, results[0].CategoryID);
				Assert.AreEqual(5, results[1].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread', LANGUAGE N'English', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>("seafood bread", 1062, null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread', LANGUAGE 1062)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllLanguageCodeTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>("seafood bread", 1053, 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread', LANGUAGE 1053, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>("seafood bread", top: 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(2, results.Count);
				Assert.AreEqual(3, results[0].CategoryID);
				Assert.AreEqual(5, results[1].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumns([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => new { t.CategoryName, t.Description }, "meat bread")
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(3, results.Count);
				Assert.AreEqual(6, results[0].CategoryID);
				Assert.AreEqual(3, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName], [Description]), N'meat bread')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => new { t.Description }, "meat bread", "Czech", null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'meat bread', LANGUAGE N'Czech')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsLanguageNameTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => new { t.Description, duplicate = t.Description }, "meat bread", "Bulgarian", 7)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description], [Description]), N'meat bread', LANGUAGE N'Bulgarian', 7)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => new { t.CategoryName }, "meat bread", 2068, null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName]), N'meat bread', LANGUAGE 2068)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsLanguageCodeTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => new { t.CategoryName, t.Description }, "meat bread", 2070, 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName], [Description]), N'meat bread', LANGUAGE 2070, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsTop([IncludeDataSources(TestProvName.Northwind)] string context, [Values(1,2,3,2)] int top)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.FreeTextTable<Northwind.Category, int>(t => new { t.CategoryName, t.Description }, "meat bread", top: top)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains($"FREETEXTTABLE([Categories], ([CategoryName], [Description]), N'meat bread', {top})"));
			}
		}

		#endregion

		#region ContainsTable
		[Test, Category("FreeText")]
		public void ContainsTableByColumn([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => t.Description, "sweetest &! meat")
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'sweetest &! meat')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => t.CategoryName, "meat", "Turkish", null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName]), N'meat', LANGUAGE N'Turkish')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnLanguageNameTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => t.Description, "food", "Thai", 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'food', LANGUAGE N'Thai', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => t.CategoryName, "sweetest NEAR candy", 2057, null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName]), N'sweetest NEAR candy', LANGUAGE 2057)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnLanguageCodeTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => t.Description, "bread", 1045, 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'bread', LANGUAGE 1045, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => t.Description, "bread AND NOT meat", top: 4)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'bread AND NOT meat', 4)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAll([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>("seafood OR bread")
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood OR bread')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>("seafood OR bread", "Russian", null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood OR bread', LANGUAGE N'Russian')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllLanguageNameTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>("seafood | bread", "English", 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood | bread', LANGUAGE N'English', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>("seafood AND bread", 1062, null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood AND bread', LANGUAGE 1062)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllLanguageCodeTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>("NEAR(seafood, \"bread\")", 1053, 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'NEAR(seafood, \"bread\")', LANGUAGE 1053, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>("seafood & bread", top: 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood & bread', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumns([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => new { t.CategoryName, t.Description }, "meat NEAR bread")
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName], [Description]), N'meat NEAR bread')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => new { t.Description }, "meat OR bread", "Czech", null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'meat OR bread', LANGUAGE N'Czech')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsLanguageNameTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => new { t.Description, duplicate = t.Description }, "bread", "Bulgarian", 7)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description], [Description]), N'bread', LANGUAGE N'Bulgarian', 7)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => new { t.CategoryName }, "meat OR bread", 2068, null)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName]), N'meat OR bread', LANGUAGE 2068)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsLanguageCodeTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => new { t.CategoryName, t.Description }, "meat AND bread", 2070, 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName], [Description]), N'meat AND bread', LANGUAGE 2070, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsTop([IncludeDataSources(TestProvName.Northwind)] string context, [Values(1, 2, 3, 2)] int top)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in db.Category.ContainsTable<Northwind.Category, int>(t => new { t.CategoryName, t.Description }, "meat", top: top)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains($"CONTAINSTABLE([Categories], ([CategoryName], [Description]), N'meat', {top})"));
			}
		}

		#endregion
	}

}
