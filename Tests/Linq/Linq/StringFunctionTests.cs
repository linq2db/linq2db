using System;
#if NETFRAMEWORK
using System.Data.Linq.SqlClient;
#else
using System.Data;
#endif

using System.Linq;
using System.Linq.Dynamic.Core;

using Shouldly;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class StringFunctionTests : TestBase
	{
		#region Non-Database tests

		[Test]
		public void Length()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.Length((string)null!), Is.Null);
				Assert.That(Sql.Length(string.Empty), Is.Zero);
				Assert.That(Sql.Length("test"), Is.EqualTo(4));
			}
		}

		[Test]
		public void Substring()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.Substring(null, 0, 0), Is.Null);
				Assert.That(Sql.Substring("test", null, 0), Is.Null);
				Assert.That(Sql.Substring("test", -1, 0), Is.Null);
				Assert.That(Sql.Substring("test", 5, 0), Is.Null);
				Assert.That(Sql.Substring("test", 0, null), Is.Null);
				Assert.That(Sql.Substring("test", 0, -1), Is.Null);

				Assert.That(Sql.Substring("test", 3, 0), Is.EqualTo(""));
				Assert.That(Sql.Substring("test", 3, 1), Is.EqualTo("s"));
				Assert.That(Sql.Substring("test", 3, 2), Is.EqualTo("st"));
				Assert.That(Sql.Substring("test", 3, 3), Is.EqualTo("st"));
			}
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.CharIndex("", null), Is.Null);
				Assert.That(Sql.CharIndex((string)null!, "test"), Is.Null);

				Assert.That(Sql.CharIndex("", "test"), Is.Zero);
				Assert.That(Sql.CharIndex("g", "test"), Is.Zero);
				Assert.That(Sql.CharIndex("st", "test"), Is.EqualTo(3));
			}
		}

		[Test]
		public void CharIndex2()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.CharIndex("", null, 0), Is.Null);
				Assert.That(Sql.CharIndex((string)null!, "test", 0), Is.Null);
				Assert.That(Sql.CharIndex("st", "test", null), Is.Null);

				Assert.That(Sql.CharIndex("", "test", 0), Is.Zero);
				Assert.That(Sql.CharIndex("g", "test", 0), Is.Zero);
				Assert.That(Sql.CharIndex("st", "test", -1), Is.EqualTo(3));
				Assert.That(Sql.CharIndex("st", "test", 2), Is.EqualTo(3));
				Assert.That(Sql.CharIndex("st", "test", 4), Is.Zero);
				Assert.That(Sql.CharIndex("st", "test", 5), Is.Zero);
			}
		}

		[Test]
		public void CharIndex3()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.CharIndex('t', null), Is.Null);
				Assert.That(Sql.CharIndex((char?)null!, "test"), Is.Null);

				Assert.That(Sql.CharIndex(Char.MinValue, "test"), Is.Zero);
				Assert.That(Sql.CharIndex('g', "test"), Is.Zero);
				Assert.That(Sql.CharIndex('s', "test"), Is.EqualTo(3));
			}
		}

		[Test]
		public void CharIndex4()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.CharIndex('t', null, 0), Is.Null);
				Assert.That(Sql.CharIndex((char?)null!, "test", 0), Is.Null);
				Assert.That(Sql.CharIndex('t', "test", null), Is.Null);

				Assert.That(Sql.CharIndex(Char.MinValue, "test", 0), Is.Zero);
				Assert.That(Sql.CharIndex('g', "test", 0), Is.Zero);
				Assert.That(Sql.CharIndex('s', "test", -1), Is.EqualTo(3));
				Assert.That(Sql.CharIndex('s', "test", 2), Is.EqualTo(3));
				Assert.That(Sql.CharIndex('s', "test", 4), Is.Zero);
				Assert.That(Sql.CharIndex('s', "test", 5), Is.Zero);
			}
		}

		[Test]
		public void Reverse()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.Reverse(null), Is.Null);
				Assert.That(Sql.Reverse(string.Empty), Is.EqualTo(string.Empty));
				Assert.That(Sql.Reverse("abcd"), Is.EqualTo("dcba"));
			}
		}

		[Test]
		public void Left()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.Left(null, 0), Is.Null);
				Assert.That(Sql.Left("test", null), Is.Null);
				Assert.That(Sql.Left("test", -1), Is.Null);
				Assert.That(Sql.Left("test", 0), Is.EqualTo(""));
				Assert.That(Sql.Left("test", 2), Is.EqualTo("te"));
				Assert.That(Sql.Left("test", 5), Is.EqualTo("test"));
			}
		}

		[Test]
		public void Right()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.Right(null, 0), Is.Null);
				Assert.That(Sql.Right("test", null), Is.Null);
				Assert.That(Sql.Right("test", -1), Is.Null);
				Assert.That(Sql.Right("test", 0), Is.EqualTo(""));
				Assert.That(Sql.Right("test", 2), Is.EqualTo("st"));
				Assert.That(Sql.Right("test", 5), Is.EqualTo("test"));
			}
		}

		[Test]
		public void Stuff1()
		{
			using (Assert.EnterMultipleScope())
			{
				// Disallowed null parameters
				Assert.That(Sql.Stuff((string)null!, 1, 1, "test"), Is.Null);
				Assert.That(Sql.Stuff("test", null, 1, "test"), Is.Null);
				Assert.That(Sql.Stuff("test", 1, null, "test"), Is.Null);
				Assert.That(Sql.Stuff("test", 1, 1, null), Is.Null);

				// Disallowed start
				Assert.That(Sql.Stuff("test", 0, 1, "test"), Is.Null);
				Assert.That(Sql.Stuff("test", 5, 1, "test"), Is.Null);

				// Disallowed length
				Assert.That(Sql.Stuff("test", 1, -1, "test"), Is.Null);

				// Correct start and length
				Assert.That(Sql.Stuff("1234", 1, 4, "5678"), Is.EqualTo("5678"));

				// Correct start
				Assert.That(Sql.Stuff("1234", 4, 0, "5678"), Is.EqualTo("12356784"));

				// Correct length
				Assert.That(Sql.Stuff("1234", 3, 5, "5678"), Is.EqualTo("125678"));
			}
		}

		[Test]
		public void Stuff2Fail()
		{
			var expression = Enumerable.Empty<string>();
			Assert.Throws<NotImplementedException>(() => Sql.Stuff(expression, 1, 1, "")); // ServerSideOnly
		}

		[Test]
		public void Space()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.Space(null), Is.Null);
				Assert.That(Sql.Space(-1), Is.Null);
				Assert.That(Sql.Space(0), Is.EqualTo(""));
				Assert.That(Sql.Space(1), Is.EqualTo(" "));
			}
		}

		[Test]
		public void PadLeft()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.PadLeft(null, 1, '.'), Is.Null);
				Assert.That(Sql.PadLeft("test", null, '.'), Is.Null);
				Assert.That(Sql.PadLeft("test", 1, null), Is.Null);

				Assert.That(Sql.PadLeft("test", -1, '.'), Is.Null);
				Assert.That(Sql.PadLeft("test", 0, '.'), Is.EqualTo(""));
				Assert.That(Sql.PadLeft("test", 3, '.'), Is.EqualTo("tes"));
				Assert.That(Sql.PadLeft("test", 4, '.'), Is.EqualTo("test"));
				Assert.That(Sql.PadLeft("test", 5, '.'), Is.EqualTo(".test"));
			}
		}

		[Test]
		public void PadRight()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.PadRight(null, 1, '.'), Is.Null);
				Assert.That(Sql.PadRight("test", null, '.'), Is.Null);
				Assert.That(Sql.PadRight("test", 1, null), Is.Null);

				Assert.That(Sql.PadRight("test", -1, '.'), Is.Null);
				Assert.That(Sql.PadRight("test", 0, '.'), Is.EqualTo(""));
				Assert.That(Sql.PadRight("test", 3, '.'), Is.EqualTo("tes"));
				Assert.That(Sql.PadRight("test", 4, '.'), Is.EqualTo("test"));
				Assert.That(Sql.PadRight("test", 5, '.'), Is.EqualTo("test."));
			}
		}

		[Test]
		public void Replace1()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.Replace(null, "e", "oa"), Is.Null);
				Assert.That(Sql.Replace("test", null, "oa"), Is.Null);
				Assert.That(Sql.Replace("test", "e", null), Is.Null);

				Assert.That(Sql.Replace("", "e", "oa"), Is.EqualTo(""));
				Assert.That(Sql.Replace("test", "", "oa"), Is.EqualTo("test"));
				Assert.That(Sql.Replace("test", "g", "oa"), Is.EqualTo("test"));
				Assert.That(Sql.Replace("test", "e", "oa"), Is.EqualTo("toast"));
			}
		}

		[Test]
		public void Replace2()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Sql.Replace(null, 'e', 'o'), Is.Null);
				Assert.That(Sql.Replace("test", null, 'o'), Is.Null);
				Assert.That(Sql.Replace("test", 'e', null), Is.Null);

				Assert.That(Sql.Replace("", 'e', 'o'), Is.EqualTo(""));
				Assert.That(Sql.Replace("test", 'g', 'o'), Is.EqualTo("test"));
				Assert.That(Sql.Replace("test", 'e', 'o'), Is.EqualTo("tost"));
			}
		}

		#endregion

		[Test]
		public void Length([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Length == "John".Length && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		class TestLengthModel
		{
			[Column] public int    Id  { get; set; }
			[Column] public string Str { get; set; } = string.Empty;
		}

		[Test]
		public void LengthWhiteSpace([DataSources(TestProvName.AllSybase)] string context, [Values("abc ", " ", " abc ")] string stringValue)
		{
			var data = new[] { new TestLengthModel { Str = stringValue } };

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var result = table.Select(t =>
				new 
				{
					Str = t.Str,
					Len = t.Str.Length,
				}).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Str, Is.EqualTo(stringValue));
				Assert.That(result.Len, Is.EqualTo(stringValue.Length));
			}
		}		
		

		static string CorrectValue(string value)
		{
			return value.Trim();
		}

		[Test]
		public void LengthFromNonTranslatable([DataSources(TestProvName.AllSybase)] string context, [Values("abc ", " ", " abc ")] string stringValue)
		{
			var data = new[] { new TestLengthModel { Str = stringValue } };

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				let str = CorrectValue(t.Str)
				select new
				{
					OldStr = t.Str,
					Str = str,
					IsChanged = str.Length != t.Str.Length,
					Condition = str.Length < t.Str.Length ? "corrected-" + str : "original-" + t.Str
				};

			AssertQuery(query);
		}

		[Test]
		public void ContainsConstant([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.FirstName.Contains("jOh") && p.ID == 1).ShouldBe(IsCaseSensitiveComparison(context) ? 0 : 1);
				db.Person.Count(p => !p.FirstName.Contains("jOh") && p.ID == 1).ShouldBe(IsCaseSensitiveComparison(context) ? 1 : 0);
			}
		}

