using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using LinqToDB;
using LinqToDB.Reflection;
using LinqToDB.Tools;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Tests.Model;

// as helper assert could be called from anywhere including multi-threading tests, we shouldn't use Assert.Multiple in helpers
// https://github.com/nunit/nunit/issues/4814
#pragma warning disable NUnit2045 // Use Assert.Multiple

namespace Tests
{
	partial class TestBase
	{
#if !NETFRAMEWORK
		public static readonly IEqualityComparer<decimal> DecimalComparerInstance = EqualityComparer<decimal>.Default;
#else
		public static readonly IEqualityComparer<decimal> DecimalComparerInstance = new DecimalComparer();

		sealed class DecimalComparer : IEqualityComparer<decimal>
		{
			bool IEqualityComparer<decimal>.Equals(decimal x, decimal y) => x == y;

			int IEqualityComparer<decimal>.GetHashCode(decimal obj)
			{
				// workaround netfx bug in decimal.GetHashCode
				return (obj / 1.00000000000000000000000000000m).GetHashCode();
			}
		}
#endif

        protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result, bool allowEmpty = false, bool printData = false)
		{
            AreEqual(t => t, expected, result, EqualityComparer<T>.Default, allowEmpty, printData : printData);
		}

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result, Func<IEnumerable<T>, IEnumerable<T>> sort)
		{
			AreEqual(t => t, expected, result, EqualityComparer<T>.Default, sort);
		}

		protected void AreEqualWithComparer<T>(IEnumerable<T> expected, IEnumerable<T> result)
		{
			AreEqual(t => t, expected, result, ComparerBuilder.GetEqualityComparer<T>());
		}

		protected void AreEqualWithComparer<T>(IEnumerable<T> expected, IEnumerable<T> result, Func<MemberAccessor, bool> memberPredicate)
		{
			AreEqual(t => t, expected, result, ComparerBuilder.GetEqualityComparer<T>(memberPredicate));
		}

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result, IEqualityComparer<T> comparer, bool allowEmpty = false, bool printData = false)
		{
			AreEqual(t => t, expected, result, comparer, allowEmpty, printData : printData);
		}

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result, IEqualityComparer<T> comparer, Func<IEnumerable<T>, IEnumerable<T>> sort)
		{
			AreEqual(t => t, expected, result, comparer, sort);
		}

		protected void AreEqual<T>(Func<T, T> fixSelector, IEnumerable<T> expected, IEnumerable<T> result)
		{
			AreEqual(fixSelector, expected, result, EqualityComparer<T>.Default);
		}

		protected void AreEqual<T>(Func<T, T> fixSelector, IEnumerable<T> expected, IEnumerable<T> result, IEqualityComparer<T> comparer, bool allowEmpty = false, bool printData = false)
		{
			AreEqual(fixSelector, expected, result, comparer, null, allowEmpty, printData : printData);
		}

		protected void AreEqual<T>(
			Func<T, T> fixSelector,
			IEnumerable<T> expected,
			IEnumerable<T> result,
			IEqualityComparer<T> comparer,
			Func<IEnumerable<T>, IEnumerable<T>>? sort,
			bool allowEmpty = false,
			bool printData  = false)
		{
			var resultList   = result.  Select(fixSelector).ToList();
			var lastQuery    = LastQuery;
			var expectedList = expected.Select(fixSelector).ToList();

			if (sort != null)
			{
				resultList = sort(resultList).ToList();
				expectedList = sort(expectedList).ToList();
			}

			if (printData)
			{
				Console.WriteLine(expectedList.ToDiagnosticString("Expected"));
				Console.WriteLine(resultList.  ToDiagnosticString("Result"));
			}

			if (!allowEmpty)
				Assert.That(expectedList, Is.Not.Empty, "Expected list cannot be empty.");
			Assert.That(resultList, Has.Count.EqualTo(expectedList.Count), "Expected and result lists are different. Length: ");

			var exceptExpectedList = resultList.  Except(expectedList, comparer).ToList();
			var exceptResultList   = expectedList.Except(resultList,   comparer).ToList();

			var exceptExpected = exceptExpectedList.Count;
			var exceptResult   = exceptResultList.  Count;
			var message        = new StringBuilder();

			if (exceptResult != 0 || exceptExpected != 0)
			{
				Debug.WriteLine(resultList.ToDiagnosticString());
				Debug.WriteLine(expectedList.ToDiagnosticString());

				for (var i = 0; i < resultList.Count; i++)
				{
					var equals = comparer.Equals(expectedList[i], resultList[i]);
					Debug.WriteLine("{0} {1} {3} {2}", equals ? " " : "!", expectedList[i], resultList[i], equals ? "==" : "<>");
					message.AppendFormat("{0} {1} {3} {2}", equals ? " " : "!", expectedList[i], resultList[i], equals ? "==" : "<>");
					message.AppendLine();
				}
			}

			Assert.That(exceptExpected, Is.EqualTo(0), $"Expected Was{Environment.NewLine}{message}");
			Assert.That(exceptResult, Is.EqualTo(0), $"Expect Result{Environment.NewLine}{message}");

			LastQuery = lastQuery;
		}

		protected void AreEqual<T>(IEnumerable<IEnumerable<T>> expected, IEnumerable<IEnumerable<T>> result)
		{
			var resultList   = result.ToList();
			var expectedList = expected.ToList();

			Assert.That(expectedList, Is.Not.Empty);
			Assert.That(resultList, Has.Count.EqualTo(expectedList.Count), "Expected and result lists are different. Length: ");

			for (var i = 0; i < resultList.Count; i++)
			{
				var elist = expectedList[i].ToList();
				var rlist = resultList[i].ToList();

				if (elist.Count > 0 || rlist.Count > 0)
					AreEqual(elist, rlist);
			}
		}

		protected void AreSame<T>(IEnumerable<T> expected, IEnumerable<T> result)
		{
			var resultList   = result.ToList();
			var expectedList = expected.ToList();

			Assert.That(expectedList, Is.Not.Empty);
			Assert.That(resultList, Has.Count.EqualTo(expectedList.Count));

			var b = expectedList.SequenceEqual(resultList);

			if (!b)
				for (var i = 0; i < resultList.Count; i++)
					Debug.WriteLine("{0} {1} --- {2}", Equals(expectedList[i], resultList[i]) ? " " : "-", expectedList[i], resultList[i]);

			Assert.That(b, Is.True);
		}

		void TestOnePerson(int id, string firstName, IQueryable<Person> persons)
		{
			var list = persons.ToList();

			Assert.That(list, Has.Count.EqualTo(1));

			var person = list[0];

			Assert.That(person.ID, Is.EqualTo(id));
			Assert.That(person.FirstName, Is.EqualTo(firstName));
		}

		protected void TestOneJohn(IQueryable<Person> persons)
		{
			TestOnePerson(1, "John", persons);
		}

		protected void TestPerson(int id, string firstName, IQueryable<IPerson> persons)
		{
			var person = persons.ToList().First(p => p.ID == id);

			Assert.That(person.ID, Is.EqualTo(id));
			Assert.That(person.FirstName, Is.EqualTo(firstName));
		}

		protected void TestJohn(IQueryable<IPerson> persons)
		{
			TestPerson(1, "John", persons);
		}

		static readonly char[] _newlineSeparators = new char[] { '\r', '\n' };

		protected void CompareSql(string expected, string result)
		{
			Assert.That(normalize(result), Is.EqualTo(normalize(expected)));

			static string normalize(string sql)
			{
				var lines = sql.Split(_newlineSeparators, StringSplitOptions.RemoveEmptyEntries);
				return string.Join("\n", lines.Where(l => !l.StartsWith("-- ")).Select(l => l.TrimStart('\t', ' ')));
			}
		}

		// helper to detect tests that leave database in inconsistent state
		// enable only for debug to not slowdown tests
		bool _badState;
