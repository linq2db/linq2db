using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Linq.Builder;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ParserTests : TestBase
	{
		static ParserTests()
		{
			ExpressionBuilder.AddBuilder(new MyContextParser());
		}

		#region IsExpressionTable

		[Test]
		public void IsExpressionTable1()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionTable2()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionTable3()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		#endregion

		#region IsExpressionScalar

		[Test]
		public void IsExpressionScalar1()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID)
					.Select    (p2 => p2)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionScalar2()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.Select    (p2 => p2)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionScalar3()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1)
					.Select    (p2 => p2)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionScalar4()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.Where     (p3 => p3 == 1)
					.Select    (p2 => p2)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				//Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.SubQuery));
			}
		}

		[Test]
		public void IsExpressionScalar5()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1)
					.Select    (p2 => p2.ParentID)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionScalar6()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p => p.Parent)
					.Select    (p => p)
					.GetMyContext();

				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionScalar7()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p => p)
					.Select    (p => p)
					.Select    (p => p.Parent)
					.GetMyContext();

				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionScalar8()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p  => p)
					.Select    (p3 => new { p1 = new { p2 = new { p = p3 } } })
					.Select    (p  => p.p1.p2.p.Parent)
					.GetMyContext();

				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionScalar9()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p  => p)
					.Select    (p3 => new { p1 = new { p2 = new { p = p3.Parent } } })
					.Select    (p  => p.p1.p2.p)
					.GetMyContext();

				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}


		[Test]
		public void IsExpressionScalar10()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p => p)
					.Select    (p => new { p = new { p } })
					.Select    (p => p.p)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		[Test]
		public void IsExpressionScalar11()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p => p)
					.Select    (p => new { p = new Child { ChildID = p.ChildID } })
					.Select    (p => p.p)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.SubQuery).   Result);
			}
		}

		#endregion

		#region IsExpressionSelect

		[Test]
		public void IsExpressionSelect1()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1.ParentID })
					.Select    (p2 => p2.ParentID)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect2()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Select    (p2 => p2.p)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect3()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1 })
					.Select    (p2 => p2.p1)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect4()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Where     (p3 => p3.p == 1)
					.Select    (p2 => p2.p)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect42()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Where     (p3 => p3.p == 1)
					.Select    (p2 => p2)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect5()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1 })
					.Select    (p2 => p2.p1.ParentID)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect6()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p => new { p })
					.Select    (p => p)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect7()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p => new { p, p.Parent })
					.Select    (p => new { p.Parent, p.p.ChildID })
					.Select    (p => p.Parent)
					.GetMyContext();

				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect8()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p => new { p, p.Parent })
					.Select    (p => new { p.Parent.ParentID, p.p.ChildID })
					.Select    (p => p.ParentID)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect9()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.GrandChild
					.Select    (p => new { p, p.Child })
					.Select    (p => new { p.Child.Parent.ParentID, p.p.ChildID })
					.Select    (p => p.ParentID)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect10()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p => p.Children.Max(c => (int?)c.ChildID) ?? p.Value1)
					.Select    (p => p)
					.GetMyContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		#endregion

		#region ConvertToIndexTable

		[Test]
		public void ConvertToIndexTable1()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent1
					.Select    (t => t)
					.GetMyContext();

				Assert.AreEqual(new[] { 0, 1 }, ctx.ConvertToIndex(null, 0, ConvertFlags.All).Select(_ => _.Index).ToArray());
				Assert.AreEqual(new[] { 0    }, ctx.ConvertToIndex(null, 0, ConvertFlags.Key).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexTable2()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (t => t)
					.GetMyContext();

				Assert.AreEqual(new[] { 0, 1 }, ctx.ConvertToIndex(null, 0, ConvertFlags.All).Select(_ => _.Index).ToArray());
				Assert.AreEqual(new[] { 0, 1 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Key).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexTable3()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (t => t.ParentID)
					.GetMyContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexTable4()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (t => t.Value1)
					.GetMyContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexTable5()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (t => new { t = new { t } })
					.Select    (t => t.t.t.ParentID)
					.Select    (t => t)
					.GetMyContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		#endregion

		#region ConvertToIndex

		[Test]
		public void ConvertToIndexScalar1()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID)
					.Select    (p2 => p2)
					.GetMyContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexScalar2()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.Select    (p2 => p2)
					.GetMyContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexScalar3()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.Where     (p3 => p3 == 1)
					.Select    (p2 => p2)
					.GetMyContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexScalar4()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = new { p = p1.ParentID } })
					.Select    (p2 => p2.p.p)
					.GetMyContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexJoin1()
		{
			using (var db = new TestDataConnection())
			{
				var q2 =
					from gc1 in db.GrandChild
						join max in
							from gch in db.GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc1.GrandChildID equals max
					select gc1;

				var result =
					from ch in db.Child
						join p   in db.Parent on ch.ParentID equals p.ParentID
						join gc2 in q2        on p.ParentID  equals gc2.ParentID into g
						from gc3 in g.DefaultIfEmpty()
				select gc3;

				var ctx = result.GetMyContext();
				var idx = ctx.ConvertToIndex(null, 0, ConvertFlags.Key);

				Assert.AreEqual(new[] { 0, 1, 2 }, idx.Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexJoin2()
		{
			using (var db = new TestDataConnection())
			{
				var result =
					from ch in db.Child
						join gc2 in db.GrandChild on ch.ParentID  equals gc2.ParentID into g
						from gc3 in g.DefaultIfEmpty()
					select gc3;

				var ctx = result.GetMyContext();
				var idx = ctx.ConvertToIndex(null, 0, ConvertFlags.Key);

				Assert.AreEqual(new[] { 0, 1, 2 }, idx.Select(_ => _.Index).ToArray());

				idx = ctx.ConvertToIndex(null, 0, ConvertFlags.All);

				Assert.AreEqual(new[] { 0, 1, 2 }, idx.Select(_ => _.Index).ToArray());
			}
		}

		#endregion

		#region ConvertToSql

		[Test]
		public void ConvertToSql1()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1.ParentID })
					.Select    (p2 => p2.ParentID)
					.GetMyContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SqlField), sql[0].Sql);
				Assert.AreEqual        ("ParentID", ((SqlField)sql[0].Sql).Name);
			}
		}

		[Test]
		public void ConvertToSql2()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Select    (p2 => p2.p)
					.GetMyContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SqlBinaryExpression), sql[0].Sql);
			}
		}

		[Test]
		public void ConvertToSql3()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Where     (p3 => p3.p == 1)
					.Select    (p2 => p2.p)
					.GetMyContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SelectQuery.Column), sql[0].Sql);
			}
		}

		[Test]
		public void ConvertToSql4()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1 })
					.Select    (p2 => p2.p1.ParentID)
					.GetMyContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SqlField), sql[0].Sql);
				Assert.AreEqual        ("ParentID", ((SqlField)sql[0].Sql).Name);
			}
		}

		[Test]
		public void ConvertToSql5()
		{
			using (var db = new TestDataConnection())
			{
				var ctx = db.Child
					.Select    (p => new { p, p.Parent })
					.Select    (p => new { p.Parent.ParentID, p.p.ChildID })
					.Select    (p => p.ParentID)
					.GetMyContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SqlField), sql[0].Sql);
				Assert.AreEqual        ("ParentID", ((SqlField)sql[0].Sql).Name);
			}
		}

		#endregion

		#region SqlTest

		[Test]
		public void Join1()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from t in
						from ch in db.Child
							join p in db.Parent on ch.ParentID equals p.ParentID
						select ch.ParentID + p.ParentID
					where t > 2
					select t;

				var ctx = q.GetMyContext();
				ctx.BuildExpression(null, 0);

				Assert.AreEqual(1, ctx.SelectQuery.Select.Columns.Count);
			}
		}

		[Test]
		public void Join2()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from t in
						from ch in db.Child
							join p in db.Parent on ch.ParentID equals p.ParentID
						select new { ID = ch.ParentID + p.ParentID }
					where t.ID > 2
					select t;

				var ctx = q.GetMyContext();
				ctx.BuildExpression(null, 0);

				Assert.AreEqual(2, ctx.SelectQuery.Select.Columns.Count);
			}
		}

		public class MyClass
		{
			public int ID;
		}

		[Test]
		public void Join3()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from p in db.Parent
					join j in db.Child on p.ParentID equals j.ParentID
					select p;

				var ctx = q.GetMyContext();
				ctx.BuildExpression(null, 0);

				Assert.AreEqual(2, ctx.SelectQuery.Select.Columns.Count);
			}
		}

		[Test]
		public void Join4()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from p in db.Parent
					select new { ID = new MyClass { ID = p.ParentID } }
					into p
					join j in
						from c in db.Child
						select new { ID = new MyClass { ID = c.ParentID } }
						on p.ID.ID equals j.ID.ID
					where p.ID.ID == 1
					select p;

				var ctx = q.GetMyContext();
				ctx.BuildExpression(null, 0);

				Assert.AreEqual(1, ctx.SelectQuery.Select.Columns.Count);
			}
		}

		[Test]
		public void Join5()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from p in db.Parent
						join c in db.Child      on p.ParentID equals c.ParentID
						join g in db.GrandChild on p.ParentID equals g.ParentID
					select new { p, c, g } into x
					select x.c.ParentID;

				var ctx = q.GetMyContext();
				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.All);

				Assert.AreEqual(1, sql.Length);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void Join6(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var q =
					from g in db.GrandChild
					join p in db.Parent4 on g.Child.ParentID equals p.ParentID
					select g;

				var ctx = q.GetMyContext();

				ctx.BuildExpression(null, 0);

				var sql = db.GetSqlText(ctx.SelectQuery);

				CompareSql(sql, @"
					SELECT
						[g1].[ParentID],
						[g1].[ChildID],
						[g1].[GrandChildID]
					FROM
						[GrandChild] [g1]
							LEFT JOIN [Child] [t1] ON [g1].[ParentID] = [t1].[ParentID] AND [g1].[ChildID] = [t1].[ChildID]
							INNER JOIN [Parent] [p] ON [t1].[ParentID] = [p].[ParentID]");
			}
		}

		#endregion

		[Test]
		public void OracleXmlTable()
		{
			using (var db = new TestDataConnection())
			{
				Assert.IsNotNull(db.OracleXmlTable<Person>(() => "<xml/>"));
				Assert.IsNotNull(db.OracleXmlTable<Person>("<xml/>"));
				Assert.IsNotNull(db.OracleXmlTable<Person>(new [] {new Person(), }));

			}

		}

	}

	class MyContextParser : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var call = buildInfo.Expression as MethodCallExpression;
			return call != null && call.Method.Name == "GetMyContext";
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var call = (MethodCallExpression)buildInfo.Expression;
			return new Context(builder.BuildSequence(new BuildInfo(buildInfo, call.Arguments[0])));
		}

		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return builder.IsSequence(new BuildInfo(buildInfo, ((MethodCallExpression)buildInfo.Expression).Arguments[0]));
		}

		public class Context : PassThroughContext
		{
			public Context(IBuildContext context) : base(context)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				query.GetElement = (ctx,db,expr,ps) => this;
			}
		}
	}

	static class Extensions
	{
		public static MyContextParser.Context GetMyContext<T>(this IQueryable<T> source)
		{

			if (source == null) throw new ArgumentNullException("source");

			var methodInfo = typeof(Extensions).GetMethods()
				.Single(method => method.Name == "GetMyContext" 
				&& method.GetParameters()
				.ElementAt(0)
				.ParameterType
				.GetGenericTypeDefinition() == typeof(IQueryable<>));

			return source.Provider.Execute<MyContextParser.Context>(
				Expression.Call(
					null,
					methodInfo.MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression }));
		}
	}
}
