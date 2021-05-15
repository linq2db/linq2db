using System;
#if NET472
using System.Data.Linq.SqlClient;
#else
using System.Data;
#endif

using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class StringFunctionTests : TestBase
	{
		#region Non-Database tests

		[Test]
		public void Length()
		{
			Assert.AreEqual(null, Sql.Length((string)null!));
			Assert.AreEqual(0,    Sql.Length(string.Empty));
			Assert.AreEqual(4,    Sql.Length("test"));
		}

		[Test]
		public void Substring()
		{
			Assert.AreEqual(null, Sql.Substring(null,   0,    0));
			Assert.AreEqual(null, Sql.Substring("test", null, 0));
			Assert.AreEqual(null, Sql.Substring("test", -1,   0));
			Assert.AreEqual(null, Sql.Substring("test", 5,    0));
			Assert.AreEqual(null, Sql.Substring("test", 0,    null));
			Assert.AreEqual(null, Sql.Substring("test", 0,    -1));

			Assert.AreEqual("",   Sql.Substring("test", 3,    0));
			Assert.AreEqual("s",  Sql.Substring("test", 3,    1));
			Assert.AreEqual("st", Sql.Substring("test", 3,    2));
			Assert.AreEqual("st", Sql.Substring("test", 3,    3));
		}

		[Test]
		public void Like()
		{
#if !NETFRAMEWORK
			Assert.Throws<InvalidOperationException>(() => Sql.Like(null, null));
			Assert.Throws<InvalidOperationException>(() => Sql.Like(null, null, null));
#else
			Assert.Pass("We don't test server-side method here.");
#endif
		}

		[Test]
		public void CharIndex1()
		{
			Assert.AreEqual(null, Sql.CharIndex("",            null));
			Assert.AreEqual(null, Sql.CharIndex((string)null!, "test"));

			Assert.AreEqual(0,    Sql.CharIndex("",            "test"));
			Assert.AreEqual(0,    Sql.CharIndex("g",           "test"));
			Assert.AreEqual(3,    Sql.CharIndex("st",          "test"));
		}

		[Test]
		public void CharIndex2()
		{
			Assert.AreEqual(null, Sql.CharIndex("",            null,   0));
			Assert.AreEqual(null, Sql.CharIndex((string)null!, "test", 0));
			Assert.AreEqual(null, Sql.CharIndex("st",          "test", null));

			Assert.AreEqual(0,    Sql.CharIndex("",            "test", 0));
			Assert.AreEqual(0,    Sql.CharIndex("g",           "test", 0));
			Assert.AreEqual(3,    Sql.CharIndex("st",          "test", -1));
			Assert.AreEqual(3,    Sql.CharIndex("st",          "test", 2));
			Assert.AreEqual(0,    Sql.CharIndex("st",          "test", 4));
			Assert.AreEqual(0,    Sql.CharIndex("st",          "test", 5));
		}

		[Test]
		public void CharIndex3()
		{
			Assert.AreEqual(null, Sql.CharIndex('t',           null));
			Assert.AreEqual(null, Sql.CharIndex((char?)null!,  "test"));

			Assert.AreEqual(0,    Sql.CharIndex(Char.MinValue, "test"));
			Assert.AreEqual(0,    Sql.CharIndex('g',           "test"));
			Assert.AreEqual(3,    Sql.CharIndex('s',           "test"));
		}

		[Test]
		public void CharIndex4()
		{
			Assert.AreEqual(null, Sql.CharIndex('t',           null,   0));
			Assert.AreEqual(null, Sql.CharIndex((char?)null!,  "test", 0));
			Assert.AreEqual(null, Sql.CharIndex('t',           "test", null));

			Assert.AreEqual(0,    Sql.CharIndex(Char.MinValue, "test", 0));
			Assert.AreEqual(0,    Sql.CharIndex('g',           "test", 0));
			Assert.AreEqual(3,    Sql.CharIndex('s',           "test", -1));
			Assert.AreEqual(3,    Sql.CharIndex('s',           "test", 2));
			Assert.AreEqual(0,    Sql.CharIndex('s',           "test", 4));
			Assert.AreEqual(0,    Sql.CharIndex('s',           "test", 5));
		}

		[Test]
		public void Reverse()
		{
			Assert.AreEqual(null,         Sql.Reverse(null));
			Assert.AreEqual(string.Empty, Sql.Reverse(string.Empty));
			Assert.AreEqual("dcba",       Sql.Reverse("abcd"));
		}

		[Test]
		public void Left()
		{
			Assert.AreEqual(null,   Sql.Left(null,   0));
			Assert.AreEqual(null,   Sql.Left("test", null));
			Assert.AreEqual(null,   Sql.Left("test", -1));
			Assert.AreEqual("",     Sql.Left("test", 0));
			Assert.AreEqual("te",   Sql.Left("test", 2));
			Assert.AreEqual("test", Sql.Left("test", 5));
		}

		[Test]
		public void Right()
		{
			Assert.AreEqual(null,   Sql.Right(null,   0));
			Assert.AreEqual(null,   Sql.Right("test", null));
			Assert.AreEqual(null,   Sql.Right("test", -1));
			Assert.AreEqual("",     Sql.Right("test", 0));
			Assert.AreEqual("st",   Sql.Right("test", 2));
			Assert.AreEqual("test", Sql.Right("test", 5));
		}

		[Test]
		public void Stuff1()
		{
			// Disallowed null parameters
			Assert.AreEqual(null,       Sql.Stuff((string)null!, 1,    1,    "test"));
			Assert.AreEqual(null,       Sql.Stuff("test",        null, 1,    "test"));
			Assert.AreEqual(null,       Sql.Stuff("test",        1,    null, "test"));
			Assert.AreEqual(null,       Sql.Stuff("test",        1,    1,    null));

			// Disallowed start
			Assert.AreEqual(null,       Sql.Stuff("test",        0,    1,    "test"));
			Assert.AreEqual(null,       Sql.Stuff("test",        5,    1,    "test"));

			// Disallowed length
			Assert.AreEqual(null,       Sql.Stuff("test",        1,    -1,   "test"));

			// Correct start and length
			Assert.AreEqual("5678",     Sql.Stuff("1234",        1,    4,    "5678"));

			// Correct start
			Assert.AreEqual("12356784", Sql.Stuff("1234",        4,    0,    "5678"));

			// Correct length												 
			Assert.AreEqual("125678",   Sql.Stuff("1234",        3,    5,    "5678"));
		}

		[Test]
		public void Stuff2()
		{
			var expression = Enumerable.Empty<string>();
			Assert.Throws<NotImplementedException>(() => Sql.Stuff(expression, 1, 1, "")); // ServerSideOnly
		}

		[Test]
		public void Space()
		{
			Assert.AreEqual(null, Sql.Space(null));
			Assert.AreEqual(null, Sql.Space(-1));
			Assert.AreEqual("",   Sql.Space(0));
			Assert.AreEqual(" ",  Sql.Space(1));
		}

		[Test]
		public void PadLeft()
		{
			Assert.AreEqual(null,     Sql.PadLeft(null,   1,    '.'));
			Assert.AreEqual(null,     Sql.PadLeft("test", null, '.'));
			Assert.AreEqual(null,     Sql.PadLeft("test", 1,    null));

			Assert.AreEqual(null,     Sql.PadLeft("test", -1,   '.'));
			Assert.AreEqual("",       Sql.PadLeft("test", 0,    '.'));
			Assert.AreEqual("tes",    Sql.PadLeft("test", 3,    '.'));
			Assert.AreEqual("test",   Sql.PadLeft("test", 4,    '.'));
			Assert.AreEqual(".test",  Sql.PadLeft("test", 5,    '.'));
		}

		[Test]
		public void PadRight()
		{
			Assert.AreEqual(null,     Sql.PadRight(null,   1,    '.'));
			Assert.AreEqual(null,     Sql.PadRight("test", null, '.'));
			Assert.AreEqual(null,     Sql.PadRight("test", 1,    null));

			Assert.AreEqual(null,     Sql.PadRight("test", -1,   '.'));
			Assert.AreEqual("",       Sql.PadRight("test", 0,    '.'));
			Assert.AreEqual("tes",    Sql.PadRight("test", 3,    '.'));
			Assert.AreEqual("test",   Sql.PadRight("test", 4,    '.'));
			Assert.AreEqual("test.",  Sql.PadRight("test", 5,    '.'));
		}

		[Test]
		public void Replace1()
		{
			Assert.AreEqual(null,    Sql.Replace(null,   "e",  "oa"));
			Assert.AreEqual(null,    Sql.Replace("test", null, "oa"));
			Assert.AreEqual(null,    Sql.Replace("test", "e",  null));

			Assert.AreEqual("",      Sql.Replace("",     "e",  "oa"));
			Assert.AreEqual("test",  Sql.Replace("test", "",   "oa"));
			Assert.AreEqual("test",  Sql.Replace("test", "g",  "oa"));
			Assert.AreEqual("toast", Sql.Replace("test", "e",  "oa"));
		}

		[Test]
		public void Replace2()
		{
			Assert.AreEqual(null,    Sql.Replace(null,   'e',  'o'));
			Assert.AreEqual(null,    Sql.Replace("test", null, 'o'));
			Assert.AreEqual(null,    Sql.Replace("test", 'e',  null));

			Assert.AreEqual("",      Sql.Replace("",     'e',  'o'));
			Assert.AreEqual("test",  Sql.Replace("test", 'g',  'o'));
			Assert.AreEqual("tost",  Sql.Replace("test", 'e',  'o'));
		}

		#endregion

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
				db.Person.Count(p => p.FirstName.Contains("jOh") && p.ID == 1).Should().Be(1);
				db.Person.Count(p => !p.FirstName.Contains("jOh") && p.ID == 1).Should().Be(0);
			}
		}

