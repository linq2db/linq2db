using System.Threading.Tasks;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

using Verify = Tests.Analyzers.AnalyzerVerifier<LinqToDB.Analyzers.DurationComparisonAnalyzer>;

namespace Tests.Analyzers
{
	[TestFixture]
	public sealed class DurationComparisonAnalyzerTests
	{
		const string Model = """
			using System;
			using System.Collections.Generic;
			using System.Linq;

			using LinqToDB;
			using LinqToDB.Mapping;

			class Row
			{
				[Column, Duration(DurationUnit.Second)]      public TimeSpan  InSeconds  { get; set; }
				[Column, Duration(DurationUnit.Millisecond)] public TimeSpan  InMillis   { get; set; }
				[Column, Duration(DurationUnit.Tick)]        public TimeSpan  InTicks    { get; set; }
				[Column, Duration(DurationUnit.Nanosecond)]  public TimeSpan  InNanos    { get; set; }
				[Column, Duration(DurationUnit.Second)]      public TimeSpan? Grace      { get; set; }
				[Column]                                     public TimeSpan  Undeclared { get; set; }

				[Column, Duration(DurationUnit.Second)]      public TimeSpan  SecondsField;
			}
			""";

		// Every snippet shares one host method, so a test body is just the statements under analysis.
		static string Source(string statements)
		{
			return Model
				+ "\n\nclass C\n{\n\tvoid M(IQueryable<Row> q, List<Row> list, TimeSpan arbitrary)\n\t{\n"
				+ statements
				+ "\n\t}\n}\n";
		}

		static DiagnosticResult Expected(string member, string unit, string value, string outcome)
		{
			return new DiagnosticResult(DurationComparisonAnalyzer.DiagnosticId, DiagnosticSeverity.Info)
				.WithLocation(0)
				.WithArguments(member, unit, value, outcome);
		}

		#region Positives