#pragma warning disable CA1805 // Do not initialize unnecessarily
		bool _assertStateEnabled = false;
#pragma warning restore CA1805 // Do not initialize unnecessarily
		void AssertState(string context)
		{
			// don't fail tests if database is not consistent already
			if (!_assertStateEnabled || _badState)
				return;

			using var _ = new DisableBaseline("isn't baseline query");
			using var db = GetDataConnection(context);

			try
			{
				AreEqual(Person.OrderBy(_ => _.ID), db.Person.OrderBy(_ => _.ID), ComparerBuilder.GetEqualityComparer<IPerson>());
				AreEqual(Doctor.OrderBy(_ => _.PersonID), db.Doctor.OrderBy(_ => _.PersonID), ComparerBuilder.GetEqualityComparer<Doctor>());
				AreEqual(Patient.OrderBy(_ => _.PersonID), db.Patient.OrderBy(_ => _.PersonID), ComparerBuilder.GetEqualityComparer<Patient>(_ => _.PersonID, _ => _.Diagnosis));

				AreEqual(Parent.OrderBy(_ => _.ParentID), db.Parent.OrderBy(_ => _.ParentID), ComparerBuilder.GetEqualityComparer<Parent>(_ => _.ParentID, _ => _.Value1));
				AreEqual(Child.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID), db.Child.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID), ComparerBuilder.GetEqualityComparer<Child>(_ => _.ParentID, _ => _.ChildID));
				AreEqual(GrandChild.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID).ThenBy(_ => _.GrandChildID), db.GrandChild.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID).ThenBy(_ => _.GrandChildID), ComparerBuilder.GetEqualityComparer<GrandChild>(_ => _.ParentID, _ => _.ChildID, _ => _.GrandChildID));

				AreEqual(InheritanceParent.OrderBy(_ => _.InheritanceParentId), db.InheritanceParent.OrderBy(_ => _.InheritanceParentId), ComparerBuilder.GetEqualityComparer<InheritanceParentBase>());
				AreEqual(InheritanceChild.OrderBy(_ => _.InheritanceChildId), db.InheritanceChild.OrderBy(_ => _.InheritanceChildId), ComparerBuilder.GetEqualityComparer<InheritanceChildBase>(_ => _.InheritanceChildId, _ => _.TypeDiscriminator, _ => _.InheritanceParentId));

				AreEqual(Types2.OrderBy(_ => _.ID), db.Types2.OrderBy(_ => _.ID), ComparerBuilder.GetEqualityComparer<LinqDataTypes2>());

				// TODO: AllTypes
			}
			catch
			{
				_badState = true;
				throw new InvalidOperationException("SMOrc");
			}
		}
	}
}
