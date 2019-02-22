using System;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using System.Data.Linq.SqlClient;
#else
using System.Data;
#endif

using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class StringFunctionTests : TestBase
	{
		[Test]
		public void Length([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Length == "John".Length && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsConstant([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Contains("oh") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsConstant2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !p.FirstName.Contains("o%h") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsConstant3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var arr = new[] { "oh", "oh'", "oh\\" };

				var q = from p in db.Person where  arr.Contains(p.FirstName) select p;
				Assert.AreEqual(0, q.Count());
			}
		}

		[Test]
		public void ContainsConstant4([DataSources(ProviderName.Firebird, TestProvName.Firebird3)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var s = "123[456";

				var q = from p in db.Person where p.ID == 1 && s.Contains("[") select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsConstant5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 && "123[456".Contains("[") select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsConstant41([DataSources(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var s  = "123[456";
				var ps = "[";

				var q = from p in db.Person where p.ID == 1 && s.Contains(ps) select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsConstant51([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ps = "[";

				var q = from p in db.Person where p.ID == 1 && "123[456".Contains(ps) select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsParameter1([DataSources] string context)
		{
			var str = "oh";

			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Contains(str) && p.ID == 1 select new { p, str };
				var r = q.ToList().First();
				Assert.AreEqual(1,   r.p.ID);
				Assert.AreEqual(str, r.str);
			}
		}

		[Test]
		public void ContainsParameter2([DataSources] string context)
		{
			var str = "o%h";

			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !p.FirstName.Contains(str) && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsParameter3()
		{
			var str = "o";

			using (var db = new TestDataConnection())
			{
				var q =
					from d in db.Doctor
					join p in db.Person.Where(p => p.FirstName.Contains(str))
					on d.PersonID equals p.ID
					select p;

				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ContainsParameter4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Person
					select new
					{
						p,
						Field1 = p.FirstName.Contains("Jo")
					} into p
					where p.Field1
					orderby p.Field1
					select p,
					from p in db.Person
					select new
					{
						p,
						Field1 = p.FirstName.Contains("Jo")
					} into p
					where p.Field1
					orderby p.Field1
					select p);
			}
		}

		[Test]
		public void ContainsNull([DataSources(ProviderName.Access, ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				string firstName = null;
				int?   id        = null;

				var _ =
				(
					from p in db.Person
					where
						(int?)p.ID == id &&
						(string.IsNullOrEmpty(firstName) || p.FirstName.Contains(firstName))
					select p
				).ToList();
			}
		}

		[Test]
		public void StartsWith1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.StartsWith("Jo") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void StartsWith2([DataSources(ProviderName.DB2, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where "John123".StartsWith(p.FirstName) select p,
					from p in db.Person where "John123".StartsWith(p.FirstName) select p);
		}

		[Test]
		public void StartsWith3([DataSources(ProviderName.DB2, ProviderName.Access)] string context)
		{
			var str = "John123";

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where str.StartsWith(p.FirstName) select p,
					from p in db.Person where str.StartsWith(p.FirstName) select p);
		}

		[Test]
		public void StartsWith4([DataSources(ProviderName.DB2, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Person
					from p2 in    Person
					where p1.ID == p2.ID && p1.FirstName.StartsWith(p2.FirstName)
					select p1,
					from p1 in db.Person
					from p2 in db.Person
					where p1.ID == p2.ID &&
						Sql.Like(p1.FirstName, p2.FirstName.Replace("%", "~%"), '~')
					select p1);
		}

		[Test]
		public void StartsWith5([DataSources(ProviderName.DB2, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Person
					from p2 in    Person
					where p1.ID == p2.ID && p1.FirstName.Replace("J", "%").StartsWith(p2.FirstName.Replace("J", "%"))
					select p1,
					from p1 in db.Person
					from p2 in db.Person
					where p1.ID == p2.ID && p1.FirstName.Replace("J", "%").StartsWith(p2.FirstName.Replace("J", "%"))
					select p1);
		}

		[Test]
		public void EndsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.EndsWith("hn") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		[Test]
		public void Like11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where SqlMethods.Like(p.FirstName, "%hn%") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Like12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !SqlMethods.Like(p.FirstName, @"%h~%n%", '~') && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}
#endif

		[Test]
		public void Like21([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Like(p.FirstName, "%hn%") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Like22([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !Sql.Like(p.FirstName, @"%h~%n%", '~') && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void IndexOf11([DataSources(ProviderName.Informix, ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.IndexOf("oh") == 1 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void IndexOf12([DataSources(ProviderName.Informix, ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.IndexOf("") == 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void IndexOf2([DataSources(ProviderName.Informix, ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.LastName.IndexOf("e", 2) == 4 && p.ID == 2 select p;
				Assert.AreEqual(2, q.ToList().First().ID);
			}
		}

		[ActiveIssue(Configurations = new[] { ProviderName.SapHana })]
		[Test]
		public void IndexOf3([DataSources(
			ProviderName.DB2, ProviderName.Firebird, TestProvName.Firebird3, ProviderName.Informix,
			ProviderName.SqlCe, ProviderName.Sybase, ProviderName.Access, ProviderName.SQLiteMS)]
			string context)
		{
			var s = "e";
			var n1 = 2;
			var n2 = 5;

			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.LastName.IndexOf(s, n1, n2) == 1 && p.ID == 2 select p;
				Assert.AreEqual(2, q.ToList().First().ID);
			}
		}

		[Test]
		public void LastIndexOf1([DataSources(
			ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix,
			ProviderName.SqlCe, ProviderName.Access, ProviderName.SapHana, ProviderName.SQLiteMS)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.LastName.LastIndexOf("p") == 2 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void LastIndexOf2([DataSources(
			ProviderName.DB2, ProviderName.Informix, ProviderName.SqlCe,
			ProviderName.Access, ProviderName.SapHana, ProviderName.SQLiteMS)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { p.ID, FirstName = "123" + p.FirstName + "012345" };
				q = q.Where(p => p.FirstName.LastIndexOf("123", 5) == 8);
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void LastIndexOf3([DataSources(
			ProviderName.DB2, ProviderName.Informix, ProviderName.SqlCe,
			ProviderName.Access, ProviderName.SapHana, ProviderName.SQLiteMS)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { p.ID, FirstName = "123" + p.FirstName + "0123451234" };
				q = q.Where(p => p.FirstName.LastIndexOf("123", 5, 6) == 8);
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CharIndex1([DataSources(ProviderName.Informix, ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.CharIndex("oh", p.FirstName) == 2 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CharIndex2([DataSources(ProviderName.Informix, ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.CharIndex("p", p.LastName, 2) == 3 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Left([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Left(p.FirstName, 2) == "Jo" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Right([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Right(p.FirstName, 3) == "ohn" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void RightInSelect([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select Sql.Right(p.FirstName, 3);
				Assert.AreEqual("ohn", q.ToList().First());
			}
		}

		[Test]
		public void Substring1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Substring(1) == "ohn" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Substring2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Substring(1, 2) == "oh" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Reverse([DataSources(
			ProviderName.DB2, ProviderName.Informix, ProviderName.SqlCe,
			ProviderName.Access, ProviderName.SapHana, ProviderName.SQLiteMS)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Reverse(p.FirstName) == "nhoJ" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Stuff1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Stuff(p.FirstName, 3, 1, "123") == "Jo123n" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

#pragma warning disable 0109
		new class Category
		{
			[PrimaryKey, Identity] public int    Id;
			[Column, NotNull]      public string Name;
		}
#pragma warning restore 0109

		class Task
		{
			[PrimaryKey, Identity] public int    Id;
			[Column, NotNull]      public string Name;
		}

		class TaskCategory
		{
			[Column, NotNull] public int Id;
			[Column, NotNull] public int TaskId;
			[Column, NotNull] public int CategoryId;
		}

		[Test]
		public void Stuff2([IncludeDataSources(
			ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GetTable<Task>()
					join tc in db.GetTable<TaskCategory>() on t.Id equals tc.TaskId into g
					from tc in g.DefaultIfEmpty()
					select new
					{
						t.Id,
						t.Name,
						Categories = Sql.Stuff(
							from c in db.GetTable<Category>()
							where c.Id == tc.CategoryId
							select "," + c.Name, 1, 1, "")
					};

				q.ToString();
			}
		}

		[Test]
		public void Insert([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Insert(2, "123") == "Jo123hn" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Remove1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Remove(2) == "Jo" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Remove2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Remove(1, 2) == "Jn" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Space([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName + Sql.Space(p.ID + 1) + "123" == "John  123" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void PadRight([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.PadRight(p.FirstName, 6, ' ') + "123" == "John  123" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void PadRight1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.PadRight(6) + "123" == "John  123" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void PadRight2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.PadRight(6, '*') + "123" == "John**123" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void PadLeft([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where "123" + Sql.PadLeft(p.FirstName, 6, ' ') == "123  John" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void PadLeft1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where "123" + p.FirstName.PadLeft(6) == "123  John" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void PadLeft2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where "123" + p.FirstName.PadLeft(6, '*') == "123**John" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Replace([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Replace("hn", "lie") == "Jolie" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Trim([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person where p.ID == 1 select new { p.ID, Name = "  " + p.FirstName + " " } into pp
					where pp.Name.Trim() == "John" select pp;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void TrimLeft([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person where p.ID == 1 select new { p.ID, Name = "  " + p.FirstName + " " } into pp
					where pp.Name.TrimStart() == "John " select pp;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void TrimRight([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person where p.ID == 1 select new { p.ID, Name = "  " + p.FirstName + " " } into pp
					where pp.Name.TrimEnd() == "  John" select pp;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ToLower([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.ToLower() == "john" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ToLowerParameter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var param = "JOHN";
				var q = from p in db.Person where p.FirstName.ToLower() == param.ToLower() && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ToUpper([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.ToUpper() == "JOHN" && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void ToUpperParam([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var param = "john";
				var q = from p in db.Person where p.FirstName.ToUpper() == param.ToUpper() && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareTo([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("John") == 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareToNotEqual1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Jo") != 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareToNotEqual2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where 0 != p.FirstName.CompareTo("Jo") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareTo1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Joh") > 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareTo2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Johnn") < 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareTo21([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Johnn") <= 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareTo22([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where 0 >= p.FirstName.CompareTo("Johnn") && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

#if !NETSTANDARD1_6
		[Test]
		public void CompareTo3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo(55) > 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareTo31([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo(55) >= 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareTo32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where 0 <= p.FirstName.CompareTo(55) && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}
#endif

		[Test]
		public void CompareOrdinal1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.CompareOrdinal(p.FirstName, "Joh") > 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CompareOrdinal2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.CompareOrdinal(p.FirstName, 1, "Joh", 1, 2) == 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Compare1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.Compare(p.FirstName, "Joh") > 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Compare2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.Compare(p.FirstName, "joh", true) > 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Compare3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.Compare(p.FirstName, 1, "Joh", 1, 2) == 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void Compare4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
#if !NETSTANDARD1_6
				var q = from p in db.Person where string.Compare(p.FirstName, 1, "Joh", 1, 2, true) == 0 && p.ID == 1 select p;
#else
				var q = from p in db.Person where string.Compare(p.FirstName, 1, "Joh", 1, 2, StringComparison.OrdinalIgnoreCase) == 0 && p.ID == 1 select p;
#endif
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void IsNullOrEmpty1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !string.IsNullOrEmpty(p.FirstName) && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void IsNullOrEmpty2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select string.IsNullOrEmpty(p.FirstName);
				Assert.AreEqual(false, q.ToList().First());
			}
		}
	}
}
