using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	/// <summary>
	/// Generated operator-precedence matrix: every cell is an expression whose correct SQL grouping gives a
	/// different result from the grouping a dialect would apply if the brackets were missing, evaluated on the
	/// server (<see cref="Sql.AsSql{T}(T)"/>) and compared with the .NET result.
	/// </summary>
	[TestFixture]
	public class OperatorPrecedenceTests : TestBase
	{
		// true: every cell logs a message-free verdict line into the baseline and the test passes, so two baseline
		// revisions can be compared cell by cell (master vs branch); false: every red cell fails the test.
		static bool RecordVerdicts => false;

		[Table]
		sealed class PrecedenceRow
		{
			[PrimaryKey]          public int    Id { get; set; }
			[Column]              public int    A  { get; set; }
			[Column]              public int    B  { get; set; }
			[Column]              public int    C  { get; set; }
			[Column]              public int?   N  { get; set; }
			[Column(Length = 10)] public string S  { get; set; } = null!;
		}

		static readonly PrecedenceRow[] Rows =
		[
			new() { Id = 1, A = 48, B = 4, C = 2, N = 3,    S = "x" },
			new() { Id = 2, A = 90, B = 6, C = 3, N = null, S = "y" },
			new() { Id = 3, A = 72, B = 6, C = 2, N = 1,    S = "z" },
		];

		// integer division semantics differ by provider (Access divides as floating point); a cell whose correct
		// grouping divides inexactly is skipped, so a red cell means grouping and nothing else
		sealed class ExactDivisionVisitor : ExpressionVisitor
		{
			public static readonly ExactDivisionVisitor Instance = new();

			static readonly MethodInfo _exactDivide = typeof(ExactDivisionVisitor).GetMethod(nameof(ExactDivide), BindingFlags.NonPublic | BindingFlags.Static)!;

			static int ExactDivide(int x, int y)
			{
				if (y == 0 || x % y != 0)
					throw new ArithmeticException("inexact division");

				return x / y;
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				if (node.NodeType == ExpressionType.Divide && node.Type == typeof(int))
					return Expression.Call(_exactDivide, Visit(node.Left), Visit(node.Right));

				return base.VisitBinary(node);
			}
		}

		sealed class Holder
		{
			public int Value = -5;
		}

		static readonly Holder _negativeHolder = new();

		static readonly ParameterExpression R = Expression.Parameter(typeof(PrecedenceRow), "r");

		static readonly Expression A = Expression.Property(R, nameof(PrecedenceRow.A));
		static readonly Expression B = Expression.Property(R, nameof(PrecedenceRow.B));
		static readonly Expression C = Expression.Property(R, nameof(PrecedenceRow.C));
		static readonly Expression N = Expression.Property(R, nameof(PrecedenceRow.N));
		static readonly Expression S = Expression.Property(R, nameof(PrecedenceRow.S));

		static readonly MethodInfo AsSqlMethod     = typeof(Sql).GetMethod(nameof(Sql.AsSql))!;
		static readonly MethodInfo AsNotNullMethod = typeof(Sql).GetMethod(nameof(Sql.AsNotNull))!;
		static readonly MethodInfo ConcatMethod    = typeof(string).GetMethod(nameof(string.Concat), [typeof(object), typeof(object)])!;
		static readonly MethodInfo JoinMethod      = typeof(string).GetMethod(nameof(string.Join), [typeof(string), typeof(object[])])!;
		static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
		static readonly MethodInfo PowMethod       = typeof(Math).GetMethod(nameof(Math.Pow))!;
		static readonly MethodInfo IntToStringMethod = typeof(int).GetMethod(nameof(int.ToString), Type.EmptyTypes)!;
		static readonly MethodInfo BetweenMethod   = typeof(Sql)
			.GetMethods()
			.Single(m => m.Name == nameof(Sql.Between) && !m.GetParameters()[0].ParameterType.IsGenericType)
			.MakeGenericMethod(typeof(int));
		static readonly MethodInfo ContainsMethod  = typeof(Enumerable)
			.GetMethods()
			.Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
			.MakeGenericMethod(typeof(int));
		static readonly MethodInfo DistinctMethod  = typeof(Sql)
			.GetMethods()
			.Single(m => m.Name == nameof(Sql.IsDistinctFrom) && m.GetParameters()[1].ParameterType == m.GetGenericArguments()[0])
			.MakeGenericMethod(typeof(int?));
		static readonly MethodInfo RunCellMethod   = typeof(OperatorPrecedenceTests).GetMethod(nameof(RunCell), BindingFlags.NonPublic | BindingFlags.Static)!;

		static readonly (string Name, ExpressionType Type)[] IntOps =
		[
			("Add", ExpressionType.Add),
			("Sub", ExpressionType.Subtract),
			("Mul", ExpressionType.Multiply),
			("Div", ExpressionType.Divide),
			("Mod", ExpressionType.Modulo),
			("And", ExpressionType.And),
			("Or",  ExpressionType.Or),
			("Xor", ExpressionType.ExclusiveOr),
		];

		static readonly (string Name, ExpressionType Type)[] ArithmeticOps =
		[
			("Add", ExpressionType.Add),
			("Sub", ExpressionType.Subtract),
			("Mul", ExpressionType.Multiply),
			("Div", ExpressionType.Divide),
		];

		// prefix kinds: unary minus, bitwise not, negative literal, inlined negative parameter
		static readonly (string Name, Func<Expression, Expression> Apply, bool Inline)[] Prefixes =
		[
			("Neg",      Expression.Negate,                                                                false),
			("Not",      Expression.Not,                                                                   false),
			("NegLit",   _ => Expression.Constant(-5),                                                     false),
			("NegParam", _ => Expression.Field(Expression.Constant(_negativeHolder), nameof(Holder.Value)), true),
		];

		[Sql.Expression("{0} + {1}", Precedence = Precedence.Additive,       ServerSideOnly = true)]
		static int TemplateAdd(int x, int y) => x + y;

		[Sql.Expression("{0} - {1}", Precedence = Precedence.Subtraction,    ServerSideOnly = true)]
		static int TemplateSub(int x, int y) => x - y;

		[Sql.Expression("{0} * {1}", Precedence = Precedence.Multiplicative, ServerSideOnly = true)]
		static int TemplateMul(int x, int y) => x * y;

		[Sql.Expression("{0} / {1}", Precedence = Precedence.Multiplicative, ServerSideOnly = true)]
		static int TemplateDiv(int x, int y) => x / y;

		// a precedence between two constants maps to the nearest one (Additive)
		[Sql.Expression("{0} + {1}", Precedence = Precedence.Additive + 1,   ServerSideOnly = true)]
		static int TemplateAddOffset(int x, int y) => x + y;

		// default (Primary) class: every operator argument is bracketed for it, as before the precedence table
		[Sql.Expression(ProviderName.PostgreSQL, "{0}::text", ServerSideOnly = true)]
		static string TemplateAsText(int x) => x.ToString(CultureInfo.InvariantCulture);

		// an operator template declared Primary, as a user would write it
		[Sql.Expression(ProviderName.PostgreSQL, "{0} AT TIME ZONE 'UTC'", ServerSideOnly = true)]
		static DateTime TemplateAtUtc(DateTime x) => x;

		static readonly (string Name, MethodInfo Method)[] Templates =
		[
			("TAdd",       typeof(OperatorPrecedenceTests).GetMethod(nameof(TemplateAdd),       BindingFlags.NonPublic | BindingFlags.Static)!),
			("TSub",       typeof(OperatorPrecedenceTests).GetMethod(nameof(TemplateSub),       BindingFlags.NonPublic | BindingFlags.Static)!),
			("TMul",       typeof(OperatorPrecedenceTests).GetMethod(nameof(TemplateMul),       BindingFlags.NonPublic | BindingFlags.Static)!),
			("TDiv",       typeof(OperatorPrecedenceTests).GetMethod(nameof(TemplateDiv),       BindingFlags.NonPublic | BindingFlags.Static)!),
			("TAddOffset", typeof(OperatorPrecedenceTests).GetMethod(nameof(TemplateAddOffset), BindingFlags.NonPublic | BindingFlags.Static)!),
		];

		public static readonly string[] Parents =
		[
			"Add", "Sub", "Mul", "Div", "Mod", "And", "Or", "Xor",
			"Neg", "Not", "Concat", "ToString", "Pow", "Predicate",
		];

		sealed record Cell(string Id, Expression Body, Expression? Misparse, bool InlineParameters = false);

		static Expression Op(ExpressionType type, Expression left, Expression right) => Expression.MakeBinary(type, left, right);

		static Expression Pre((string Name, Func<Expression, Expression> Apply, bool Inline) prefix, Expression operand) => prefix.Apply(operand);

		static Expression ToDouble(Expression e) => Expression.Convert(e, typeof(double));

		static Expression Box(Expression e) => Expression.Convert(e, typeof(object));

		static Expression Concat(Expression left, Expression right) => Expression.Call(ConcatMethod, Box(left), Box(right));

		static Expression Flag(Expression condition) => Expression.Condition(condition, Expression.Constant(1), Expression.Constant(0));

		static IEnumerable<Cell> CellsFor(string parent)
		{
			var binary = IntOps.FirstOrDefault(o => o.Name == parent);

			if (binary.Name != null)
			{
				var p = binary.Type;

				foreach (var (q, qt) in IntOps)
				{
					// left child: a Q b P c; a missing bracket reads as a Q (b P c)
					yield return new Cell($"{parent}({q}(a,b),c)", Op(p, Op(qt, A, B), C), Op(qt, A, Op(p, B, C)));
					// right child: a P b Q c; a missing bracket reads as (a P b) Q c
					yield return new Cell($"{parent}(a,{q}(b,c))", Op(p, A, Op(qt, B, C)), Op(qt, Op(p, A, B), C));
					// transparent wrapper around the child
					yield return new Cell($"{parent}(AsNotNull({q}(a,b)),c)", Op(p, Expression.Call(AsNotNullMethod.MakeGenericMethod(typeof(int)), Op(qt, A, B)), C), Op(qt, A, Op(p, B, C)));
					yield return new Cell($"{parent}(a,AsNotNull({q}(b,c)))", Op(p, A, Expression.Call(AsNotNullMethod.MakeGenericMethod(typeof(int)), Op(qt, B, C))), Op(qt, Op(p, A, B), C));
				}

				// a raw "/" template divides as floating point on some dialects (DuckDB), so it is no bitwise operand
				foreach (var (t, method) in Templates.Where(x => x.Name != "TDiv" || p is not (ExpressionType.And or ExpressionType.Or or ExpressionType.ExclusiveOr)))
				{
					// a template is opaque: its declared class decides how it groups as an operand
					yield return new Cell($"{parent}({t}(a,b),c)", Op(p, Expression.Call(method, A, B), C), null);
					yield return new Cell($"{parent}(a,{t}(b,c))", Op(p, A, Expression.Call(method, B, C)), null);
					// a template ending in a negative literal: the minus must not reach over the parent
					// (-6 divides every A exactly)
					yield return new Cell($"{parent}({t}(a,NegLit),c)", Op(p, Expression.Call(method, A, Expression.Constant(-6)), C), null);
				}

				foreach (var pre in Prefixes)
				{
					// prefix operand: P(a, -b) and P(-b, c); a prefix that reaches too far reads -(b P c)
					yield return new Cell($"{parent}(a,{pre.Name}(b))", Op(p, A, Pre(pre, B)), null, pre.Inline);
					yield return new Cell($"{parent}({pre.Name}(b),c)", Op(p, Pre(pre, B), C), pre.Name is "Neg" or "Not" ? Pre(pre, Op(p, B, C)) : null, pre.Inline);

					// triples: (a Q pre(b)) P c; a prefix that reaches over P reads a Q pre(b P c)
					foreach (var (q, qt) in IntOps)
					{
						var misparse = pre.Name is "Neg" or "Not" ? Op(qt, A, Pre(pre, Op(p, B, C))) : null;
						yield return new Cell($"{parent}({q}(a,{pre.Name}(b)),c)", Op(p, Op(qt, A, Pre(pre, B)), C), misparse, pre.Inline);
					}
				}

				yield break;
			}

			switch (parent)
			{
				case "Neg":
				case "Not":
				{
					var pre = Prefixes.Single(x => x.Name == parent);

					foreach (var (q, qt) in IntOps)
					{
						// pre(a Q b); a missing bracket reads pre(a) Q b
						yield return new Cell($"{parent}({q}(a,b))", Pre(pre, Op(qt, A, B)), Op(qt, Pre(pre, A), B));
						yield return new Cell($"{parent}(AsNotNull({q}(a,b)))", Pre(pre, Expression.Call(AsNotNullMethod.MakeGenericMethod(typeof(int)), Op(qt, A, B))), Op(qt, Pre(pre, A), B));
					}

					// prefix over a prefix: must never render as a "--" comment
					foreach (var inner in Prefixes)
						yield return new Cell($"{parent}({inner.Name}(b))", Pre(pre, Pre(inner, B)), null, inner.Inline);

					// prefix over a product with a negative literal on its left
					yield return new Cell($"{parent}(Mul(NegLit,b))", Pre(pre, Op(ExpressionType.Multiply, Expression.Constant(-5), B)), null);
					break;
				}

				case "Concat":
				{
					foreach (var (q, qt) in IntOps)
					{
						yield return new Cell($"Concat(s,{q}(a,b))",      Concat(S, Op(qt, A, B)), null);
						yield return new Cell($"Concat({q}(a,b),s)",      Concat(Op(qt, A, B), S), null);
						// the string column goes first: a non-string first item is a separate defect (#6200)
						yield return new Cell($"Join(s,{q}(b,c),c)",      Expression.Call(JoinMethod, Expression.Constant(","), Expression.NewArrayInit(typeof(object), Box(S), Box(Op(qt, B, C)), Box(C))), null);
						yield return new Cell($"Concat(s,Concat(s,{q}(a,b)))", Concat(S, Concat(S, Op(qt, A, B))), null);
					}

					foreach (var pre in Prefixes)
					{
						yield return new Cell($"Concat(s,{pre.Name}(a))", Concat(S, Pre(pre, A)), null, pre.Inline);
						yield return new Cell($"Concat({pre.Name}(a),s)", Concat(Pre(pre, A), S), null, pre.Inline);
					}

					break;
				}

				case "ToString":
				{
					foreach (var (q, qt) in IntOps)
						yield return new Cell($"ToString({q}(a,b))", Expression.Call(Op(qt, A, B), IntToStringMethod), null);

					foreach (var pre in Prefixes.Where(x => x.Name is "Neg" or "Not"))
						yield return new Cell($"ToString({pre.Name}(a))", Expression.Call(Pre(pre, A), IntToStringMethod), null);

					break;
				}

				case "Pow":
				{
					foreach (var (q, qt) in ArithmeticOps)
					{
						// base: (a Q b) ^ 2; a missing bracket reads a Q b ^ 2
						yield return new Cell($"Pow({q}(b,c),2)", Expression.Call(PowMethod, ToDouble(Op(qt, B, C)), Expression.Constant(2.0)), Op(qt, ToDouble(B), Expression.Call(PowMethod, ToDouble(C), Expression.Constant(2.0))));
						// exponent: c ^ (b Q c); a missing bracket reads c ^ b Q c
						yield return new Cell($"Pow(c,{q}(b,c))", Expression.Call(PowMethod, ToDouble(C), ToDouble(Op(qt, B, C))), Op(qt, Expression.Call(PowMethod, ToDouble(C), ToDouble(B)), ToDouble(C)));
						// power as a child of an arithmetic operator
						yield return new Cell($"{q}(Pow(b,2),c)", Op(qt, Expression.Call(PowMethod, ToDouble(B), Expression.Constant(2.0)), ToDouble(C)), null);
					}

					yield return new Cell("Pow(Neg(b),2)",         Expression.Call(PowMethod, ToDouble(Expression.Negate(B)), Expression.Constant(2.0)), Expression.Negate(Expression.Call(PowMethod, ToDouble(B), Expression.Constant(2.0))));
					yield return new Cell("Neg(Pow(b,2))",         Expression.Negate(Expression.Call(PowMethod, ToDouble(B), Expression.Constant(2.0))), null);
					yield return new Cell("Pow(c,Pow(c,c))",       Expression.Call(PowMethod, ToDouble(C), Expression.Call(PowMethod, ToDouble(C), ToDouble(C))), Expression.Call(PowMethod, Expression.Call(PowMethod, ToDouble(C), ToDouble(C)), ToDouble(C)));
					yield return new Cell("Pow(AsNotNull(Add(b,c)),2)", Expression.Call(PowMethod, ToDouble(Expression.Call(AsNotNullMethod.MakeGenericMethod(typeof(int)), Op(ExpressionType.Add, B, C))), Expression.Constant(2.0)), null);
					break;
				}

				case "Predicate":
				{
					foreach (var (q, qt) in IntOps)
					{
						yield return new Cell($"Eq({q}(a,b),c)",          Flag(Expression.Equal(Op(qt, A, B), C)), null);
						yield return new Cell($"Lt(c,{q}(a,b))",          Flag(Expression.LessThan(C, Op(qt, A, B))), null);
						yield return new Cell($"Between({q}(a,b),c,a)",   Flag(Expression.Call(BetweenMethod, Op(qt, A, B), C, A)), null);
						yield return new Cell($"Between(a,{q}(b,c),a)",  Flag(Expression.Call(BetweenMethod, A, Op(qt, B, C), A)), null);
						yield return new Cell($"In({q}(a,b))",            Flag(Expression.Call(ContainsMethod, Expression.Constant(new[] { 52, 96, 2, 192, 15, 12, 0, 4, 1 }), Op(qt, A, B))), null);
						yield return new Cell($"IsNull({q}(a,n))",        Flag(Expression.Equal(Expression.MakeBinary(qt, Expression.Convert(A, typeof(int?)), N), Expression.Constant(null, typeof(int?)))), null);
						yield return new Cell($"Like(Concat(s,{q}(a,b)))", Flag(Expression.Call(Concat(S, Op(qt, A, B)), StartsWithMethod, Expression.Constant("x5"))), null);
						yield return new Cell($"NotAnd({q}(a,b))",        Flag(Expression.AndAlso(Expression.Not(Expression.GreaterThan(Op(qt, A, B), C)), Expression.LessThan(B, A))), null);
						// a nullable operand keeps IS DISTINCT FROM from simplifying to <> (DECODE on Oracle)
						yield return new Cell($"Distinct({q}(a,b),n)",    Flag(Expression.Call(DistinctMethod, Expression.Convert(Op(qt, A, B), typeof(int?)), N)), null);
						yield return new Cell($"Distinct(n,{q}(a,b))",    Flag(Expression.Call(DistinctMethod, N, Expression.Convert(Op(qt, A, B), typeof(int?)))), null);
					}

					foreach (var pre in Prefixes)
					{
						yield return new Cell($"Eq({pre.Name}(a),c)", Flag(Expression.Equal(Pre(pre, A), C)), null, pre.Inline);
						yield return new Cell($"Eq(c,{pre.Name}(a))", Flag(Expression.Equal(C, Pre(pre, A))), null, pre.Inline);
					}

					break;
				}

				default:
					throw new InvalidOperationException($"Unknown parent family {parent}");
			}
		}

		enum Verdict { Green, Red, Error, Skipped }

		static object? Evaluate(Expression body, PrecedenceRow row)
		{
			var compiled = Expression
				.Lambda(body, R)
				.Compile();

			return compiled.DynamicInvoke(row);
		}

		// null when the cell cannot be evaluated in .NET (division by zero, overflow) or both groupings agree on every row
		static object?[]? ExpectedFor(Cell cell)
		{
			var expected = new object?[Rows.Length];
			var differs  = cell.Misparse == null;

			for (var i = 0; i < Rows.Length; i++)
			{
				try
				{
					expected[i] = Evaluate(ExactDivisionVisitor.Instance.Visit(cell.Body), Rows[i]);
				}
				catch (TargetInvocationException ex) when (ex.InnerException is ArithmeticException)
				{
					return null;
				}

				if (cell.Misparse != null && !differs)
				{
					try
					{
						differs = !Same(expected[i], Evaluate(cell.Misparse, Rows[i]));
					}
					catch (TargetInvocationException ex) when (ex.InnerException is ArithmeticException)
					{
						differs = true;
					}
				}
			}

			return differs ? expected : null;
		}

		static bool Same(object? x, object? y)
		{
			if (x is double dx && y is double dy)
				return Math.Abs(dx - dy) <= 1e-9 * Math.Max(1, Math.Max(Math.Abs(dx), Math.Abs(dy)));

			if (x is double || y is double)
				return Same(Convert.ToDouble(x, CultureInfo.InvariantCulture), Convert.ToDouble(y, CultureInfo.InvariantCulture));

			return Equals(x, y);
		}

		static object?[] RunCell<T>(ITable<PrecedenceRow> table, Expression body)
		{
			var selector = Expression.Lambda<Func<PrecedenceRow, T>>(Expression.Call(AsSqlMethod.MakeGenericMethod(typeof(T)), body), R);

			return table
				.OrderBy(r => r.Id)
				.Select(selector)
				.AsEnumerable()
				.Select(v => (object?)v)
				.ToArray();
		}

		// all cells of one batch in a single query, one column per cell; per row, the cell values in cell order
		static object?[][] RunBatch(ITable<PrecedenceRow> table, IReadOnlyList<Cell> cells)
		{
			var columns  = cells.Select(c => Box(Expression.Call(AsSqlMethod.MakeGenericMethod(c.Body.Type), c.Body)));
			var selector = Expression.Lambda<Func<PrecedenceRow, object?[]>>(Expression.NewArrayInit(typeof(object), columns), R);

			return table
				.OrderBy(r => r.Id)
				.Select(selector)
				.ToArray();
		}

		static Verdict Judge(object?[] actual, object?[] expected)
		{
			if (actual.Length != expected.Length)
				return Verdict.Red;

			var same = actual
				.Zip(expected, Same)
				.All(x => x);

			return same ? Verdict.Green : Verdict.Red;
		}

		[Test]
		public void Matrix([DataSources] string context, [ValueSource(nameof(Parents))] string parent)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			var failures = new List<string>();
			var verdicts = new Dictionary<Cell, Verdict>();
			var runnable = new List<(Cell Cell, object?[] Expected)>();

			foreach (var cell in CellsFor(parent))
			{
				var expected = ExpectedFor(cell);

				if (expected == null)
					verdicts[cell] = Verdict.Skipped;
				else
					runnable.Add((cell, expected));
			}

			var inline = db.InlineParameters;

			try
			{
				// one query per inline-parameters setting; when the combined query throws, every cell runs on its own
				foreach (var batch in runnable.GroupBy(x => x.Cell.InlineParameters))
				{
					db.InlineParameters = batch.Key;

					var cells = batch.ToList();

					var batchCells = cells
						.Select(x => x.Cell)
						.ToList();

					object?[][]? rows = null;

					try
					{
						rows = RunBatch(table, batchCells);
					}
					catch (Exception ex) when (ex is not AssertionException)
					{
						rows = null;
					}

					for (var i = 0; i < cells.Count; i++)
					{
						var (cell, expected) = cells[i];

						object?[] actual;

						if (rows != null)
						{
							actual = rows
								.Select(row => row[i])
								.ToArray();
						}
						else
						{
							try
							{
								actual = (object?[])RunCellMethod
									.MakeGenericMethod(cell.Body.Type)
									.Invoke(null, [table, cell.Body])!;
							}
							catch (TargetInvocationException ex)
							{
								verdicts[cell] = Verdict.Error;
								failures.Add($"{cell.Id}: {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}");
								continue;
							}
						}

						var verdict = Judge(actual, expected);
						verdicts[cell] = verdict;

						if (verdict == Verdict.Red)
							failures.Add($"{cell.Id}: expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
					}
				}
			}
			finally
			{
				db.InlineParameters = inline;
			}

			if (RecordVerdicts)
			{
				var verdictById = verdicts.ToDictionary(v => v.Key.Id, v => v.Value);

				foreach (var cell in CellsFor(parent))
				{
					var verdict = verdictById[cell.Id]
						.ToString()
						.ToLowerInvariant();

					BaselinesManager.LogQuery($"-- cell {parent}/{cell.Id}: {verdict}");
				}
			}

			if (!RecordVerdicts && failures.Count > 0)
				Assert.Fail(string.Join(Environment.NewLine, failures));

			if (RecordVerdicts && failures.Count > 0)
				TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures));
		}

		// Access binds parameters by position and names every occurrence apart, so an operand rendered twice (once to
		// classify it, once for real) would add a parameter the SQL does not reference in that order
		[Test]
		public void PositionalParametersRenderOnce([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			var p1 = 7;
			var p2 = 3;
			var p3 = -5;

			var query = table
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Neg  = Sql.AsSql(-(r.A + p1)),
					Cast = Sql.AsSql((long)(r.A * p2)),
					Diff = Sql.AsSql(-(r.A - Sql.ToSql(p3))),
				});

			var expected = Rows
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Neg  = -(r.A + p1),
					Cast = (long)(r.A * p2),
					Diff = -(r.A - p3),
				})
				.ToList();

			query
				.ToList()
				.ShouldBe(expected);

			var sql = query.ToSqlQuery();

			// OleDb names its placeholders, ODBC writes ?: both bind by position
			var placeholders = Regex
				.Matches(sql.Sql, @"@\w+|\?")
				.Cast<Match>()
				.Select(m => m.Value.TrimStart('@'))
				.ToList();

			var names = sql.Parameters
				.Select(p => p.Name!.TrimStart('@'))
				.ToList();

			names
				.Count
				.ShouldBe(2);

			placeholders
				.Count
				.ShouldBe(names.Count, sql.Sql);

			if (!placeholders.Contains("?"))
				placeholders.ShouldBe(names);
		}

		// the subquery brings its own parentheses; the prefix operator must not add a second pair
		[Test]
		public void PrefixOverScalarSubqueryHasOneParenthesisPair([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			var query = table
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Neg = Sql.AsSql(-table.Where(x => x.Id <= r.Id).Max(x => x.A)),
					Not = Sql.AsSql(~table.Where(x => x.Id <= r.Id).Max(x => x.B)),
				});

			var expected = Rows
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Neg = -Rows.Where(x => x.Id <= r.Id).Max(x => x.A),
					Not = ~Rows.Where(x => x.Id <= r.Id).Max(x => x.B),
				})
				.ToList();

			query
				.ToList()
				.ShouldBe(expected);

			var sql = query
				.ToSqlQuery()
				.Sql;

			Regex
				.IsMatch(sql, @"-\(\s*SELECT")
				.ShouldBeTrue(sql);

			Regex
				.IsMatch(sql, @"~\(\s*SELECT")
				.ShouldBeTrue(sql);

			Regex
				.IsMatch(sql, @"[-~]\(\s*\(\s*SELECT")
				.ShouldBeFalse(sql);
		}

		// two minus signs side by side start a comment: a prefix minus over a negative literal, an inlined negative
		// parameter or an operator chain that starts with one must keep them apart
		[Test]
		public void NegationNeverRendersAComment([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			db.InlineParameters = true;

			var p = -5;

			var query = table
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					NegatedProductOfNegative = Sql.AsSql(-(p * r.A)),
					NegatedProductByNegative = Sql.AsSql(-(r.A * p)),
					NegatedDifference        = Sql.AsSql(-(r.A - p)),
					NegatedLiteralPlus       = Sql.AsSql(-Sql.ToSql(p) + r.A),
					NegatedNegation          = Sql.AsSql(-(-r.A + r.B)),
				});

			var expected = Rows
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					NegatedProductOfNegative = -(p * r.A),
					NegatedProductByNegative = -(r.A * p),
					NegatedDifference        = -(r.A - p),
					NegatedLiteralPlus       = -p + r.A,
					NegatedNegation          = -(-r.A + r.B),
				})
				.ToList();

			query
				.ToList()
				.ShouldBe(expected);

			var sql = query
				.ToSqlQuery()
				.Sql;

			sql.ShouldNotContain("--", Case.Sensitive, sql);
		}

		// an inlined negative or exponent literal renders as a primary operand of a product and under a prefix minus
		[Test]
		public void NegativeLiteralOperands([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			var negativeDecimal = -5.5m;
			var negativeDouble  = -5.5d;
			var small           = 1e-7d;

			var productOfNegativeDecimal = table
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(r.A * Sql.ToSql(negativeDecimal)));

			AssertQueryWithinDelta(productOfNegativeDecimal);

			var negatedNegativeDecimal = table
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(-Sql.ToSql(negativeDecimal) * r.B));

			AssertQueryWithinDelta(negatedNegativeDecimal);

			var productOfNegativeDouble = table
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(r.A * Sql.ToSql(negativeDouble)));

			AssertQueryWithinDelta(productOfNegativeDouble);

			var productOfExponent = table
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(r.A * Sql.ToSql(small)));

			AssertQueryWithinDelta(productOfExponent);
		}

		// :: binds tighter than any operator: a Primary template keeps bracketing an operator argument, and the
		// DateDiff templates bracket a placeholder under :: whatever level its argument has
		[Test]
		public void PostfixCastKeepsTemplateArgumentsGrouped([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			var start = new DateTime(2020, 6, 15, 12, 0, 0);
			var end   = new DateTime(2023, 6, 15, 12, 0, 0);

			var query = table
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Text       = TemplateAsText(r.A + r.B),
					YearsToUtc = Sql.DateDiff(Sql.DateParts.Year, start, TemplateAtUtc(end.AddDays(r.C))),
					YearsOfUtc = Sql.DateDiff(Sql.DateParts.Year, TemplateAtUtc(start.AddDays(r.C)), end),
				});

			var expected = Rows
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Text       = (r.A + r.B).ToString(CultureInfo.InvariantCulture),
					YearsToUtc = (int?)3,
					YearsOfUtc = (int?)3,
				})
				.ToList();

			query
				.ToList()
				.ShouldBe(expected);
		}

		// a negative literal or an operator under a postfix :: cast keeps its brackets: -5.5::text reads -(5.5::text)
		[Test]
		public void PostfixCastKeepsNegativeOperandGrouped([IncludeDataSources(true, TestProvName.AllPostgreSQL, TestProvName.AllInformix)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			db.InlineParameters = true;

			var negativeDecimal = -5.5m;
			var negativeInt     = -5;

			var query = table
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					DecimalText = Sql.AsSql(Sql.ConvertTo<string>.From(Sql.ToSql(negativeDecimal))),
					IntText     = Sql.AsSql(Sql.ConvertTo<string>.From(Sql.ToSql(negativeInt))),
					NegatedText = Sql.AsSql(Sql.ConvertTo<string>.From(-r.C)),
					SumText     = Sql.AsSql(Sql.ConvertTo<string>.From(r.A + r.B)),
					ProductText = Sql.AsSql(Sql.ConvertTo<string>.From(r.A * r.B)),
				});

			var expected = Rows
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					DecimalText = "-5.5",
					IntText     = "-5",
					NegatedText = (-r.C).ToString(CultureInfo.InvariantCulture),
					SumText     = (r.A + r.B).ToString(CultureInfo.InvariantCulture),
					ProductText = (r.A * r.B).ToString(CultureInfo.InvariantCulture),
				})
				.ToList();

			query
				.ToList()
				.ShouldBe(expected);
		}

		[Table]
		sealed class ConcatRow
		{
			[PrimaryKey]          public int     Id { get; set; }
			[Column(Length = 10)] public string? S  { get; set; }
		}

		// a concatenation binds tighter than a comparison, so where the table orders the two it renders bare as an operand
		[Test]
		public void ConcatOperandOfComparisonRendersBare([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new ConcatRow[]
			{
				new() { Id = 1, S = "a"  },
				new() { Id = 2, S = null },
			});

			var value = "x";

			var query = table
				.Where(r => r.S + "x" == value)
				.Select(r => r.Id);

			query
				.ToList()
				.ShouldBe([2]);

			var sql = query
				.ToSqlQuery()
				.Sql;

			// the precedence of SQL Server 2025's || is undocumented, and before 9.5 PostgreSQL sorts every other operator
			// below IS, which shares the comparison level: both keep the concatenation bracketed
			var bracketed = context.IsAnyOf(TestProvName.AllSqlServer2025Plus, TestProvName.AllPostgreSQL93Minus);

			// the concatenation ends in its 'x' literal, so a closing bracket before = is one around it
			Regex
				.IsMatch(sql, @"\)\s*=")
				.ShouldBe(bracketed, sql);
		}

		// a provider rewrites a non-integer remainder to cast its dividend; the rebuilt % keeps its right operand grouped
		[Test]
		public void NonIntegerRemainderKeepsItsGrouping([IncludeDataSources(true, TestProvName.AllSqlServer, ProviderName.SqlCe, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			var query = table
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql((double)r.A % (r.B + r.C)));

			var expected = Rows
				.OrderBy(r => r.Id)
				.Select(r => (double)r.A % (r.B + r.C))
				.ToList();

			query
				.ToList()
				.ShouldBe(expected);
		}

		// PostgreSQL has no operator adding one timestamp to another: base + (end - start) keeps its brackets
		[Test]
		public void IntervalDifferenceKeepsItsGrouping([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			var start = new DateTime(2020, 1, 1);
			var end   = new DateTime(2020, 1, 11);

			var plain = table
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(start.AddDays(r.A) + (end.AddDays(r.B) - start)));

			plain
				.ToList()
				.ShouldBe(Rows
					.OrderBy(r => r.Id)
					.Select(r => start.AddDays(r.A) + (end.AddDays(r.B) - start))
					.ToList());

			// a shifted start is itself a sum: under the - it keeps its own brackets
			var shifted = table
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(start.AddDays(r.A) + (end.AddDays(r.B) - start.AddDays(r.C))));

			shifted
				.ToList()
				.ShouldBe(Rows
					.OrderBy(r => r.Id)
					.Select(r => start.AddDays(r.A) + (end.AddDays(r.B) - start.AddDays(r.C)))
					.ToList());
		}

		// clauses of the grouping rule that a value comparison cannot see: a right operand of the same operator keeps
		// its brackets unless the operator is associative, and a left-associative chain renders bare
		[Test]
		public void ShapesTheMatrixCannotReachRenderAsSpecified([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows);

			string SqlOf(Expression<Func<PrecedenceRow, int>> selector)
			{
				return table
					.Select(selector)
					.ToSqlQuery()
					.Sql;
			}

			SqlOf(r => Sql.AsSql(r.A + (r.B + r.C)))
				.ShouldContain("[r].[A] + ([r].[B] + [r].[C])");

			SqlOf(r => Sql.AsSql(r.A * (r.B * r.C)))
				.ShouldContain("[r].[A] * ([r].[B] * [r].[C])");

			SqlOf(r => Sql.AsSql(r.A & (r.B & r.C)))
				.ShouldContain("[r].[A] & [r].[B] & [r].[C]");

			SqlOf(r => Sql.AsSql(r.A + r.B - r.C))
				.ShouldContain("[r].[A] + [r].[B] - [r].[C]");

			SqlOf(r => Sql.AsSql(r.A - r.B - r.C))
				.ShouldContain("[r].[A] - [r].[B] - [r].[C]");

			SqlOf(r => Sql.AsSql(r.A * r.B * r.C))
				.ShouldContain("[r].[A] * [r].[B] * [r].[C]");

			SqlOf(r => Sql.AsSql(r.A / r.B * r.C))
				.ShouldContain("[r].[A] / [r].[B] * [r].[C]");
		}
	}
}
