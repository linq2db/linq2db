using System;
using System.Linq.Expressions;
using System.Text;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class NullabilityContextTests : TestBase
	{
		class FirstTable
		{
			[Column(IsPrimaryKey = true)]
			public int FirstId { get; set; }

			[Column(CanBeNull = true)]
			public int? SecondId { get; set; }

			[Column(CanBeNull = true)]
			public int? FirstNullableInt { get; set; }

			[Column(CanBeNull = false)]
			public string FirstNotNullString { get; } = default!;
		}

		class SecondTable
		{
			[Column(IsPrimaryKey = true)]
			public int SecondId { get; set; }

			[Column(CanBeNull = true)]
			public int? ThirdId { get; set; }

			[Column(CanBeNull = true)]
			public int? SecondNullableInt { get; set; }

			[Column(CanBeNull = false)]
			public string SecondNotNullString { get; } = default!;
		}

		class ThirdTable
		{
			[Column(IsPrimaryKey = true)]
			public int ThirdId { get; set; }

			[Column(CanBeNull = true)]
			public int? FourthId { get; set; }

			[Column(CanBeNull = true)]
			public int? ThirdNullableInt { get; set; }

			[Column(CanBeNull = false)]
			public string ThirdNotNullString { get; } = default!;
		}

		class FourthTable
		{
			[Column(IsPrimaryKey = true)]
			public int FourthId { get; set; }

			[Column(CanBeNull = true)]
			public int? FourthNullableInt { get; set; }

			[Column(CanBeNull = false)]
			public string FourthNotNullString { get; } = default!;
		}

		#region Helper methods

		static SqlField GetField<TEntity>(SqlTable table, Expression<Func<TEntity, object?>> filedExpr)
		{
			var memberName = MemberHelper.PropertyOf(filedExpr).Name;
			var field      = table.FindFieldByMemberName(memberName);

			if (field == null)
				throw new InvalidOperationException($"Field '{memberName}' not found in table.");

			return field;
		}

		static SqlTable CreateTable<TEntity>(MappingSchema ms)
		{
			var entityDescriptor = ms.GetEntityDescriptor(typeof(TEntity));
			return new SqlTable(entityDescriptor);
		}

		static string GenerateSQL(SelectQuery selectQuery)
		{
			var dataProvider = new SQLiteDataProviderClassic();

			var stringBuilder     = new StringBuilder();
			var evaluationContext = new EvaluationContext();
			var dataOptions       = new DataOptions();
			var optimizer         = new SqlExpressionOptimizerVisitor(false);
			var sqlOptimizer      = dataProvider.GetSqlOptimizer(dataOptions);
			var factory           = sqlOptimizer.CreateSqlExpressionFactory(dataProvider.MappingSchema, dataOptions);
			var convertVisitor    = sqlOptimizer.CreateConvertVisitor(false);
			var aliasesContext    = new AliasesContext();

			var optimizationContext = new OptimizationContext(evaluationContext, dataOptions, dataProvider.SqlProviderFlags, dataProvider.MappingSchema, optimizer, convertVisitor, factory, false,
				true,
				static () => NoopQueryParametersNormalizer.Instance);

			var sqlBuilder = dataProvider.CreateSqlBuilder(dataProvider.MappingSchema, dataOptions);

			sqlBuilder.BuildSql(0, new SqlSelectStatement(selectQuery), stringBuilder, optimizationContext, aliasesContext, null, 0);

			return stringBuilder.ToString();
		}

		#endregion

		static void AssertJoinNotNullable(NullabilityContext nullabilityContext, SqlJoinedTable joinTable)
		{
			nullabilityContext = nullabilityContext.WithJoinSource(joinTable.Table.Source);

			foreach (var p in joinTable.Condition.Predicates)
			{
				if (p is not SqlPredicate.ExprExpr exprExpr)
					throw new InvalidOperationException("Only ExprExpr predicates are supported.");

				if (exprExpr.Expr1.CanBeNullable(nullabilityContext) && exprExpr.Expr2.CanBeNullable(nullabilityContext))
				{
					throw new InvalidOperationException("Both expressions can be nullable.");
				}
			}
		}

		[Test]
		public void FullFourth()
		{
			var ms = new MappingSchema();

			var table1 = CreateTable<FirstTable>(ms);
			var table2 = CreateTable<SecondTable>(ms);
			var table3 = CreateTable<ThirdTable>(ms);
			var table4 = CreateTable<FourthTable>(ms);

			var selectQuery = new SelectQuery();

			var ts1   = new SqlTableSource(table1, "t2");
			var ts2   = new SqlTableSource(table2, "t2");
			var ts3   = new SqlTableSource(table3, "t3");
			var ts4   = new SqlTableSource(table4, "t4");

			selectQuery.From.Tables.Add(ts1);

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, ts2, false, new SqlSearchCondition()
				.AddEqual(GetField<FirstTable>(table1, t => t.SecondId), GetField<SecondTable>(table2, t => t.SecondId), CompareNulls.LikeSql)));

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, ts3, false, new SqlSearchCondition()
				.AddEqual(GetField<SecondTable>(table2, t => t.ThirdId), GetField<ThirdTable>(table3, t => t.ThirdId), CompareNulls.LikeSql)));

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Full, ts4, false, new SqlSearchCondition()
				.AddEqual(GetField<ThirdTable>(table3, t => t.FourthId), GetField<FourthTable>(table4, t => t.FourthId), CompareNulls.LikeSql)));

			var nullabilityContext = NullabilityContext.GetContext(selectQuery);

			Console.WriteLine(GenerateSQL(selectQuery));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(nullabilityContext.CanBeNullSource(ts1.Source), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(ts2.Source), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(ts3.Source), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(ts4.Source), Is.True);
			}

			AssertJoinNotNullable(nullabilityContext, ts1.Joins[0]);
			AssertJoinNotNullable(nullabilityContext, ts1.Joins[1]);
			AssertJoinNotNullable(nullabilityContext, ts1.Joins[2]);
		}

		[Test]
		public void FullFourthSubqueries()
		{
			var ms = new MappingSchema();

			var table1 = CreateTable<FirstTable>(ms);
			var table2 = CreateTable<SecondTable>(ms);
			var table3 = CreateTable<ThirdTable>(ms);
			var table4 = CreateTable<FourthTable>(ms);

			var selectQuery = new SelectQuery();

			var qs1 = new SelectQuery();
			var qs2 = new SelectQuery();
			var qs3 = new SelectQuery();
			var qs4 = new SelectQuery();

			qs1.From.Table(table1);
			qs2.From.Table(table2);
			qs3.From.Table(table3);
			qs4.From.Table(table4);

			var qts1 = new SqlTableSource(qs1, "s1");
			var qts2 = new SqlTableSource(qs2, "s2");
			var qts3 = new SqlTableSource(qs3, "s3");
			var qts4 = new SqlTableSource(qs4, "s4");

			selectQuery.From.Tables.Add(qts1);

			qts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, qts2, false, new SqlSearchCondition()
				.AddEqual(GetField<FirstTable>(table1, t => t.SecondId), GetField<SecondTable>(table2, t => t.SecondId), CompareNulls.LikeSql)));

			qts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, qts3, false, new SqlSearchCondition()
				.AddEqual(GetField<SecondTable>(table2, t => t.ThirdId), GetField<ThirdTable>(table3, t => t.ThirdId), CompareNulls.LikeSql)));

			qts1.Joins.Add(new SqlJoinedTable(JoinType.Full, qts4, false, new SqlSearchCondition()
				.AddEqual(GetField<ThirdTable>(table3, t => t.FourthId), GetField<FourthTable>(table4, t => t.FourthId), CompareNulls.LikeSql)));

			var nullabilityContext = NullabilityContext.GetContext(selectQuery);

			Console.WriteLine(GenerateSQL(selectQuery));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(nullabilityContext.CanBeNullSource(table1), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(table2), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(table3), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(table4), Is.True);
			}

			AssertJoinNotNullable(nullabilityContext, qts1.Joins[0]);
			AssertJoinNotNullable(nullabilityContext, qts1.Joins[1]);
			AssertJoinNotNullable(nullabilityContext, qts1.Joins[2]);
		}

		[Test]
		public void FullThird()
		{
			var ms = new MappingSchema();

			var table1 = CreateTable<FirstTable>(ms);
			var table2 = CreateTable<SecondTable>(ms);
			var table3 = CreateTable<ThirdTable>(ms);
			var table4 = CreateTable<FourthTable>(ms);

			var selectQuery = new SelectQuery();

			var ts1 = new SqlTableSource(table1, "t2");
			var ts2 = new SqlTableSource(table2, "t2");
			var ts3 = new SqlTableSource(table3, "t3");
			var ts4 = new SqlTableSource(table4, "t4");

			selectQuery.From.Tables.Add(ts1);

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, ts2, false, new SqlSearchCondition()
				.AddEqual(GetField<FirstTable>(table1, t => t.SecondId), GetField<SecondTable>(table2, t => t.SecondId), CompareNulls.LikeSql)));

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Full, ts3, false, new SqlSearchCondition()
				.AddEqual(GetField<SecondTable>(table2, t => t.ThirdId), GetField<ThirdTable>(table3, t => t.ThirdId), CompareNulls.LikeSql)));

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, ts4, false, new SqlSearchCondition()
				.AddEqual(GetField<ThirdTable>(table3, t => t.FourthId), GetField<FourthTable>(table4, t => t.FourthId), CompareNulls.LikeSql)));

			var nullabilityContext = NullabilityContext.GetContext(selectQuery);

			Console.WriteLine(GenerateSQL(selectQuery));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(nullabilityContext.CanBeNullSource(ts1.Source), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(ts2.Source), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(ts3.Source), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(ts4.Source), Is.False);
			}

			AssertJoinNotNullable(nullabilityContext, ts1.Joins[0]);
			AssertJoinNotNullable(nullabilityContext, ts1.Joins[1]);
			AssertJoinNotNullable(nullabilityContext, ts1.Joins[2]);
		}

		[Test]
		public void FullFullThirdSubqueries()
		{
			var ms = new MappingSchema();

			var table1 = CreateTable<FirstTable>(ms);
			var table2 = CreateTable<SecondTable>(ms);
			var table3 = CreateTable<ThirdTable>(ms);
			var table4 = CreateTable<FourthTable>(ms);

			var selectQuery = new SelectQuery();

			var qs1 = new SelectQuery();
			var qs2 = new SelectQuery();
			var qs3 = new SelectQuery();
			var qs4 = new SelectQuery();

			qs1.From.Table(table1);
			qs2.From.Table(table2);
			qs3.From.Table(table3);
			qs4.From.Table(table4);

			var qts1 = new SqlTableSource(qs1, "s1");
			var qts2 = new SqlTableSource(qs2, "s2");
			var qts3 = new SqlTableSource(qs3, "s3");
			var qts4 = new SqlTableSource(qs4, "s4");

			selectQuery.From.Tables.Add(qts1);

			qts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, qts2, false, new SqlSearchCondition()
				.AddEqual(GetField<FirstTable>(table1, t => t.SecondId), GetField<SecondTable>(table2, t => t.SecondId), CompareNulls.LikeSql)));

			qts1.Joins.Add(new SqlJoinedTable(JoinType.Full, qts3, false, new SqlSearchCondition()
				.AddEqual(GetField<SecondTable>(table2, t => t.ThirdId), GetField<ThirdTable>(table3, t => t.ThirdId), CompareNulls.LikeSql)));

			qts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, qts4, false, new SqlSearchCondition()
				.AddEqual(GetField<ThirdTable>(table3, t => t.FourthId), GetField<FourthTable>(table4, t => t.FourthId), CompareNulls.LikeSql)));

			var nullabilityContext = NullabilityContext.GetContext(selectQuery);

			Console.WriteLine(GenerateSQL(selectQuery));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(nullabilityContext.CanBeNullSource(table1), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(table2), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(table3), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(table4), Is.False);
			}

			AssertJoinNotNullable(nullabilityContext, qts1.Joins[0]);
			AssertJoinNotNullable(nullabilityContext, qts1.Joins[1]);
			AssertJoinNotNullable(nullabilityContext, qts1.Joins[2]);
		}

		[Test]
		public void RightThird()
		{
			var ms = new MappingSchema();

			var table1 = CreateTable<FirstTable>(ms);
			var table2 = CreateTable<SecondTable>(ms);
			var table3 = CreateTable<ThirdTable>(ms);
			var table4 = CreateTable<FourthTable>(ms);

			var selectQuery = new SelectQuery();

			var ts1 = new SqlTableSource(table1, "t2");
			var ts2 = new SqlTableSource(table2, "t2");
			var ts3 = new SqlTableSource(table3, "t3");
			var ts4 = new SqlTableSource(table4, "t4");

			selectQuery.From.Tables.Add(ts1);

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, ts2, false, new SqlSearchCondition()
				.AddEqual(GetField<FirstTable>(table1, t => t.SecondId), GetField<SecondTable>(table2, t => t.SecondId), CompareNulls.LikeSql)));

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Right, ts3, false, new SqlSearchCondition()
				.AddEqual(GetField<SecondTable>(table2, t => t.ThirdId), GetField<ThirdTable>(table3, t => t.ThirdId), CompareNulls.LikeSql)));

			ts1.Joins.Add(new SqlJoinedTable(JoinType.Inner, ts4, false, new SqlSearchCondition()
				.AddEqual(GetField<ThirdTable>(table3, t => t.FourthId), GetField<FourthTable>(table4, t => t.FourthId), CompareNulls.LikeSql)));

			var nullabilityContext = NullabilityContext.GetContext(selectQuery);

			Console.WriteLine(GenerateSQL(selectQuery));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(nullabilityContext.CanBeNullSource(ts1.Source), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(ts2.Source), Is.True);
				Assert.That(nullabilityContext.CanBeNullSource(ts3.Source), Is.False);
				Assert.That(nullabilityContext.CanBeNullSource(ts4.Source), Is.False);
			}

			AssertJoinNotNullable(nullabilityContext, ts1.Joins[0]);
			AssertJoinNotNullable(nullabilityContext, ts1.Joins[1]);
			AssertJoinNotNullable(nullabilityContext, ts1.Joins[2]);
		}

	}
}