#if NETSTANDARD2_1PLUS
		[Test]
		public void ContainsConstantWithCase1([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			{
				//db.Person.Count(p =>  p.FirstName.Contains("Joh", StringComparison.Ordinal) && p.ID == 1).Should().Be(1);
				db.Person.Count(p => !p.FirstName.Contains("Joh", StringComparison.Ordinal) && p.ID == 1).Should().Be(0);

				// db.Person.Count(p =>  p.FirstName.Contains("joh", StringComparison.Ordinal) && p.ID == 1).Should().Be(0);
				// db.Person.Count(p => !p.FirstName.Contains("joh", StringComparison.Ordinal) && p.ID == 1).Should().Be(1);
			}
		}
#endif
		[Test]
		public void ContainsConstantWithCase2([DataSources(ProviderName.SqlCe)] string context)
		{
			using (new CaseSensitiveStringSearch())
			using (var db = GetDataContext(context))
			{
				//db.Person.Count(p =>  p.FirstName.Contains("Joh") && p.ID == 1).Should().Be(1);
				db.Person.Count(p => !p.FirstName.Contains("Joh") && p.ID == 1).Should().Be(0);

				// db.Person.Count(p =>  p.FirstName.Contains("joh") && p.ID == 1).Should().Be(0);
				// db.Person.Count(p => !p.FirstName.Contains("joh") && p.ID == 1).Should().Be(1);
			}
		}


		[Test]
		public void ContainsConstant2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.FirstName.Contains("o%h") && p.ID == 1).Should().Be(0);
				db.Person.Count(p => !p.FirstName.Contains("o%h") && p.ID == 1).Should().Be(1);
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
		public void ContainsConstant4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var s = "123[456";

				db.Person.Count(p => p.ID == 1 && s.Contains("[")).Should().Be(1);
				db.Person.Count(p => p.ID == 1 && !s.Contains("[")).Should().Be(0);
			}
		}

		[Test]
		public void ContainsConstant5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.ID == 1 && "123[456".Contains("[")).Should().Be(1);
			}
		}

		[Test]
		public void ContainsConstant41([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var s  = "123[456";
				var ps = "[";

				db.Person.Count(p => p.ID == 1 && s.Contains(ps)).Should().Be(1);
			}
		}

		[Test]
		public void ContainsValueAll([DataSources] string context,
			[Values("n", "-", "*", "?", "#", "%", "[", "]", "[]", "[[", "]]")]string toTest)
		{
			using (var db = GetDataContext(context))
			{
				var s  = "123" + toTest + "456";

				db.Person.Count(p => p.ID == 1 && s.Contains(Sql.ToSql(toTest))).Should().Be(1);
				db.Person.Count(p => p.ID == 1 && !s.Contains(Sql.ToSql(toTest))).Should().Be(0);
			}
		}


		[Test]
		public void ContainsParameterAll([DataSources] string context,
			[Values("n", "-", "*", "?", "#", "%", "[", "]", "[]", "[[", "]]")]string toTest)
		{
			using (var db = GetDataContext(context))
			{
				var s  = "123" + toTest + "456";

				db.Person.Count(p => p.ID == 1 && s.Contains(toTest)).Should().Be(1);
			}
		}

		[Test]
		public void ContainsConstant51([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ps = "[";

				db.Person.Count(p => p.ID == 1 && "123[456".Contains(ps)).Should().Be(1);
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
		public void ContainsNull([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				string? firstName = null;
				int?    id        = null;

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
		public void StartsWithCacheCheck([DataSources] string context, [Values(StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal, StringComparison.InvariantCultureIgnoreCase, StringComparison.InvariantCulture)] StringComparison comparison)
		{
			using (var db = GetDataContext(context))
			{
				var firstName = db.Person.Where(p => p.ID == 1).Select(p => p.FirstName).Single();
				var nameToCheck = firstName.Substring(0, 3);
				switch (comparison)
				{
					case StringComparison.OrdinalIgnoreCase : 
					case StringComparison.InvariantCultureIgnoreCase : 
					case StringComparison.CurrentCultureIgnoreCase : 
						nameToCheck = nameToCheck.ToUpper();
						break;
				}

				db.Person.Count(p => p.FirstName.StartsWith(nameToCheck, comparison)  && p.ID == 1).Should().Be(1);
				db.Person.Count(p => !p.FirstName.StartsWith(nameToCheck, comparison) && p.ID == 1).Should().Be(0);

				switch (comparison)
				{
					case StringComparison.Ordinal : 
					case StringComparison.CurrentCulture : 
					case StringComparison.InvariantCulture : 
					{
						nameToCheck = firstName.Substring(0, 3);
						nameToCheck = nameToCheck.ToUpper();

						db.Person.Count(p => p.FirstName.StartsWith(nameToCheck, comparison)  && p.ID == 1).Should().Be(0);
						db.Person.Count(p => !p.FirstName.StartsWith(nameToCheck, comparison) && p.ID == 1).Should().Be(1);

						break;
					}
				}

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
		public void StartsWith1IgnoreCase([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.FirstName.StartsWith("joH", StringComparison.OrdinalIgnoreCase) && p.ID == 1).Should().Be(1);
				db.Person.Count(p => !p.FirstName.StartsWith("joH", StringComparison.OrdinalIgnoreCase) && p.ID == 1).Should().Be(0);
			}
		}

		[Test]
		public void StartsWith1Case([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.FirstName.StartsWith("Jo", StringComparison.Ordinal) && p.ID == 1).Should().Be(1);
				db.Person.Count(p => p.FirstName.StartsWith("jo", StringComparison.Ordinal) && p.ID == 1).Should().Be(0);

				db.Person.Count(p => !p.FirstName.StartsWith("Jo", StringComparison.Ordinal) && p.ID == 1).Should().Be(0);
			}
		}

		[Test]
		public void StartsWith2([DataSources(ProviderName.DB2, TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where "John123".StartsWith(p.FirstName) select p,
					from p in db.Person where "John123".StartsWith(p.FirstName) select p);
		}

		[Test]
		public void StartsWith3([DataSources(ProviderName.DB2, TestProvName.AllAccess)] string context)
		{
			var str = "John123";

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where str.StartsWith(p.FirstName) select p,
					from p in db.Person where str.StartsWith(p.FirstName) select p);
		}

		[Test]
		public void StartsWith4([DataSources(ProviderName.DB2, TestProvName.AllAccess)] string context)
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
		public void StartsWith5([DataSources(ProviderName.DB2, TestProvName.AllAccess)] string context)
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
		public void EndsWithIgnoreCase([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.FirstName.EndsWith("JOHN") && p.ID == 1).Should().Be(1);
				db.Person.Count(p => !p.FirstName.EndsWith("JOHN") && p.ID == 1).Should().Be(0);
			}
		}

		[Test]
		public void EndsWithWithCase([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Patient.Count(p =>  p.Diagnosis.EndsWith("Persecution", StringComparison.Ordinal) && p.PersonID == 2).Should().Be(1);
				db.Patient.Count(p => !p.Diagnosis.EndsWith("Persecution", StringComparison.Ordinal) && p.PersonID == 2).Should().Be(0);

				db.Patient.Count(p =>  p.Diagnosis.EndsWith("persecution", StringComparison.Ordinal) && p.PersonID == 2).Should().Be(0);
				db.Patient.Count(p => !p.Diagnosis.EndsWith("persecution", StringComparison.Ordinal) && p.PersonID == 2).Should().Be(1);
			}
		}

#if NET472
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
		public void Like23([DataSources] string context)
		{
			var pattern = @"%h~%n%";
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !Sql.Like(p.FirstName, pattern, '~') && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void IndexOf11([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.IndexOf("oh") == 1 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void IndexOf12([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.IndexOf("") == 0 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void IndexOf2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.LastName.IndexOf("e", 2) == 4 && p.ID == 2 select p;
				Assert.AreEqual(2, q.ToList().First().ID);
			}
		}

		[ActiveIssue(Details = "Sql.CharIndex(string, string, int) have incorrect SQL logic for all providers (except HANA)")]
		[Test]
		public void IndexOf3([DataSources(
			ProviderName.DB2, TestProvName.AllFirebird,
			ProviderName.SqlCe, TestProvName.AllAccess, ProviderName.SQLiteMS)]
			string context)
		{
			var s = "e";
			var n1 = 2;
			var n2 = 5;

			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.LastName.IndexOf(s, n1, n2) == 4 && p.ID == 2 select p;
				Assert.AreEqual(2, q.ToList().First().ID);
			}
		}

		[Test]
		public void LastIndexOf1([DataSources(
			ProviderName.DB2,
			ProviderName.SqlCe, TestProvName.AllAccess, TestProvName.AllSapHana, ProviderName.SQLiteMS)]
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
			ProviderName.DB2, ProviderName.SqlCe,
			TestProvName.AllAccess, TestProvName.AllSapHana, ProviderName.SQLiteMS)]
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
			ProviderName.DB2, ProviderName.SqlCe,
			TestProvName.AllAccess, TestProvName.AllSapHana, ProviderName.SQLiteMS)]
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
		public void CharIndex1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.CharIndex("oh", p.FirstName) == 2 && p.ID == 1 select p;
				Assert.AreEqual(1, q.ToList().First().ID);
			}
		}

		[Test]
		public void CharIndex2([DataSources(ProviderName.SQLiteMS)] string context)
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
			ProviderName.DB2, ProviderName.SqlCe,
			TestProvName.AllAccess, TestProvName.AllSapHana, ProviderName.SQLiteMS)]
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

		class Category
		{
			[PrimaryKey, Identity] public int     Id;
			[Column, NotNull]      public string? Name;
		}

		class Task
		{
			[PrimaryKey, Identity] public int     Id;
			[Column, NotNull]      public string? Name;
		}

		class TaskCategory
		{
			[Column, NotNull] public int Id;
			[Column, NotNull] public int TaskId;
			[Column, NotNull] public int CategoryId;
		}

		[Test]
		public void Stuff2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				TestContext.WriteLine(q.ToString());
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
		public void Replace([DataSources(TestProvName.AllAccess)] string context)
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
				var q = from p in db.Person where string.Compare(p.FirstName, 1, "Joh", 1, 2, true) == 0 && p.ID == 1 select p;
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

		[Table]
		class CollatedTable
		{
			[Column, PrimaryKey] public int    Id              { get; set; }
			[Column            ] public string CaseSensitive   { get; set; } = null!;
			[Column            ] public string CaseInsensitive { get; set; } = null!;

			public static readonly CollatedTable TestData = new () { Id = 1, CaseSensitive = "TestString", CaseInsensitive = "TestString" };
		}

#if NETSTANDARD2_1PLUS
		[Test]
		public void ExplicitOrdinalIgnoreCase_Contains([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.Contains("stSt", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.Contains("stSt", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.Contains("stst", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.Contains("stst", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
			}
		}
#endif

		[Test]
		public void ExplicitOrdinalIgnoreCase_StartsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.StartsWith("TestSt", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.StartsWith("TestSt", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.StartsWith("testst", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.StartsWith("testst", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
			}
		}

		[Test]
		public void ExplicitOrdinalIgnoreCase_EdnsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.EndsWith("stString", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.EndsWith("stString", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.EndsWith("ststring", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.EndsWith("ststring", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
			}
		}
	}
}