		[Test]
		public Task ReportsFractionalSecondsOnSecondColumn()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == TimeSpan.FromSeconds(1.5)|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsInequalityAsAlwaysMatching()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds != TimeSpan.FromSeconds(1.5)|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "always matches"));
		}

		[Test]
		public Task ReportsNullableInequalityWithoutClaimingEveryRow()
		{
			// A NULL column is excluded by '!=' unless CompareNulls.LikeClr is in force, so the message must not
			// promise that every row matches.
			var source = Source("""
						q.Where(r => {|#0:r.Grace != TimeSpan.FromSeconds(1.5)|});
				""");

			return Verify.VerifyAsync(source, Expected("Grace", "seconds", "00:00:01.5000000", "excludes no row that has a value"));
		}

		[Test]
		public Task ReportsNullableMember()
		{
			var source = Source("""
						q.Where(r => {|#0:r.Grace == TimeSpan.FromSeconds(1.5)|});
				""");

			return Verify.VerifyAsync(source, Expected("Grace", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsMirroredOperandOrder()
		{
			var source = Source("""
						q.Where(r => {|#0:TimeSpan.FromSeconds(1.5) == r.InSeconds|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsConstructorConstant()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == new TimeSpan(0, 0, 0, 1, 500)|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsTicksConstantOnMillisecondColumn()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InMillis == TimeSpan.FromTicks(15000001)|});
				""");

			return Verify.VerifyAsync(source, Expected("InMillis", "milliseconds", "00:00:01.5000001", "can never match"));
		}

		[Test]
		public Task ReportsParsedConstant()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == TimeSpan.Parse("00:00:01.5")|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsMaxValue()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == TimeSpan.MaxValue|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "10675199.02:48:05.4775807", "can never match"));
		}

		[Test]
		public Task ReportsAddedConstants()
		{
			// The corpus's clearest real instance of the defect is written this way, so folding '+' is load-bearing
			// rather than a nicety.
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == TimeSpan.FromMinutes(15) + TimeSpan.FromMilliseconds(500)|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:15:00.5000000", "can never match"));
		}

		[Test]
		public Task ReportsField()
		{
			var source = Source("""
						q.Where(r => {|#0:r.SecondsField == TimeSpan.FromSeconds(1.5)|});
				""");

			return Verify.VerifyAsync(source, Expected("SecondsField", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsSingleAssignmentLocal()
		{
			var source = Source("""
						var bound = TimeSpan.FromSeconds(1.5);
						q.Where(r => {|#0:r.InSeconds == bound|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsForEachWhenEveryCandidateFails()
		{
			var source = Source("""
						foreach (var bound in new[] { TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(2.5) })
							q.Where(r => {|#0:r.InSeconds == bound|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsForEachMixedCandidatesWithQualifiedWording()
		{
			// One iteration is degenerate and the other is fine, so the message must not claim the comparison can
			// never match at all.
			var source = Source("""
						foreach (var bound in new[] { TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(2) })
							q.Where(r => {|#0:r.InSeconds == bound|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match when that value is compared"));
		}

		[Test]
		public Task ReportsRangeVariable()
		{
			var source = Source("""
						var bounds = new[] { TimeSpan.FromSeconds(1.5) };
						bounds.Select(d => q.Where(r => {|#0:r.InSeconds == d|})).ToList();
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsInsideNestedNonExpressionLambda()
		{
			// The inner lambda is converted to Func<>, not Expression<>, so the gate has to check every enclosing
			// lambda rather than the nearest one.
			var source = Source("""
						q.Where(r => new[] { r }.Any(x => {|#0:x.InSeconds == TimeSpan.FromSeconds(1.5)|}));
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsNamedUnitArgumentOverConstructorArgument()
		{
			// Unit is settable as well as a constructor parameter, and the named assignment runs last.
			const string source = """
				using System;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.Mapping;

				class Named
				{
					[Column, Duration(DurationUnit.Second, Unit = DurationUnit.Hour)] public TimeSpan Elapsed { get; set; }
				}

				class C
				{
					void M(IQueryable<Named> q)
					{
						q.Where(r => {|#0:r.Elapsed == TimeSpan.FromSeconds(1.5)|});
					}
				}
				""";

			return Verify.VerifyAsync(source, Expected("Elapsed", "hours", "00:00:01.5000000", "can never match"));
		}

		#endregion

		#region Negatives

		[Test]
		public Task DoesNotReportRepresentableValue()
		{
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InSeconds == TimeSpan.FromSeconds(2));
				"""));
		}

		[Test]
		public Task DoesNotReportOrderingOperators()
		{
			// A bound the column cannot land on is already correct for an ordering operator: '> 1.5s' is exactly
			// '> 1s' for a column of whole seconds.
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InSeconds >  TimeSpan.FromSeconds(1.5));
						q.Where(r => r.InSeconds >= TimeSpan.FromSeconds(1.5));
						q.Where(r => r.InSeconds <  TimeSpan.FromSeconds(1.5));
						q.Where(r => r.InSeconds <= TimeSpan.FromSeconds(1.5));
				"""));
		}

		[Test]
		public Task DoesNotReportTickUnit()
		{
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InTicks == TimeSpan.FromSeconds(1.5));
				"""));
		}

		[Test]
		public Task DoesNotReportNanosecondUnit()
		{
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InNanos == TimeSpan.FromSeconds(1.5));
				"""));
		}

		[Test]
		public Task DoesNotReportUndeclaredMember()
		{
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.Undeclared == TimeSpan.FromSeconds(1.5));
				"""));
		}

		[Test]
		public Task DoesNotReportNonConstantOperand()
		{
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InSeconds == arbitrary);
				"""));
		}

		[Test]
		public Task DoesNotReportOutsideExpressionTree()
		{
			// An in-memory comparison against an object that never came from the database is legitimate.
			return Verify.VerifyAsync(Source("""
						list.Where(r => r.InSeconds == TimeSpan.FromSeconds(1.5)).ToList();
				"""));
		}

		[Test]
		public Task DoesNotReportPlainStatement()
		{
			return Verify.VerifyAsync(Source("""
						var row = new Row();
						if (row.InSeconds == TimeSpan.FromSeconds(1.5))
							return;
				"""));
		}

		[Test]
		public Task DoesNotReportNullConstant()
		{
			// The row is excluded for null semantics, not for representability - and a null must not fold to zero
			// ticks, which every unit can represent and would be silent for the wrong reason.
			return Verify.VerifyAsync(Source("""
						TimeSpan? absent = null;
						q.Where(r => r.Grace == absent);
				"""));
		}

		[Test]
		public Task DoesNotReportZero()
		{
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InSeconds == TimeSpan.Zero);
				"""));
		}

		[Test]
		public Task DoesNotReportSubMillisecondDoubleFactory()
		{
			// .NET Framework rounds the product to a whole millisecond, which lands this on a representable value,
			// so the three runtime readings do not agree and nothing is reported.
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InMillis == TimeSpan.FromMilliseconds(0.5));
				"""));
		}

		[Test]
		public Task DoesNotReportForEachWhenEveryCandidateIsRepresentable()
		{
			// The control that separates "the candidate set is evaluated" from "the candidate set is merely
			// resolved" - without it, an analyzer reporting on any resolvable foreach passes the whole fixture.
			return Verify.VerifyAsync(Source("""
						foreach (var bound in new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) })
							q.Where(r => r.InSeconds == bound);
				"""));
		}

		[Test]
		public Task DoesNotReportTwoParameterLambdaParameter()
		{
			// 'b' comes from the second sequence, so resolving the invocation's first argument would report a
			// duration this predicate never compares.
			return Verify.VerifyAsync(Source("""
						var wrong = new[] { TimeSpan.FromSeconds(1.5) };
						var right = new[] { TimeSpan.FromSeconds(2) };
						wrong.Zip(right, (a, b) => q.Where(r => r.InSeconds == b)).ToList();
				"""));
		}

		[Test]
		public Task DoesNotReportInnerKeySelectorParameter()
		{
			// The shape an argument-position rule cannot tell from a Select: 'b' is a one-parameter lambda in a
			// later argument, but it ranges over the *second* sequence. Resolving the invocation's first argument
			// would report 1.5 seconds, which this predicate never compares - hence an operator allowlist.
			return Verify.VerifyAsync(Source("""
						var wrong = new[] { TimeSpan.FromSeconds(1.5) };
						var right = new[] { TimeSpan.FromSeconds(2) };
						wrong.Join(right, a => 1, b => q.Where(r => r.InSeconds == b).Count(), (a, b) => a).ToList();
				"""));
		}

		[Test]
		public Task DoesNotReportDeconstructionReassignedLocal()
		{
			return Verify.VerifyAsync(Source("""
						var bound = TimeSpan.FromSeconds(1.5);
						var other = 0;
						(bound, other) = (TimeSpan.FromSeconds(2), 1);
						q.Where(r => r.InSeconds == bound);
				"""));
		}

		[Test]
		public Task DoesNotReportConfigurationScopedDeclarationOnly()
		{
			// Under a configuration the attribute does not name, the column has no duration semantics at all.
			const string source = """
				using System;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.Mapping;

				class Scoped
				{
					[Column, Duration(DurationUnit.Second, Configuration = "SqlServer")] public TimeSpan Elapsed { get; set; }
				}

				class C
				{
					void M(IQueryable<Scoped> q)
					{
						q.Where(r => r.Elapsed == TimeSpan.FromSeconds(1.5));
					}
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task DoesNotReportWhenDeclaredUnitsDisagree()
		{
			// A column of milliseconds holds 1.5 seconds exactly, so the comparison genuinely matches under that
			// configuration and reporting it would be a false positive.
			const string source = """
				using System;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.Mapping;

				class Mixed
				{
					[Column]
					[Duration(DurationUnit.Second)]
					[Duration(DurationUnit.Millisecond, Configuration = "SqlServer")]
					public TimeSpan Elapsed { get; set; }
				}

				class C
				{
					void M(IQueryable<Mixed> q)
					{
						q.Where(r => r.Elapsed == TimeSpan.FromSeconds(1.5));
					}
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task DoesNotReportWithoutLinqToDBReference()
		{
			// The capability gate: with no linq2db in the compilation the rule must be silent and must not throw.
			const string source = """
				using System;

				class C
				{
					bool M(TimeSpan value) => value == TimeSpan.FromSeconds(1.5);
				}
				""";

			return Verify.VerifyWithoutLinqToDBAsync(source);
		}

		#endregion
	}
}
