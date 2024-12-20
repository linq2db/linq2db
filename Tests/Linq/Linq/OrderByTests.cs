using System;
using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.SqlQuery;
using LinqToDB.Tools;

using NUnit.Framework;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class OrderByTests : TestBase
	{
		[Test]
		public void OrderBy1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in Child
					orderby ch.ParentID descending, ch.ChildID ascending
					select ch;

				var result =
					from ch in db.Child
					orderby ch.ParentID descending, ch.ChildID ascending
					select ch;

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBy2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in Child
					orderby ch.ParentID descending, ch.ChildID ascending
					select ch;

				var result =
					from ch in db.Child
					orderby ch.ParentID descending, ch.ChildID ascending
					select ch;

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBy3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in
						from ch in Child
						orderby ch.ParentID descending
						select ch
					orderby ch.ParentID descending , ch.ChildID
					select ch;

				var result =
					from ch in
						from ch in db.Child
						orderby ch.ParentID descending
						select ch
					orderby ch.ParentID descending , ch.ChildID
					select ch;

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBy4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in
						from ch in Child
						orderby ch.ParentID descending
						select ch
					orderby ch.ParentID descending, ch.ChildID, ch.ParentID + 1 descending
					select ch;

				var result =
					from ch in
						from ch in db.Child
						orderby ch.ParentID descending
						select ch
					orderby ch.ParentID descending, ch.ChildID, ch.ParentID + 1 descending
					select ch;

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBy5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in Child
					orderby ch.ChildID % 2, ch.ChildID
					select ch;

				var result =
					from ch in db.Child
					orderby ch.ChildID % 2, ch.ChildID
					select ch;

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void ConditionOrderBy([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in Child
					orderby ch.ParentID > 0 && ch.ChildID != ch.ParentID descending, ch.ChildID
					select ch;

				var result =
					from ch in db.Child
					orderby ch.ParentID > 0 && ch.ChildID != ch.ParentID descending, ch.ChildID
					select ch;

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBy6([DataSources(false)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var q =
					from person in db.Person
					join patient in db.Patient on person.ID equals patient.PersonID into g
					from patient in g.DefaultIfEmpty()
					orderby person.MiddleName // if comment this line then "Diagnosis" is not selected.
					select new { person.ID, PatientID = patient != null ? (int?)patient.PersonID : null };

				q.ToList();

				Assert.That(db.LastQuery!, Does.Not.Contain("Diagnosis"), "Why do we select Patient.Diagnosis??");
			}
		}

		[Test]
		public void OrderBy7([DataSources] string context)
		{
			using (var db = GetDataContext(context, o => o.UseDoNotClearOrderBys(true)))
			{

				var expected =
					from ch in Child
					orderby ch.ChildID % 2, ch.ChildID
					select ch;

				var qry =
					from ch in db.Child
					orderby ch.ChildID % 2
					select new { ch };

				var result = qry.OrderBy(x => x.ch.ChildID).Select(x => x.ch);

				AreSame(expected, result);
			}
		}

		[Test]
		public void OrderBy8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					from ch in Child
					orderby ch.ChildID%2, ch.ChildID
					select ch;

				var qry =
					from ch in db.Child
					orderby ch.ChildID%2
					select new {ch};

				var result = qry.ThenOrBy(x => x.ch.ChildID).Select(x => x.ch);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBy9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					from ch in Child
					orderby ch.ChildID%2, ch.ChildID descending
					select ch;

				var qry =
					from ch in db.Child
					orderby ch.ChildID%2 descending
					select new {ch};

				var result = qry.ThenOrByDescending(x => x.ch.ChildID).Select(x => x.ch);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBy10([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					(from ch in Child
					orderby ch.ChildID%2
					select ch).ThenByDescending(ch => ch.ChildID);

				var qry =
					from ch in db.Child
					orderby ch.ChildID%2
					select new {ch};

				var result = qry.ThenOrByDescending(x => x.ch.ChildID).Select(x => x.ch);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBy11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					(from ch in Child
					orderby ch.ChildID%2
					select ch).ThenByDescending(ch => ch.ChildID);

				var qry =
					from ch in db.Child
					orderby ch.ChildID%2
					select ch;

				var result = qry.ThenOrByDescending(x => x.ChildID).Select(x => x);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBy12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					from ch in Child
					orderby ch.ChildID%2 descending
					select ch;

				var qry =
					from ch in db.Child
					select ch;

				var result = qry.ThenOrByDescending(x => x.ChildID%2);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBySelf1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Parent orderby p select p;
				var result   = from p in db.Parent orderby p select p;
				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBySelf2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Parent1 orderby p select p;
				var result   = from p in db.Parent1 orderby p select p;
				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBySelectMany1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent.OrderBy(p => p.ParentID)
					from c in Child. OrderBy(c => c.ChildID)
					where p == c.Parent
					select new { p.ParentID, c.ChildID };

				var result =
					from p in db.Parent.OrderBy(p => p.ParentID)
					from c in db.Child. OrderBy(c => c.ChildID)
					where p == c.Parent
					select new { p.ParentID, c.ChildID };

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBySelectMany2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent1.OrderBy(p => p.ParentID)
					from c in Child.  OrderBy(c => c.ChildID)
					where p.ParentID == c.Parent1!.ParentID
					select new { p.ParentID, c.ChildID };

				var result =
					from p in db.Parent1.OrderBy(p => p.ParentID)
					from c in db.Child.  OrderBy(c => c.ChildID)
					where p == c.Parent1
					select new { p.ParentID, c.ChildID };

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void OrderBySelectMany3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent.OrderBy(p => p.ParentID)
					from c in Child. OrderBy(c => c.ChildID)
					where c.Parent == p
					select new { p.ParentID, c.ChildID };

				var result =
					from p in db.Parent.OrderBy(p => p.ParentID)
					from c in db.Child. OrderBy(c => c.ChildID)
					where c.Parent == p
					select new { p.ParentID, c.ChildID };

				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void OrderByContinuous([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var firstOrder =
					from p in db.Parent
					orderby (p.Children.Count)
					select p;

				var secondOrder =
					from p in firstOrder
					join pp in db.Parent on p.Value1 equals pp.Value1
					orderby pp.ParentID
					select p;

				secondOrder.ToArray();

				var selectQuery = secondOrder.GetSelectQuery();
				Assert.That(selectQuery.OrderBy.Items, Has.Count.EqualTo(2));
				var field = QueryHelper.GetUnderlyingField(selectQuery.OrderBy.Items[0].Expression);
				Assert.That(field, Is.Not.Null);
				Assert.That(field!.Name, Is.EqualTo("ParentID"));
			}
		}

		[Test]
		public void OrderByContinuousDuplicates([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var firstOrder =
					from p in db.Parent
					orderby p.ParentID
					select p;

				var secondOrder =
					from p in firstOrder
					join pp in db.Parent on p.ParentID equals pp.ParentID
					orderby p.ParentID descending
					select p;

				secondOrder.ToArray();
			
				var selectQuery = secondOrder.GetSelectQuery();
				Assert.That(selectQuery.OrderBy.Items, Has.Count.EqualTo(1));
				Assert.That(selectQuery.OrderBy.Items[0].IsDescending, Is.True);
				var field = QueryHelper.GetUnderlyingField(selectQuery.OrderBy.Items[0].Expression);
				Assert.That(field, Is.Not.Null);
				Assert.That(field!.Name, Is.EqualTo("ParentID"));
			}
		}

		[Test]
		public void OrderAscDesc([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =    Parent.OrderBy(p => p.ParentID).OrderByDescending(p => p.ParentID);
				var result   = db.Parent.OrderBy(p => p.ParentID).OrderByDescending(p => p.ParentID);
				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void Count1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent.OrderBy(p => p.ParentID).Count(), Is.EqualTo(Parent.OrderBy(p => p.ParentID).Count()));
		}

		[Test]
		public void Count2([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent.OrderBy(p => p.ParentID).Take(3).Count(), Is.EqualTo(Parent.OrderBy(p => p.ParentID).Take(3).Count()));
		}

		[Test]
		public void Min1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent.OrderBy(p => p.ParentID).Min(p => p.ParentID), Is.EqualTo(Parent.OrderBy(p => p.ParentID).Min(p => p.ParentID)));
		}

		[Test]
		public void Min2([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent.OrderBy(p => p.ParentID).Take(3).Min(p => p.ParentID), Is.EqualTo(Parent.OrderBy(p => p.ParentID).Take(3).Min(p => p.ParentID)));
		}

		[Test]
		public void Min3([DataSources(TestProvName.AllSybase, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent.OrderBy(p => p.Value1).Take(3).Min(p => p.ParentID), Is.EqualTo(Parent.OrderBy(p => p.Value1).Take(3).Min(p => p.ParentID)));
		}

		[Test]
		public void Distinct([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in Parent
					join c in Child on p.ParentID equals c.ParentID
					join g in GrandChild on c.ChildID equals  g.ChildID
					select p).Distinct().OrderBy(p => p.ParentID)
					,
					(from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					join g in db.GrandChild on c.ChildID equals  g.ChildID
					select p).Distinct().OrderBy(p => p.ParentID));
		}

		[Test]
		public void Take([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					(from p in db.Parent
					 join c in db.Child on p.ParentID equals c.ParentID
					 join g in db.GrandChild on c.ChildID equals g.ChildID
					 select p).Take(3).OrderBy(p => p.ParentID);

				Assert.That(q.AsEnumerable().Count(), Is.EqualTo(3));
			}
		}


		[Test]
		public void OrderByConstant([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var param = 2;
				var query =
					from ch in db.Child
					orderby "1" descending, param - 2
					select ch;

				query.ToArray();

				Assert.That(db.LastQuery, Does.Not.Contain("ORDER BY"));
			}
		}

		[Test]
		public void OrderByConstant2([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var param = 2;
				var query =
					from ch in db.Child
					orderby Sql.ToNullable((int)Sql.Abs(1)!) descending, Sql.ToNullable((int)Sql.Abs(param + 1)!)
					select ch;

				query.ToArray();

				Assert.That(db.LastQuery, Does.Not.Contain("ORDER BY"));
			}
		}


		[Test]
		public void OrderByIndex([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var query =
					from p in db.ComplexPerson
					where p.ID.In(1, 3)
					orderby Sql.Ordinal(p.Name.LastName) descending, p.Name.FirstName descending
					select new
					{
						p.ID, 
						p.Name.LastName
					};

				query.ToArray();

				db.LastQuery.Should().Contain("2 DESC");
			}
		}

		[Test]
		public void OrderByIndexOptimization([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withIndex)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var query =
					from p in db.ComplexPerson
					where p.ID.In(1, 3)
					select new
					{
						p.ID, 
						CuttedName = p.Name.LastName.Substring(0, 3)
					} into s
					orderby withIndex ? Sql.Ordinal(s.CuttedName) : s.CuttedName descending
					select new
					{
						s.ID, 
						s.CuttedName
					};

				query.ToArray();

				var selectQuery = query.GetSelectQuery();

				var firstSource = selectQuery.From.Tables[0].Source;

				if (withIndex)
				{
					firstSource.Should().BeOfType<SqlTable>();
				}
				else
				{
					firstSource.Should().BeOfType<SelectQuery>();
				}
			}
		}

		[Test]
		public void OrderByIndexInExpr([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var query =
					from p in db.ComplexPerson
					where Sql.Ordinal(p.Name.LastName) == "Some"
					select new
					{
						p.ID, 
						p.Name.LastName
					};

				FluentActions.Enumerating(() => query)
					.Should()
					.Throw<LinqToDBException>()
					.WithMessage("The LINQ expression 'Sql.Ordinal<string>(p.Name.LastName)' could not be converted to SQL.");
			}
		}

		[Test]
		public void OrderByImmutableSubquery([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var param = 2;

				var query =
					from ch in db.Child
					orderby Sql.ToNullable((int)Sql.Abs(1)!) descending, Sql.ToNullable((int)Sql.Abs(param + 1)!)
					select new { ch.ChildID, ch.ParentID, OrderElement = (int?)param };

				query.AsSubQuery().OrderBy(c => c.OrderElement).ToArray();

				Assert.That(db.LastQuery, Does.Not.Contain("ORDER BY"));
			}
		}

		[Test]
		public void OrderByUnionImmutable([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var param = 2;

				var query1 =
					from ch in db.Child
					orderby Sql.ToNullable((int)Sql.Abs(1)!) descending, Sql.ToNullable((int)Sql.Abs(param)!)
					select new { ch.ChildID, ch.ParentID, OrderElement = Sql.ToNullable((int)Sql.Abs(1)!) };

				var query2 =
					from ch in db.Child
					orderby "1" descending, param
					select new { ch.ChildID, ch.ParentID, OrderElement = (int?)param };

				var result = query1.Concat(query2).OrderBy(c => c.OrderElement)
					.ToArray();

				Assert.That(db.LastQuery, Does.Contain("ORDER BY"));
			}
		}

		[Test]
		public void OrderByInUnion([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{

				var query1 =
					db.Child.OrderBy(c => c.ChildID).Concat(db.Child.OrderByDescending(c => c.ChildID));
				var query2 =
					db.Child.Concat(db.Child.OrderByDescending(c => c.ChildID));

				var query3 = query1.OrderBy(_ => _.ChildID);

				Assert.DoesNotThrow(() => query1.ToArray());
				Assert.DoesNotThrow(() => query2.ToArray());
				Assert.DoesNotThrow(() => query3.ToArray());
			}
		}

		[Test]
		public void OrderByContains([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ids = new int[]{ 1, 3 };
				db.Person.OrderBy(_ => ids.Contains(_.ID)).ToList();
			}
		}

		[Test]
		public void OrderByContainsSubquery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ids = new int[]{ 1, 3 };
				db.Person.Select(_ => new { _.ID, _.LastName, flag = ids.Contains(_.ID) }).OrderBy(_ => _.flag).ToList();
			}
		}

		[Test]
		public void EnableConstantExpressionInOrderByTest([DataSources(ProviderName.SqlCe)] string context, [Values] bool enableConstantExpressionInOrderBy)
		{
			using var db  = GetDataContext(context, o => o.UseEnableConstantExpressionInOrderBy(enableConstantExpressionInOrderBy));

			var q =
			(
				from p in db.Person
				where p.ID.In(1, 3)
				orderby 1, p.LastName
				select new
				{
					p.ID,
					p.LastName
				}
			)
			.ToList();

			Assert.That(q[0].ID, Is.EqualTo(enableConstantExpressionInOrderBy ? 1 : 3));
		}

		[Test]
		public void EnableConstantExpressionInOrderByTest2([DataSources(ProviderName.SqlCe)] string context, [Values] bool enableConstantExpressionInOrderBy)
		{
			using var db  = GetDataContext(context, o => o.UseEnableConstantExpressionInOrderBy(enableConstantExpressionInOrderBy));

			var q =
			(
				from p in db.ComplexPerson
				where p.ID.In(1, 3)
				orderby 1 descending , p.Name.LastName descending
				select new
				{
					p.ID,
					p.Name.LastName
				}
			)
			.ToList();

			Assert.That(q[0].ID, Is.EqualTo(enableConstantExpressionInOrderBy ? 3 : 1));
		}

		[Test]
		public void EnableConstantExpressionInOrderByTest3([DataSources(ProviderName.SqlCe)] string context, [Values] bool enableConstantExpressionInOrderBy)
		{
			using var db  = GetDataContext(context, o => o.UseEnableConstantExpressionInOrderBy(enableConstantExpressionInOrderBy));

			var q =
			(
				from p in db.ComplexPerson
				where p.ID.In(1, 3)
				orderby 1, p.Name.LastName
				select p
			)
			.ToList();

			Assert.That(q[0].ID, Is.EqualTo(enableConstantExpressionInOrderBy ? 1 : 3));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4586")]
		public void Issue4586Test([DataSources(false)] string context)
		{
			using var db  = GetDataConnection(context);

			var result = db.Person.AsQueryable().Where(x => x.FirstName.StartsWith("J"))
				.OrderByDescending(x => x.ID)
				.Skip(1)
				.Take(2)
				.ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(result[0].ID, Is.EqualTo(3));
				Assert.That(result[1].ID, Is.EqualTo(1));
			});

			var selects = db.LastQuery!.Split(["SELECT"], StringSplitOptions.None).Length - 1;

			Assert.That(selects, Is.EqualTo(1).Or.EqualTo(2).Or.EqualTo(3));

			var expectedOrders = selects switch
			{
				1 => 1,
				_ => 2
			};

			Assert.That(expectedOrders, Is.EqualTo(db.LastQuery.Split(["ORDER BY"], StringSplitOptions.None).Length - 1));
		}
	}
}
