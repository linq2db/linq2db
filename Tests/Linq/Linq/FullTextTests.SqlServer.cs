using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using NUnit.Framework;
using System.Linq;
using Tests.Model;

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
					join c in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, c => c.Description, "sweetest candy bread and dry meat") on t.CategoryID equals c.Key
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
					  from c in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, c => c.Description, "sweetest candy bread and dry meat").Where(f => f.Key == t.CategoryID).DefaultIfEmpty()
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
					join t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, t => t.Description, "sweetest candy bread and dry meat")
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => t.CategoryName, "meat", "Turkish")
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => t.Description, "food", "Thai", 2)
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => t.CategoryName, "sweetest candy bread and dry meat", 2057)
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => t.Description, "sweetest candy bread and dry meat", 1045, 2)
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
					join t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, t => t.Description, "sweetest candy bread and dry meat", 4)
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
					join t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, "seafood bread")
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, "seafood bread", "Russian")
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, "seafood bread", "English", 2)
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, "seafood bread", 1062)
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, "seafood bread", 1053, 2)
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
					join t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, "seafood bread", 2)
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
					join t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat bread")
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.Description }, "meat bread", "Czech")
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.Description, duplicate = t.Description }, "meat bread", "Bulgarian", 7)
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName }, "meat bread", 2068)
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
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat bread", 2070, 2)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName], [Description]), N'meat bread', LANGUAGE 2070, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsTop([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat bread", 3)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName], [Description]), N'meat bread', 3)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableWithParameters(
			[IncludeDataSources(TestProvName.Northwind)] string context,
			[Values("meat", "bread")] string search,
			[Values(1033, 1048)] int lang,
			[Values(1, 2, 3, 2)] int top)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					join t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, search, lang, top)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains($"FREETEXTTABLE([Categories], ([CategoryName], [Description]), N'{search}', LANGUAGE {lang}, {top})"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, t => t.Description, "sweetest candy bread and dry meat").Where(t => c.CategoryID == t.Key)
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
		public void FreeTextTableByColumnLanguageNameAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => t.CategoryName, "meat", "Turkish").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName]), N'meat', LANGUAGE N'Turkish')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnLanguageNameTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => t.Description, "food", "Thai", 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'food', LANGUAGE N'Thai', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnLanguageCodeAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => t.CategoryName, "sweetest candy bread and dry meat", 2057).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName]), N'sweetest candy bread and dry meat', LANGUAGE 2057)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnLanguageCodeTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => t.Description, "sweetest candy bread and dry meat", 1045, 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'sweetest candy bread and dry meat', LANGUAGE 1045, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, t => t.Description, "sweetest candy bread and dry meat", 4).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'sweetest candy bread and dry meat', 4)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, "seafood bread").Where(t => c.CategoryID == t.Key)
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
		public void FreeTextTableByAllLanguageNameAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, "seafood bread", "Russian").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread', LANGUAGE N'Russian')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllLanguageNameTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, "seafood bread", "English", 2).Where(t => c.CategoryID == t.Key)
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
		public void FreeTextTableByAllLanguageCodeAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, "seafood bread", 1062).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread', LANGUAGE 1062)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllLanguageCodeTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, "seafood bread", 1053, 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], *, N'seafood bread', LANGUAGE 1053, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByAllTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, "seafood bread", 2).Where(t => c.CategoryID == t.Key)
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
		public void FreeTextTableByColumnsAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat bread").Where(t => c.CategoryID == t.Key)
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
		public void FreeTextTableByColumnsLanguageNameAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.Description }, "meat bread", "Czech").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description]), N'meat bread', LANGUAGE N'Czech')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsLanguageNameTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.Description, duplicate = t.Description }, "meat bread", "Bulgarian", 7).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([Description], [Description]), N'meat bread', LANGUAGE N'Bulgarian', 7)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsLanguageCodeAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName }, "meat bread", 2068).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName]), N'meat bread', LANGUAGE 2068)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsLanguageCodeTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat bread", 2070, 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName], [Description]), N'meat bread', LANGUAGE 2070, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableByColumnsTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context, [Values(1, 2, 3, 2)] int top)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTable<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat bread", top).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName], [Description]), N'meat bread', @top)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableWithParameters2(
			[IncludeDataSources(TestProvName.Northwind)] string context,
			[Values("meat", "bread")] string search,
			[Values(1033, 1048)] int lang,
			[Values(1, 2, 3, 2)] int top)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, search, lang, top).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("FREETEXTTABLE([Categories], ([CategoryName], [Description]), @search0, LANGUAGE @lang, @top)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextTableWithLinqService([IncludeDataSources(true, TestProvName.Northwind)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from c in db.GetTable<Northwind.Category>()
					from t in Sql.Ext.SqlServer().FreeTextTableWithLanguage<Northwind.Category, int>(db.GetTable<Northwind.Category>(), "seafood bread", 1053, 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);
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
					join t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, t => t.Description, "sweetest &! meat")
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => t.CategoryName, "meat", "Turkish")
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => t.Description, "food", "Thai", 2)
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => t.CategoryName, "sweetest NEAR candy", 2057)
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => t.Description, "bread", 1045, 2)
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
					join t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, t => t.Description, "bread AND NOT meat", 4)
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
					join t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, "seafood OR bread")
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, "seafood OR bread", "Russian")
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, "seafood | bread", "English", 2)
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, "seafood AND bread", 1062)
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, "NEAR(seafood, \"bread\")", 1053, 2)
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
					join t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, "seafood & bread", 2)
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
					join t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat NEAR bread")
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.Description }, "meat OR bread", "Czech")
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.Description, duplicate = t.Description }, "bread", "Bulgarian", 7)
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName }, "meat OR bread", 2068)
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
					join t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat AND bread", 2070, 2)
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
					join t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat", top)
					on c.CategoryID equals t.Key
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains($"CONTAINSTABLE([Categories], ([CategoryName], [Description]), N'meat', {top})"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, t => t.Description, "sweetest &! meat").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'sweetest &! meat')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnLanguageNameAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => t.CategoryName, "meat", "Turkish").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName]), N'meat', LANGUAGE N'Turkish')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnLanguageNameTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => t.Description, "food", "Thai", 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'food', LANGUAGE N'Thai', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnLanguageCodeAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => t.CategoryName, "sweetest NEAR candy", 2057).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName]), N'sweetest NEAR candy', LANGUAGE 2057)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnLanguageCodeTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => t.Description, "bread", 1045, 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'bread', LANGUAGE 1045, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, t => t.Description, "bread AND NOT meat", 4).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'bread AND NOT meat', 4)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, "seafood OR bread").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood OR bread')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllLanguageNameAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, "seafood OR bread", "Russian").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood OR bread', LANGUAGE N'Russian')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllLanguageNameTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, "seafood | bread", "English", 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood | bread', LANGUAGE N'English', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllLanguageCodeAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, "seafood AND bread", 1062).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood AND bread', LANGUAGE 1062)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllLanguageCodeTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, "NEAR(seafood, \"bread\")", 1053, 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'NEAR(seafood, \"bread\")', LANGUAGE 1053, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByAllTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, "seafood & bread", 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], *, N'seafood & bread', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat NEAR bread").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName], [Description]), N'meat NEAR bread')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsLanguageNameAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.Description }, "meat OR bread", "Czech").Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description]), N'meat OR bread', LANGUAGE N'Czech')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsLanguageNameTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.Description, duplicate = t.Description }, "bread", "Bulgarian", 7).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([Description], [Description]), N'bread', LANGUAGE N'Bulgarian', 7)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsLanguageCodeAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName }, "meat OR bread", 2068).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName]), N'meat OR bread', LANGUAGE 2068)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsLanguageCodeTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat AND bread", 2070, 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName], [Description]), N'meat AND bread', LANGUAGE 2070, 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableByColumnsTopAsExpressionMethod([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTable<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, "meat", 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName], [Description]), N'meat', 2)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableWithParameters(
			[IncludeDataSources(TestProvName.Northwind)] string context,
			[Values("meat", "bread")] string search,
			[Values("English", "Russian")] string lang,
			[Values(1, 2, 3, 2)] int top)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.Category, t => new { t.CategoryName, t.Description }, search, lang, top).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINSTABLE([Categories], ([CategoryName], [Description]), @search0, LANGUAGE @lang, @top)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsTableWithLinqService([IncludeDataSources(true, TestProvName.Northwind)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from c in db.GetTable<Northwind.Category>()
					from t in Sql.Ext.SqlServer().ContainsTableWithLanguage<Northwind.Category, int>(db.GetTable<Northwind.Category>(), "seafood | bread", "English", 2).Where(t => c.CategoryID == t.Key)
					orderby t.Rank descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(8, results[0].CategoryID);
			}
		}
		#endregion

		#region FreeText
		[Test, Category("FreeText")]
		public void FreeTextByTableAll([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeText(c, "sweetest candy bread and dry meat")
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT([c_1].*, N'sweetest candy bread and dry meat')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByTableAllLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeTextWithLanguage(c, "sweetest candy bread and dry meat", "English")
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT([c_1].*, N'sweetest candy bread and dry meat', LANGUAGE N'English')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByTableAllLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeTextWithLanguage(c, "sweetest candy bread and dry meat", 1033)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT([c_1].*, N'sweetest candy bread and dry meat', LANGUAGE 1033)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByColumn([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeText(c, "sweetest candy bread and dry meat", c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT(([c_1].[Description]), N'sweetest candy bread and dry meat')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByColumnLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeTextWithLanguage(c, "sweetest candy bread and dry meat", "English", c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT(([c_1].[Description]), N'sweetest candy bread and dry meat', LANGUAGE N'English')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByColumnLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeTextWithLanguage(c, "sweetest candy bread and dry meat", 1033, c.CategoryName)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(6, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT(([c_1].[CategoryName]), N'sweetest candy bread and dry meat', LANGUAGE 1033)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByColumns([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeText(c, "sweetest candy bread and dry meat", c.Description, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT(([c_1].[Description], [c_1].[Description]), N'sweetest candy bread and dry meat')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByColumnsLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeTextWithLanguage(c, "sweetest candy bread and dry meat", "English", c.CategoryName, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT(([c_1].[CategoryName], [c_1].[Description]), N'sweetest candy bread and dry meat', LANGUAGE N'English')"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByColumnsLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeTextWithLanguage(c, "sweetest candy bread and dry meat", 1033, c.CategoryName, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT(([c_1].[CategoryName], [c_1].[Description]), N'sweetest candy bread and dry meat', LANGUAGE 1033)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextWithParameters(
			[IncludeDataSources(TestProvName.Northwind)] string context,
			[Values("sweetest candy bread and dry meat")] string search,
			[Values("English", "French", "English")] string lang)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().FreeTextWithLanguage(c, search, lang, c.CategoryName, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				if (lang == "English")
				{
					Assert.AreEqual(4, results.Count);
					Assert.AreEqual(7, results[0].CategoryID);
					Assert.AreEqual(6, results[1].CategoryID);
					Assert.AreEqual(5, results[2].CategoryID);
					Assert.AreEqual(3, results[3].CategoryID);
				}
				else
				{
					Assert.AreEqual(1, results.Count);
					Assert.AreEqual(6, results[0].CategoryID);
				}

				Assert.That(db.LastQuery.Contains("FREETEXT(([c_1].[CategoryName], [c_1].[Description]), @search0, LANGUAGE @lang)"));
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextWithLinqService([IncludeDataSources(true, TestProvName.Northwind)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from c in db.GetTable<Northwind.Category>()
					where Sql.Ext.SqlServer().FreeTextWithLanguage(c, "sweetest candy bread and dry meat", "English", c.CategoryName, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(4, results.Count);
				Assert.AreEqual(7, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);
				Assert.AreEqual(5, results[2].CategoryID);
				Assert.AreEqual(3, results[3].CategoryID);
			}
		}

		[Test, Category("FreeText")]
		public void FreeTextByTwoTables([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c1 in db.Category
					from c2 in db.Category.Where(c => Sql.Ext.SqlServer().FreeText(c, "bread") && Sql.Ext.SqlServer().FreeText(c1, "meat"))
					orderby c1.CategoryID descending
					select c1;

				var results = q.ToList();

				Assert.AreEqual(2, results.Count);
				Assert.AreEqual(6, results[0].CategoryID);
				Assert.AreEqual(6, results[1].CategoryID);

				Assert.That(db.LastQuery.Contains("FREETEXT([c_1].*, N'bread')"));
				Assert.That(db.LastQuery.Contains("FREETEXT([c1].*, N'meat')"));
			}
		}

		#endregion

		#region Contains
		[Test, Category("FreeText")]
		public void ContainsByTableAll([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().Contains(c, "candy OR meat")
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(6, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINS([c_1].*, N'candy OR meat')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByTableAllLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsWithLanguage(c, "dry", "English")
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(0, results.Count);

				Assert.That(db.LastQuery.Contains("CONTAINS([c_1].*, N'dry', LANGUAGE N'English')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByTableAllLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsWithLanguage(c, "sweetest", 1033)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(0, results.Count);

				Assert.That(db.LastQuery.Contains("CONTAINS([c_1].*, N'sweetest', LANGUAGE 1033)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByColumn([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().Contains(c, "bread", c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(0, results.Count);

				Assert.That(db.LastQuery.Contains("CONTAINS(([c_1].[Description]), N'bread')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByColumnLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsWithLanguage(c, "dry & bread", "English", c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(0, results.Count);

				Assert.That(db.LastQuery.Contains("CONTAINS(([c_1].[Description]), N'dry & bread', LANGUAGE N'English')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByColumnLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsWithLanguage(c, "candy | meat", 1033, c.CategoryName)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(6, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINS(([c_1].[CategoryName]), N'candy | meat', LANGUAGE 1033)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByColumns([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().Contains(c, "aнанас", c.Description, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(0, results.Count);

				Assert.That(db.LastQuery.Contains("CONTAINS(([c_1].[Description], [c_1].[Description]), N'aнанас')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByColumnsLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsWithLanguage(c, "salo & bread", "English", c.CategoryName, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(0, results.Count);

				Assert.That(db.LastQuery.Contains("CONTAINS(([c_1].[CategoryName], [c_1].[Description]), N'salo & bread', LANGUAGE N'English')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByColumnsLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsWithLanguage(c, "meat", 1033, c.CategoryName, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(6, results[0].CategoryID);

				Assert.That(db.LastQuery.Contains("CONTAINS(([c_1].[CategoryName], [c_1].[Description]), N'meat', LANGUAGE 1033)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsWithParameters(
			[IncludeDataSources(TestProvName.Northwind)] string context,
			[Values("bread", "meat")] string search,
			[Values(1033, 1036, 1033)] int code)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsWithLanguage(c, search, code, c.CategoryName, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.That(db.LastQuery.Contains("CONTAINS(([c_1].[CategoryName], [c_1].[Description]), @search0, LANGUAGE @code)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsWithLinqService([IncludeDataSources(true, TestProvName.Northwind)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from c in db.GetTable<Northwind.Category>()
					where Sql.Ext.SqlServer().ContainsWithLanguage(c, "candy", "English", c.CategoryName, c.Description)
					orderby c.CategoryID descending
					select c;

				var results = q.ToList();

				Assert.AreEqual(0, results.Count);
			}
		}

		[Test, Category("FreeText")]
		public void ContainsByTwoTables([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c1 in db.Category
					from c2 in db.Category.Where(c => Sql.Ext.SqlServer().Contains(c, "bread") && Sql.Ext.SqlServer().Contains(c1, "meat"))
					orderby c1.CategoryID descending
					select c1;

				var results = q.ToList();

				Assert.AreEqual(0, results.Count);

				Assert.That(db.LastQuery.Contains("CONTAINS([c_1].*, N'bread')"));
				Assert.That(db.LastQuery.Contains("CONTAINS([c1].*, N'meat')"));
			}
		}

		#endregion

		#region ContainsProperty
		// TODO: we don't test ContainsProperty against database right now as we don't have configured test database for it

		[Test, Category("FreeText")]
		public void ContainsPropertyByColumn([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsProperty(c.Description, "title", "bread")
					orderby c.CategoryID descending
					select c;

				Assert.That(q.ToString().Contains("CONTAINS(PROPERTY([c_1].[Description], N'title'), N'bread')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsPropertyByColumnLanguageName([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsPropertyWithLanguage(c.Description, "Title", "dry & bread", "English")
					orderby c.CategoryID descending
					select c;

				Assert.That(q.ToString().Contains("CONTAINS(PROPERTY([c_1].[Description], N'Title'), N'dry & bread', LANGUAGE N'English')"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsPropertyByColumnLanguageCode([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsPropertyWithLanguage(c.CategoryName, "Title", "candy | meat", 1033)
					orderby c.CategoryID descending
					select c;

				Assert.That(q.ToString().Contains("CONTAINS(PROPERTY([c_1].[CategoryName], N'Title'), N'candy | meat', LANGUAGE 1033)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsPropertyByColumnWithParameters(
			[IncludeDataSources(TestProvName.Northwind)] string context,
			[Values("Title", "Author", "Title")] string property,
			[Values("bread", "meat")] string search)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsProperty(c.Description, property, search)
					orderby c.CategoryID descending
					select c;

				Assert.That(q.ToString().Contains($"CONTAINS(PROPERTY([c_1].[Description], N'{property}'), @search0)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsPropertyByColumnLanguageNameWithParameters(
			[IncludeDataSources(TestProvName.Northwind)] string context,
			[Values("Title", "Author", "Title")] string property,
			[Values("bread", "meat")] string search,
			[Values("English", "Russian")] string lang)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsPropertyWithLanguage(c.Description, property, search, lang)
					orderby c.CategoryID descending
					select c;

				Assert.That(q.ToString().Contains($"CONTAINS(PROPERTY([c_1].[Description], N'{property}'), @search0, LANGUAGE @lang)"));
			}
		}

		[Test, Category("FreeText")]
		public void ContainsPropertyByColumnLanguageCodeWithParameters(
			[IncludeDataSources(TestProvName.Northwind)] string context,
			[Values("Title", "Author", "Title")] string property,
			[Values("bread", "meat")] string search,
			[Values(1033, 1029)] int lang)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from c in db.Category
					where Sql.Ext.SqlServer().ContainsPropertyWithLanguage(c.CategoryName, property, search, lang)
					orderby c.CategoryID descending
					select c;

				Assert.That(q.ToString().Contains($"CONTAINS(PROPERTY([c_1].[CategoryName], N'{property}'), @search0, LANGUAGE @lang)"));
			}
		}
		#endregion

	}

}