#if NET8_0_OR_GREATER
		[Test]
		public void ContainsConstantWithCase1([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			{
				//db.Person.Count(p =>  p.FirstName.Contains("Joh", StringComparison.Ordinal) && p.ID == 1).ShouldBe(1);
				db.Person.Count(p => !p.FirstName.Contains("Joh", StringComparison.Ordinal) && p.ID == 1).ShouldBe(0);

				// db.Person.Count(p =>  p.FirstName.Contains("joh", StringComparison.Ordinal) && p.ID == 1).ShouldBe(0);
				// db.Person.Count(p => !p.FirstName.Contains("joh", StringComparison.Ordinal) && p.ID == 1).ShouldBe(1);
			}
		}
#endif
		[Test]
		public void ContainsConstantWithCase2([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.FirstName.Contains("Joh") && p.ID == 1).ShouldBe(1);
				db.Person.Count(p => !p.FirstName.Contains("Joh") && p.ID == 1).ShouldBe(0);
			}
		}

		[Test]
		public void ContainsConstant2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.FirstName.Contains("o%h") && p.ID == 1).ShouldBe(0);
				db.Person.Count(p => !p.FirstName.Contains("o%h") && p.ID == 1).ShouldBe(1);
			}
		}

		[Test]
		public void ContainsConstant3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var arr = new[] { "oh", "oh'", "oh\\" };

				var q = from p in db.Person where  arr.Contains(p.FirstName) select p;
				Assert.That(q.Count(), Is.Zero);
			}
		}

		[Test]
		public void ContainsConstant4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var s = "123[456";

				db.Person.Count(p => p.ID == 1 && s.Contains("[")).ShouldBe(1);
				db.Person.Count(p => p.ID == 1 && !s.Contains("[")).ShouldBe(0);
			}
		}

		[Test]
		public void ContainsConstant5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.ID == 1 && "123[456".Contains("[")).ShouldBe(1);
			}
		}

		[Test]
		public void ContainsConstant41([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var s  = "123[456";
				var ps = "[";

				db.Person.Count(p => p.ID == 1 && s.Contains(ps)).ShouldBe(1);
			}
		}

		[Test]
		public void ContainsValueAll([DataSources] string context,
			[Values("n", "-", "*", "?", "#", "%", "[", "]", "[]", "[[", "]]")] string toTest)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = true;

			var s  = "123" + toTest + "456";

			db.Person.Count(p => p.ID == 1 && s.Contains(Sql.ToSql(toTest))).ShouldBe(1);
			db.Person.Count(p => p.ID == 1 && !s.Contains(Sql.ToSql(toTest))).ShouldBe(0);
		}

		[Test]
		public void ContainsParameterAll([DataSources] string context,
			[Values("n", "-", "*", "?", "#", "%", "[", "]", "[]", "[[", "]]")]string toTest)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = false;

			var s  = "123" + toTest + "456";

			db.Person.Count(p => p.ID == 1 && s.Contains(toTest)).ShouldBe(1);
		}

		[Test]
		public void ContainsConstant51([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ps = "[";

				db.Person.Count(p => p.ID == 1 && "123[456".Contains(ps)).ShouldBe(1);
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r.p.ID, Is.EqualTo(1));
					Assert.That(r.str, Is.EqualTo(str));
				}
			}
		}

		[Test]
		public void ContainsParameter2([DataSources] string context)
		{
			var str = "o%h";

			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !p.FirstName.Contains(str) && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
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

				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
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
		public void ContainsNull([DataSources] string context)
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
						nameToCheck = nameToCheck.ToUpperInvariant();
						break;
				}

				db.Person.Count(p =>  p.FirstName.StartsWith(nameToCheck, comparison) && p.ID == 1).ShouldBe(1);
				db.Person.Count(p => !p.FirstName.StartsWith(nameToCheck, comparison) && p.ID == 1).ShouldBe(0);

				switch (comparison)
				{
					case StringComparison.Ordinal :
					case StringComparison.CurrentCulture :
					case StringComparison.InvariantCulture :
					{
						nameToCheck = firstName.Substring(0, 3);
						nameToCheck = nameToCheck.ToUpperInvariant();

						db.Person.Count(p =>  p.FirstName.StartsWith(nameToCheck, comparison) && p.ID == 1).ShouldBe(0);
						db.Person.Count(p => !p.FirstName.StartsWith(nameToCheck, comparison) && p.ID == 1).ShouldBe(1);

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
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void StartsWith1IgnoreCase([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p =>  p.FirstName.StartsWith("joH", StringComparison.OrdinalIgnoreCase) && p.ID == 1).ShouldBe(1);
				db.Person.Count(p => !p.FirstName.StartsWith("joH", StringComparison.OrdinalIgnoreCase) && p.ID == 1).ShouldBe(0);
			}
		}

		[Test]
		public void StartsWith1Case([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Count(p => p.FirstName.StartsWith("Jo", StringComparison.Ordinal) && p.ID == 1).ShouldBe(1);
				db.Person.Count(p => p.FirstName.StartsWith("jo", StringComparison.Ordinal) && p.ID == 1).ShouldBe(0);

				db.Person.Count(p => !p.FirstName.StartsWith("Jo", StringComparison.Ordinal) && p.ID == 1).ShouldBe(0);
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
				db.Person.Count(p =>  p.FirstName.EndsWith("JOHN") && p.ID == 1).ShouldBe(IsCaseSensitiveComparison(context) ? 0 : 1);
				db.Person.Count(p => !p.FirstName.EndsWith("JOHN") && p.ID == 1).ShouldBe(IsCaseSensitiveComparison(context) ? 1 : 0);
			}
		}

		[Table]
		sealed class StringTypesTable
		{
			[Column]                                                              public int    Id             { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.Char)]     public string CharColumn     { get; set; } = null!;
			[Column(Length = 50, CanBeNull = true, DataType = DataType.NChar)]    public string NCharColumn    { get; set; } = null!;
			[Column(Length = 50, CanBeNull = true, DataType = DataType.VarChar)]  public string VarCharColumn  { get; set; } = null!;
			[Column(Length = 50, CanBeNull = true, DataType = DataType.NVarChar)] public string NVarCharColumn { get; set; } = null!;
		}

		[Test]
		public void StartsWithDataType1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var str   = "some";
				var table = db.GetTable<StringTypesTable>();
				var sqlExpr = table.Where(t => t.VarCharColumn.StartsWith(str)).GetSelectQuery()
					.Find(e => e.ElementType == QueryElementType.SqlParameter);

				sqlExpr.ShouldNotBeNull();

				var param = (SqlParameter)sqlExpr!;

				param.Type.DataType.ShouldBe(DataType.VarChar);
			}
		}

		[Test]
		public void StartsWithDataType2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var str   = "some";
				var table = db.GetTable<StringTypesTable>();
				var sqlExpr = table.Where(t => t.NVarCharColumn.StartsWith(str)).GetSelectQuery()
					.Find(e => e.ElementType == QueryElementType.SqlParameter);

				sqlExpr.ShouldNotBeNull();

				var param = (SqlParameter)sqlExpr!;

				param.Type.DataType.ShouldBe(DataType.NVarChar);
			}
		}

		[Test]
		public void StartsWithDataType3([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var str   = "some";
				var table = db.GetTable<StringTypesTable>();
				var sqlExpr = table.Where(t => str.StartsWith(t.NVarCharColumn)).GetSelectQuery()
					.Find(e => e.ElementType == QueryElementType.SqlParameter);

				sqlExpr.ShouldNotBeNull();

				var param = (SqlParameter)sqlExpr!;

				param.Type.DataType.ShouldBe(DataType.NVarChar);
			}
		}

		[Test]
		public void LikeWithDataType1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var str   = "some";
				var table = db.GetTable<StringTypesTable>();
				var sqlExpr = table.Where(t => Sql.Like(t.VarCharColumn, str)).GetSelectQuery()
					.Find(e => e.ElementType == QueryElementType.SqlParameter);

				sqlExpr.ShouldNotBeNull();

				var param = (SqlParameter)sqlExpr!;

				param.Type.DataType.ShouldBe(DataType.VarChar);
			}
		}

		[Test]
		public void LikeWithDataType2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var str   = "some";
				var table = db.GetTable<StringTypesTable>();
				var sqlExpr = table.Where(t => Sql.Like(t.NVarCharColumn, str)).GetSelectQuery()
					.Find(e => e.ElementType == QueryElementType.SqlParameter);

				sqlExpr.ShouldNotBeNull();

				var param = (SqlParameter)sqlExpr!;

				param.Type.DataType.ShouldBe(DataType.NVarChar);
			}
		}

		[Test]
		public void LikeWithDataType3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var str   = "some";
				var table = db.GetTable<StringTypesTable>();
				var sqlExpr = table.Where(t => Sql.Like(str, t.NVarCharColumn)).GetSelectQuery()
					.Find(e => e.ElementType == QueryElementType.SqlParameter);

				sqlExpr.ShouldNotBeNull();

				var param = (SqlParameter)sqlExpr!;

				param.Type.DataType.ShouldBe(DataType.NVarChar);
			}
		}

		[Test]
		public void StartWithByTypes([DataSources] string context)
		{
			var dataStr = "someString";
			var data = new StringTypesTable[]
			{
				new()
				{
					Id             = 1,
					CharColumn     = dataStr,
					NCharColumn    = dataStr,
					NVarCharColumn = dataStr,
					VarCharColumn  = dataStr,
				}
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var str   = "some";

				var result = table.Where(t =>
					t.CharColumn.StartsWith(str)    &&
					t.NCharColumn.StartsWith(str)   &&
						t.VarCharColumn.StartsWith(str) &&
						t.NVarCharColumn.StartsWith(str)
					);

				result.ToList().Count.ShouldBe(1);
			}
		}

		[Test]
		public void EndsWithWithCase([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Patient.Count(p =>  p.Diagnosis.EndsWith("Persecution", StringComparison.Ordinal) && p.PersonID == 2).ShouldBe(1);
				db.Patient.Count(p => !p.Diagnosis.EndsWith("Persecution", StringComparison.Ordinal) && p.PersonID == 2).ShouldBe(0);

				db.Patient.Count(p =>  p.Diagnosis.EndsWith("persecution", StringComparison.Ordinal) && p.PersonID == 2).ShouldBe(0);
				db.Patient.Count(p => !p.Diagnosis.EndsWith("persecution", StringComparison.Ordinal) && p.PersonID == 2).ShouldBe(1);
			}
		}

