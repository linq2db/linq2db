using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Linq.Builder;
using LinqToDB.SqlBuilder;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ParserTest : TestBase
	{
		static ParserTest()
		{
			ExpressionBuilder.AddBuilder(new ContextParser());
		}

		#region IsExpressionTable

		[Test]
		public void IsExpressionTable1()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID)
					.Select    (p2 => p2)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.Select    (p2 => p2)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1)
					.Select    (p2 => p2)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.Where     (p3 => p3 == 1)
					.Select    (p2 => p2)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1)
					.Select    (p2 => p2.ParentID)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p => p.Parent)
					.Select    (p => p)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p => p)
					.Select    (p => p)
					.Select    (p => p.Parent)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p  => p)
					.Select    (p3 => new { p1 = new { p2 = new { p = p3 } } })
					.Select    (p  => p.p1.p2.p.Parent)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p  => p)
					.Select    (p3 => new { p1 = new { p2 = new { p = p3.Parent } } })
					.Select    (p  => p.p1.p2.p)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p => p)
					.Select    (p => new { p = new { p } })
					.Select    (p => p.p)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p => p)
					.Select    (p => new { p = new Child { ChildID = p.ChildID } })
					.Select    (p => p.p)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1.ParentID })
					.Select    (p2 => p2.ParentID)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect2()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Select    (p2 => p2.p)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect3()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1 })
					.Select    (p2 => p2.p1)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect4()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Where     (p3 => p3.p == 1)
					.Select    (p2 => p2.p)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect42()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Where     (p3 => p3.p == 1)
					.Select    (p2 => p2)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect5()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1 })
					.Select    (p2 => p2.p1.ParentID)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect6()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p => new { p })
					.Select    (p => p)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect7()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p => new { p, p.Parent })
					.Select    (p => new { p.Parent, p.p.ChildID })
					.Select    (p => p.Parent)
					.GetContext();

				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect8()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p => new { p, p.Parent })
					.Select    (p => new { p.Parent.ParentID, p.p.ChildID })
					.Select    (p => p.ParentID)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect9()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.GrandChild
					.Select    (p => new { p, p.Child })
					.Select    (p => new { p.Child.Parent.ParentID, p.p.ChildID })
					.Select    (p => p.ParentID)
					.GetContext();

				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Association).Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Object).     Result);
				Assert.IsTrue (ctx.IsExpression(null, 0, RequestFor.Field).      Result);
				Assert.IsFalse(ctx.IsExpression(null, 0, RequestFor.Expression). Result);
			}
		}

		[Test]
		public void IsExpressionSelect10()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p => p.Children.Max(c => (int?)c.ChildID) ?? p.Value1)
					.Select    (p => p)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent1
					.Select    (t => t)
					.GetContext();

				Assert.AreEqual(new[] { 0, 1 }, ctx.ConvertToIndex(null, 0, ConvertFlags.All).Select(_ => _.Index).ToArray());
				Assert.AreEqual(new[] { 0    }, ctx.ConvertToIndex(null, 0, ConvertFlags.Key).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexTable2()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (t => t)
					.GetContext();

				Assert.AreEqual(new[] { 0, 1 }, ctx.ConvertToIndex(null, 0, ConvertFlags.All).Select(_ => _.Index).ToArray());
				Assert.AreEqual(new[] { 0, 1 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Key).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexTable3()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (t => t.ParentID)
					.GetContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexTable4()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (t => t.Value1)
					.GetContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexTable5()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (t => new { t = new { t } })
					.Select    (t => t.t.t.ParentID)
					.Select    (t => t)
					.GetContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		#endregion

		#region ConvertToIndex

		[Test]
		public void ConvertToIndexScalar1()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID)
					.Select    (p2 => p2)
					.GetContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexScalar2()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.Select    (p2 => p2)
					.GetContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexScalar3()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => p1.ParentID + 1)
					.Where     (p3 => p3 == 1)
					.Select    (p2 => p2)
					.GetContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexScalar4()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = new { p = p1.ParentID } })
					.Select    (p2 => p2.p.p)
					.GetContext();

				Assert.AreEqual(new[] { 0 }, ctx.ConvertToIndex(null, 0, ConvertFlags.Field).Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexJoin1()
		{
			using (var db = new TestDbManager())
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

				var ctx = result.GetContext();
				var idx = ctx.ConvertToIndex(null, 0, ConvertFlags.Key);

				Assert.AreEqual(new[] { 0, 1, 2 }, idx.Select(_ => _.Index).ToArray());
			}
		}

		[Test]
		public void ConvertToIndexJoin2()
		{
			using (var db = new TestDbManager())
			{
				var result =
					from ch in db.Child
						join gc2 in db.GrandChild on ch.ParentID  equals gc2.ParentID into g
						from gc3 in g.DefaultIfEmpty()
					select gc3;

				var ctx = result.GetContext();
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
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1.ParentID })
					.Select    (p2 => p2.ParentID)
					.GetContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SqlField), sql[0].Sql);
				Assert.AreEqual        ("ParentID", ((SqlField)sql[0].Sql).Name);
			}
		}

		[Test]
		public void ConvertToSql2()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Select    (p2 => p2.p)
					.GetContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SqlBinaryExpression), sql[0].Sql);
			}
		}

		[Test]
		public void ConvertToSql3()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p = p1.ParentID + 1 })
					.Where     (p3 => p3.p == 1)
					.Select    (p2 => p2.p)
					.GetContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SqlQuery.Column), sql[0].Sql);
			}
		}

		[Test]
		public void ConvertToSql4()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Parent
					.Select    (p1 => new { p1 })
					.Select    (p2 => p2.p1.ParentID)
					.GetContext();

				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.Field);

				Assert.AreEqual        (1, sql.Length);
				Assert.IsAssignableFrom(typeof(SqlField), sql[0].Sql);
				Assert.AreEqual        ("ParentID", ((SqlField)sql[0].Sql).Name);
			}
		}

		[Test]
		public void ConvertToSql5()
		{
			using (var db = new TestDbManager())
			{
				var ctx = db.Child
					.Select    (p => new { p, p.Parent })
					.Select    (p => new { p.Parent.ParentID, p.p.ChildID })
					.Select    (p => p.ParentID)
					.GetContext();

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
			using (var db = new TestDbManager())
			{
				var q =
					from t in
						from ch in db.Child
							join p in db.Parent on ch.ParentID equals p.ParentID
						select ch.ParentID + p.ParentID
					where t > 2
					select t;

				var ctx = q.GetContext();
				ctx.BuildExpression(null, 0);

				Assert.AreEqual(1, ctx.SqlQuery.Select.Columns.Count);
			}
		}

		[Test]
		public void Join2()
		{
			using (var db = new TestDbManager())
			{
				var q =
					from t in
						from ch in db.Child
							join p in db.Parent on ch.ParentID equals p.ParentID
						select new { ID = ch.ParentID + p.ParentID }
					where t.ID > 2
					select t;

				var ctx = q.GetContext();
				ctx.BuildExpression(null, 0);

				Assert.AreEqual(2, ctx.SqlQuery.Select.Columns.Count);
			}
		}

		public class MyClass
		{
			public int ID;
		}

		[Test]
		public void Join3()
		{
			using (var db = new TestDbManager())
			{
				var q =
					from p in db.Parent
					join j in db.Child on p.ParentID equals j.ParentID
					select p;

				var ctx = q.GetContext();
				ctx.BuildExpression(null, 0);

				Assert.AreEqual(2, ctx.SqlQuery.Select.Columns.Count);
			}
		}

		[Test]
		public void Join4()
		{
			using (var db = new TestDbManager())
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

				var ctx = q.GetContext();
				ctx.BuildExpression(null, 0);

				Assert.AreEqual(1, ctx.SqlQuery.Select.Columns.Count);
			}
		}

		[Test]
		public void Join5()
		{
			using (var db = new TestDbManager())
			{
				var q =
					from p in db.Parent
						join c in db.Child      on p.ParentID equals c.ParentID
						join g in db.GrandChild on p.ParentID equals g.ParentID
					select new { p, c, g } into x
					select x.c.ParentID;

				var ctx = q.GetContext();
				var sql = ctx.ConvertToSql(null, 0, ConvertFlags.All);

				Assert.AreEqual(1, sql.Length);
			}
		}

		[Test]
		public void Join6([IncludeDataContexts(ProviderName.SqlServer2008)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				var q =
					from g in db.GrandChild
					join p in db.Parent4 on g.Child.ParentID equals p.ParentID
					select g;

				var ctx = q.GetContext();

				ctx.BuildExpression(null, 0);

				var sql = db.GetSqlText(ctx.SqlQuery);

				CompareSql(sql, @"
					SELECT
						[g].[ParentID],
						[g].[ChildID],
						[g].[GrandChildID]
					FROM
						[GrandChild] [g]
							LEFT JOIN [Child] [t1] ON [g].[ParentID] = [t1].[ParentID] AND [g].[ChildID] = [t1].[ChildID]
							INNER JOIN [Parent] [p] ON [t1].[ParentID] = [p].[ParentID]");
			}
		}

		#endregion
	}

	class ContextParser : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var call = buildInfo.Expression as MethodCallExpression;
			return call != null && call.Method.Name == "GetContext";
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
		public static ContextParser.Context GetContext<T>(this IQueryable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			return source.Provider.Execute<ContextParser.Context>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression }));
		}

		static public Expression Unwrap(this Expression ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.Quote          :
				case ExpressionType.Convert        :
				case ExpressionType.ConvertChecked : return ((UnaryExpression)ex).Operand.Unwrap();
			}

			return ex;
		}
	}
}