#if NETFRAMEWORK
		[Test]
		public void Like11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where SqlMethods.Like(p.FirstName, "%hn%") && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Like12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !SqlMethods.Like(p.FirstName, @"%h~%n%", '~') && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}
#endif

		[Test]
		public void Like21([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Like(p.FirstName, "%hn%") && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Like22([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !Sql.Like(p.FirstName, @"%h~%n%", '~') && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Like23([DataSources] string context)
		{
			var pattern = @"%h~%n%";
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !Sql.Like(p.FirstName, pattern, '~') && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void IndexOf11([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.IndexOf("oh") == 1 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void IndexOf12([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.IndexOf("") == 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void IndexOf2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.LastName.IndexOf("e", 2) == 4 && p.ID == 2 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(2));
			}
		}

		[ActiveIssue(Details = "Sql.CharIndex(string, string, int) have incorrect SQL logic for all providers (except HANA)",
			Configurations =
			[
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase,
				TestProvName.AllSQLiteClassic,
			])]
		[Test]
		public void IndexOf3([DataSources(
			ProviderName.DB2, TestProvName.AllFirebird,
			ProviderName.SqlCe, TestProvName.AllAccess, ProviderName.SQLiteMS)]
			string context)
		{
			var s  = "e";
			var n1 = 2;
			var n2 = 5;

			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.LastName.IndexOf(s, n1, n2) == 4 && p.ID == 2 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(2));
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
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
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
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
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
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CharIndex1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.CharIndex("oh", p.FirstName) == 2 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CharIndex2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.CharIndex("p", p.LastName, 2) == 3 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Left([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Left(p.FirstName, 2) == "Jo" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Right([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Right(p.FirstName, 3) == "ohn" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void RightInSelect([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select Sql.Right(p.FirstName, 3);
				Assert.That(q.ToList().First(), Is.EqualTo("ohn"));
			}
		}

		[Test]
		public void Substring1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Substring(1) == "ohn" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Substring2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Substring(1, 2) == "oh" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
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
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Stuff1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.Stuff(p.FirstName, 3, 1, "123") == "Jo123n" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		sealed class Category
		{
			[PrimaryKey, Identity] public int     Id;
			[Column, NotNull]      public string? Name;
		}

		sealed class Task
		{
			[PrimaryKey, Identity] public int     Id;
			[Column, NotNull]      public string? Name;
		}

		sealed class TaskCategory
		{
			[Column, NotNull] public int Id;
			[Column, NotNull] public int TaskId;
			[Column, NotNull] public int CategoryId;
		}

		[Test]
		public void Stuff2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Task>();
			using var t2 = db.CreateLocalTable<TaskCategory>();
			using var t3 = db.CreateLocalTable<Category>();

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

			_ = q.ToArray();
		}

		[Test]
		public void Insert([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Insert(2, "123") == "Jo123hn" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Remove1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Remove(2) == "Jo" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Remove2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.Remove(1, 2) == "Jn" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Space([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName + Sql.Space(p.ID + 1) + "123" == "John  123" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void PadRight([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where Sql.PadRight(p.FirstName, 6, ' ') + "123" == "John  123" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void PadRight1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.PadRight(6) + "123" == "John  123" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void PadRight2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.PadRight(6, '*') + "123" == "John**123" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void PadLeft([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where "123" + Sql.PadLeft(p.FirstName, 6, ' ') == "123  John" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void PadLeft1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where "123" + p.FirstName.PadLeft(6) == "123  John" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void PadLeft2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where "123" + p.FirstName.PadLeft(6, '*') == "123**John" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4799")]
		public void String_PadLeft_Translation([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.Select(() => Sql.AsSql("test".PadLeft(0, '.'))),  Is.EqualTo("test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadLeft(3, '.'))),  Is.EqualTo("test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadLeft(4, '.'))),  Is.EqualTo("test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadLeft(5, '.'))),  Is.EqualTo(".test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadLeft(6, ' '))),  Is.EqualTo("  test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadLeft(6))),       Is.EqualTo("  test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadLeft(16, '.'))), Is.EqualTo("............test"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4799")]
		public void String_PadLeft_TranslationExpressionArguments([DataSources] string context)
			{
			using var db = GetDataContext(context);

			var query =
				from p in db.Person
				select new
				{
					p.ID,
					FirstName = p.FirstName.PadLeft(p.ID, '.')
				} into s
				where s.FirstName != ""
				select s;

			AssertQuery(query);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4799")]
		public void String_PadRight_Translation([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.Select(() => Sql.AsSql("test".PadRight(0, '.'))), Is.EqualTo("test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadRight(3, '.'))), Is.EqualTo("test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadRight(4, '.'))), Is.EqualTo("test"));
				Assert.That(db.Select(() => Sql.AsSql("test".PadRight(5, '.'))), Is.EqualTo("test."));
				Assert.That(db.Select(() => Sql.AsSql("test".PadRight(6, '.'))), Is.EqualTo("test.."));
			}
		}

		[Test]
		public void Replace([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = 
					from p in db.Person 
					where p.FirstName.Replace("hn", "lie") == "Jolie" && p.ID == 1 
					select p;

				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Trim([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person 
					where p.ID == 1 
					select new { p.ID, Name = "  " + p.FirstName + " " } into pp
					where pp.Name.Trim() == "John" select pp;

				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
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
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
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
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		// for disabled providers see notes on implementation at
		// Expressions.TrimLeft/TrimRight
		[Test]
		public void TrimLeftCharacters([DataSources(
			TestProvName.AllFirebird,
			TestProvName.AllMySql,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllSqlServer2019Minus,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var q =
				from p in db.Person
				where p.ID == 1
				select new { p.ID, Name = "  " + p.FirstName + " " } into pp
				where pp.Name.TrimStart(new char[]{ ' ', 'J' }) == "ohn "
				select pp;
			Assert.That(q.ToList().First().ID, Is.EqualTo(1));
		}

		[Test]
		public void TrimRightCharacters([DataSources(
			TestProvName.AllFirebird,
			TestProvName.AllMySql,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllSqlServer2019Minus,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var q =
				from p in db.Person
				where p.ID == 1
				select new { p.ID, Name = "  " + p.FirstName + " " } into pp
				where pp.Name.TrimEnd(new char[]{ ' ', 'n' }) == "  Joh"
				select pp;
			Assert.That(q.ToList().First().ID, Is.EqualTo(1));
		}

		// for disabled providers see notes on implementation at
		// Expressions.TrimLeft/TrimRight
		[Test]
		public void TrimLeftCharacter([DataSources(
			TestProvName.AllFirebird,
			TestProvName.AllMySql,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllSqlServer2019Minus,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var q =
				from p in db.Person
				where p.ID == 1
				select new { p.ID, Name = "  " + p.FirstName + " " } into pp
				where pp.Name.TrimStart(' ') == "John "
				select pp;
			Assert.That(q.ToList().First().ID, Is.EqualTo(1));
		}

		[Test]
		public void TrimRightCharacter([DataSources(
			TestProvName.AllFirebird,
			TestProvName.AllMySql,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllSqlServer2019Minus,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var q =
				from p in db.Person
				where p.ID == 1
				select new { p.ID, Name = "  " + p.FirstName + " " } into pp
				where pp.Name.TrimEnd(' ') == "  John"
				select pp;
			Assert.That(q.ToList().First().ID, Is.EqualTo(1));
		}

		[Test]
		public void ToLower([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.ToLower() == "john" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void ToLowerParameter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var param = "JOHN";
				var q     = from p in db.Person where p.FirstName.ToLower() == param.ToLower() && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void ToUpper([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.ToUpper() == "JOHN" && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void ToUpperParam([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var param = "john";
				var q     = from p in db.Person where p.FirstName.ToUpper() == param.ToUpper() && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareTo([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("John") == 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareToNotEqual1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Jo") != 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareToNotEqual2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where 0 != p.FirstName.CompareTo("Jo") && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareTo1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Joh") > 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareTo2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Johnn") < 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareTo21([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo("Johnn") <= 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareTo22([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where 0 >= p.FirstName.CompareTo("Johnn") && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareTo3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo(55) > 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareTo31([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.FirstName.CompareTo(55) >= 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareTo32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where 0 <= p.FirstName.CompareTo(55) && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareOrdinal1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.CompareOrdinal(p.FirstName, "Joh") > 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void CompareOrdinal2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.CompareOrdinal(p.FirstName, 1, "Joh", 1, 2) == 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Compare1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.Compare(p.FirstName, "Joh") > 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Compare2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.Compare(p.FirstName, "joh", true) > 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Compare3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.Compare(p.FirstName, 1, "Joh", 1, 2) == 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void Compare4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where string.Compare(p.FirstName, 1, "Joh", 1, 2, true) == 0 && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void IsNullOrEmpty1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where !string.IsNullOrEmpty(p.FirstName) && p.ID == 1 select p;
				Assert.That(q.ToList().First().ID, Is.EqualTo(1));
			}
		}

		[Test]
		public void IsNullOrEmpty2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select string.IsNullOrEmpty(p.FirstName);
				Assert.That(q.ToList().First(), Is.False);
			}
		}

		[Table]
		sealed class CollatedTable
		{
			[Column, PrimaryKey] public int    Id              { get; set; }
			[Column            ] public string CaseSensitive   { get; set; } = null!;
			[Column            ] public string CaseInsensitive { get; set; } = null!;

			public static readonly CollatedTable TestData = new () { Id = 1, CaseSensitive = "TestString", CaseInsensitive = "TestString" };
		}

#if NET8_0_OR_GREATER
		[Test]
		public void ExplicitOrdinalIgnoreCase_Contains([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.Contains("stSt", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.Contains("stSt", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.Contains("stst", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.Contains("stst", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
			}
		}

		[ActiveIssue(3444, Configuration = ProviderName.SqlCe)]
		[Test]
		public void ExplicitOrdinal_Contains([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.Contains("stSt", StringComparison.Ordinal)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.Contains("stSt", StringComparison.Ordinal)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.Contains("stst", StringComparison.Ordinal)).ShouldBe(0);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.Contains("stst", StringComparison.Ordinal)).ShouldBe(0);
			}
		}

		[ActiveIssue(3444, Configuration = ProviderName.SqlCe)]
		[Test]
		public void Explicit_Contains([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Patient
					.Count(r => r.Diagnosis.Contains("Paranoid", StringComparison.Ordinal)).ShouldBe(1);
				db.Patient
					.Count(r => r.Diagnosis.Contains("paranoid", StringComparison.Ordinal)).ShouldBe(0);
				db.Patient
					.Count(r => r.Diagnosis.Contains("paranoid", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.Patient
					.Count(r => r.Diagnosis.Contains("Paranoid", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
			}
		}
#endif

		[Test]
		public void Default_Contains([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.Contains("stSt")).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.Contains("stSt")).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.Contains("stst")).ShouldBe(IsCollatedTableConfigured(context) || IsCaseSensitiveComparison(context) ? 0 : 1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.Contains("stst")).ShouldBe(IsCollatedTableConfigured(context) || !IsCaseSensitiveComparison(context) ? 1 : 0);
			}
		}

		[Test]
		public void ExplicitOrdinalIgnoreCase_StartsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.StartsWith("TestSt", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.StartsWith("TestSt", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.StartsWith("testst", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.StartsWith("testst", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
			}
		}

		[Test]
		public void ExplicitOrdinal_StartsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.StartsWith("TestSt", StringComparison.Ordinal)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.StartsWith("TestSt", StringComparison.Ordinal)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.StartsWith("testst", StringComparison.Ordinal)).ShouldBe(0);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.StartsWith("testst", StringComparison.Ordinal)).ShouldBe(0);
			}
		}

		[Test]
		public void Default_StartsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.StartsWith("TestSt")).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.StartsWith("TestSt")).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.StartsWith("testst")).ShouldBe(IsCollatedTableConfigured(context) || IsCaseSensitiveComparison(context) ? 0 : 1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.StartsWith("testst")).ShouldBe(IsCollatedTableConfigured(context) || !IsCaseSensitiveComparison(context) ? 1 : 0);
			}
		}

		[Test]
		public void Explicit_StartsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Patient
					.Count(r => r.Diagnosis.StartsWith("Hall", StringComparison.Ordinal)).ShouldBe(1);
				db.Patient
					.Count(r => r.Diagnosis.StartsWith("hall", StringComparison.Ordinal)).ShouldBe(0);
				db.Patient
					.Count(r => r.Diagnosis.StartsWith("hall", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.Patient
					.Count(r => r.Diagnosis.StartsWith("Hall", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
			}
		}

		[Test]
		public void ExplicitOrdinalIgnoreCase_EndsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.EndsWith("stString", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.EndsWith("stString", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.EndsWith("ststring", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.EndsWith("ststring", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
			}
		}

		[Test]
		public void ExplicitOrdinal_EndsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.EndsWith("stString", StringComparison.Ordinal)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.EndsWith("stString", StringComparison.Ordinal)).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.EndsWith("ststring", StringComparison.Ordinal)).ShouldBe(0);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.EndsWith("ststring", StringComparison.Ordinal)).ShouldBe(0);
			}
		}

		[Test]
		public void Default_EndsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<CollatedTable>().Delete();
				db.Insert(CollatedTable.TestData);

				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.EndsWith("stString")).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.EndsWith("stString")).ShouldBe(1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseSensitive.EndsWith("ststring")).ShouldBe(IsCollatedTableConfigured(context) || IsCaseSensitiveComparison(context) ? 0 : 1);
				db.GetTable<CollatedTable>()
					.Count(r => r.CaseInsensitive.EndsWith("ststring")).ShouldBe(IsCollatedTableConfigured(context) || !IsCaseSensitiveComparison(context) ? 1 : 0);
			}
		}

		[Test]
		public void Explicit_EndsWith([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Patient
					.Count(r => r.Diagnosis.EndsWith("Persecution", StringComparison.Ordinal)).ShouldBe(1);
				db.Patient
					.Count(r => r.Diagnosis.EndsWith("persecution", StringComparison.Ordinal)).ShouldBe(0);
				db.Patient
					.Count(r => r.Diagnosis.EndsWith("persecution", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
				db.Patient
					.Count(r => r.Diagnosis.EndsWith("Persecution", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
			}
		}

		#region Issue 3002
		public abstract class MySpecialBaseClass : IConvertible, IEquatable<MySpecialBaseClass>
		{
			[NotNull]
			public string Value { get; set; }

			protected MySpecialBaseClass(string value)
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				Value = value;
			}

			public static bool operator ==(MySpecialBaseClass? leftSide, string? rightSide)
				=> leftSide?.Value == rightSide;

			public static bool operator !=(MySpecialBaseClass? leftSide, string? rightSide)
				=> leftSide?.Value != rightSide;

			public override string ToString() => Value;

			public override int GetHashCode() => Value.GetHashCode();

			public override bool Equals(object? obj)
			{
				if (obj == null)
					return false;

				if (obj is string str)
					return Value.Equals(str);

				if (obj.GetType() == GetType())
					return string.Equals(((MySpecialBaseClass)obj).Value, Value);

				return base.Equals(obj);
			}

			public bool Equals(MySpecialBaseClass? other)
			{
				if (other?.GetType() != GetType())
					return false;

				return string.Equals(other.Value, Value);
			}

			#region IConvertible
			public TypeCode GetTypeCode() => TypeCode.String;

			public string ToString(IFormatProvider? provider) => Value;

			public object ToType(Type conversionType, IFormatProvider? provider)
			{
				if (conversionType.IsSubclassOf(typeof(MySpecialBaseClass))
					|| conversionType == typeof(MySpecialBaseClass))
					return this;

				return Value;
			}

			public bool     ToBoolean (IFormatProvider? provider) { throw new NotImplementedException(); }
			public char     ToChar    (IFormatProvider? provider) { throw new NotImplementedException(); }
			public sbyte    ToSByte   (IFormatProvider? provider) { throw new NotImplementedException(); }
			public byte     ToByte    (IFormatProvider? provider) { throw new NotImplementedException(); }
			public short    ToInt16   (IFormatProvider? provider) { throw new NotImplementedException(); }
			public ushort   ToUInt16  (IFormatProvider? provider) { throw new NotImplementedException(); }
			public int      ToInt32   (IFormatProvider? provider) { throw new NotImplementedException(); }
			public uint     ToUInt32  (IFormatProvider? provider) { throw new NotImplementedException(); }
			public long     ToInt64   (IFormatProvider? provider) { throw new NotImplementedException(); }
			public ulong    ToUInt64  (IFormatProvider? provider) { throw new NotImplementedException(); }
			public float    ToSingle  (IFormatProvider? provider) { throw new NotImplementedException(); }
			public double   ToDouble  (IFormatProvider? provider) { throw new NotImplementedException(); }
			public decimal  ToDecimal (IFormatProvider? provider) { throw new NotImplementedException(); }
			public DateTime ToDateTime(IFormatProvider? provider) { throw new NotImplementedException(); }
			#endregion
		}

		public class MyClass : MySpecialBaseClass
		{
			public MyClass(string value)
				: base(value)
			{
			}

			public static implicit operator MyClass?(string? value)
			{
				if (value == null)
					return null;
				return new MyClass(value);
			}

			public static implicit operator string?(MyClass? auswahlliste)
				=> auswahlliste?.Value;
		}

		[Table]
		sealed class SampleClass
		{
			[Column]                                            public int      Id     { get; set; }
			[Column(DataType = DataType.NVarChar, Length = 50)] public MyClass? Value  { get; set; }
			[Column]                                            public string?  Value2 { get; set; }
		}

		[Test]
		public void Issue3002Test([DataSources(
			// providers doesn't support IConvertible parameter coercion
			ProviderName.SQLiteMS,
			ProviderName.DB2,
			TestProvName.AllClickHouse,
			TestProvName.AllMySqlConnector,
			TestProvName.AllPostgreSQL,
			TestProvName.AllInformix,
			TestProvName.AllSybase)] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				table.Insert(() => new SampleClass()
				{
					Id     = 1,
					Value  = "Test",
					Value2 = "SampleClass"
				});
				table.Insert(() => new SampleClass()
				{
					Id     = 2,
					Value  = "Value",
					Value2 = "SomeTest"
				});

				var test = "Test";
				using (Assert.EnterMultipleScope())
				{
					Assert.That(table.Any(sampleClass => sampleClass.Value   == test || sampleClass.Value2!.Contains(test)), Is.True);
					Assert.That(table.Count(sampleClass => sampleClass.Value == test || sampleClass.Value2!.Contains(test)), Is.EqualTo(2));
				}

				test = "Value";
				using (Assert.EnterMultipleScope())
				{
					Assert.That(table.Any(sampleClass => sampleClass.Value   == test || sampleClass.Value2!.Contains(test)), Is.True);
					Assert.That(table.Count(sampleClass => sampleClass.Value == test || sampleClass.Value2!.Contains(test)), Is.EqualTo(1));
				}

				test = "Class";
				using (Assert.EnterMultipleScope())
				{
					Assert.That(table.Any(sampleClass => sampleClass.Value   == test || sampleClass.Value2!.Contains(test)), Is.True);
					Assert.That(table.Count(sampleClass => sampleClass.Value == test || sampleClass.Value2!.Contains(test)), Is.EqualTo(1));
				}
			}
		}
		#endregion
	}
}
