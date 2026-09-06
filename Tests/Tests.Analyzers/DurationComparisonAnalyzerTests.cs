using System;
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
				[Column, Duration(DurationUnit.Minute)]      public TimeSpan  InMinutes  { get; set; }
				[Column, Duration(DurationUnit.Second)]      public TimeSpan  InSeconds  { get; set; }
				[Column, Duration(DurationUnit.Millisecond)] public TimeSpan  InMillis   { get; set; }
				[Column, Duration(DurationUnit.Microsecond)] public TimeSpan  InMicros   { get; set; }
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
		public Task ReportsTicksConstructorConstant()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InMillis == new TimeSpan(15000001)|});
				""");

			return Verify.VerifyAsync(source, Expected("InMillis", "milliseconds", "00:00:01.5000001", "can never match"));
		}

		[Test]
		public Task ReportsThreeArgumentConstructorConstant()
		{
			// Against a minute column, so the seconds slot decides the verdict: a transposed arity would report a
			// different duration rather than the same one.
			var source = Source("""
						q.Where(r => {|#0:r.InMinutes == new TimeSpan(0, 0, 90)|});
				""");

			return Verify.VerifyAsync(source, Expected("InMinutes", "minutes", "00:01:30", "can never match"));
		}

		[Test]
		public Task ReportsFourArgumentConstructorConstant()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InMinutes == new TimeSpan(0, 0, 1, 30)|});
				""");

			return Verify.VerifyAsync(source, Expected("InMinutes", "minutes", "00:01:30", "can never match"));
		}

		[Test]
		public Task ReportsSixArgumentConstructorConstant()
		{
			// The microseconds slot, which only the six-argument arity has.
			var source = Source("""
						q.Where(r => {|#0:r.InMillis == new TimeSpan(0, 0, 0, 0, 0, 1500)|});
				""");

			return Verify.VerifyAsync(source, Expected("InMillis", "milliseconds", "00:00:00.0015000", "can never match"));
		}

		[Test]
		public Task ReportsSubtractedConstants()
		{
			// The corpus shape at IntervalTranslationTests.Queries.cs:815, proven until now only by the dogfood.
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == TimeSpan.FromMinutes(15) - TimeSpan.FromMilliseconds(500)|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:14:59.5000000", "can never match"));
		}

		[Test]
		public Task ReportsNegatedConstant()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == -TimeSpan.FromSeconds(1.5)|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "-00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsParseExactConstant()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == TimeSpan.ParseExact("00:00:01.5000000", "c", null)|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsMinValue()
		{
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == TimeSpan.MinValue|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "-10675199.02:48:05.4775808", "can never match"));
		}

		[Test]
		public Task ReportsSubMicrosecondOnMicrosecondColumn()
		{
			// Written with FromTicks so the verdict depends on the microsecond ratio and nothing else: it is the one
			// row of the unit table typed by hand rather than read from TimeSpan.TicksPer*.
			var source = Source("""
						q.Where(r => {|#0:r.InMicros == TimeSpan.FromTicks(15)|});
				""");

			return Verify.VerifyAsync(source, Expected("InMicros", "microseconds", "00:00:00.0000015", "can never match"));
		}

		[Test]
		public Task ReportsMicrosecondFactoryConstant()
		{
			// TimeSpan.FromMicroseconds is .NET 7+, so .NET Framework's whole-millisecond rounding cannot be one of
			// this value's readings - carrying it would round 1.5us to zero, which every unit can represent.
			var source = Source("""
						q.Where(r => {|#0:r.InSeconds == TimeSpan.FromMicroseconds(1.5)|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:00.0000015", "can never match"));
		}

		[Test]
		public Task ReportsDoubleFactoryOnMillisecondColumn()
		{
			// The .NET Framework reading is a whole millisecond by construction, so carrying it on a target that
			// cannot be .NET Framework silenced every From*(double) constant on millisecond and microsecond columns.
			var source = Source("""
						q.Where(r => {|#0:r.InMillis == TimeSpan.FromSeconds(0.0015)|});
				""");

			return Verify.VerifyAsync(source, Expected("InMillis", "milliseconds", "00:00:00.0015000", "can never match"));
		}

		[Test]
		public Task ReportsSubMillisecondDoubleFactory()
		{
			// Diagnosed because the snippet targets .NET 8. The same value against the same column is silent on a
			// target that could be .NET Framework, where the product rounds to a whole millisecond - pinned by
			// DoesNotReportSubMillisecondDoubleFactoryOnNetStandard20, whose only differing factor is the
			// reference set.
			var source = Source("""
						q.Where(r => {|#0:r.InMillis == TimeSpan.FromMilliseconds(0.5)|});
				""");

			return Verify.VerifyAsync(source, Expected("InMillis", "milliseconds", "00:00:00.0005000", "can never match"));
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
		public Task ReportsForEachMixedCandidatesInequalityWording()
		{
			// The '!=' half of the mixed-candidate wording on a non-nullable member. With the two above and the two
			// unqualified forms, this closes the outcome matrix - operator x nullability x mixed-vs-total.
			var source = Source("""
						foreach (var bound in new[] { TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(2) })
							q.Where(r => {|#0:r.InSeconds != bound|});
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "always matches when that value is compared"));
		}

		[Test]
		public Task ReportsForEachMixedCandidatesNullableInequalityWording()
		{
			// The nullable '!=' cell: a NULL column is excluded by '!=' unless CompareNulls.LikeClr is in force, so
			// the message promises only the rows that have a value.
			var source = Source("""
						foreach (var bound in new[] { TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(2) })
							q.Where(r => {|#0:r.Grace != bound|});
				""");

			return Verify.VerifyAsync(source, Expected("Grace", "seconds", "00:00:01.5000000", "excludes no row that has a value when that value is compared"));
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
		public Task ReportsInAssignedExpressionTree()
		{
			// The predicate-builder position: the Expression<T> comes from the local's declaration rather than from
			// an argument conversion. Every other positive here reaches the gate as an argument, and the node shape
			// above the lambda is the one thing D-2 was decided on without being able to observe it.
			var source = Source("""
						System.Linq.Expressions.Expression<Func<Row, bool>> predicate = r => {|#0:r.InSeconds == TimeSpan.FromSeconds(1.5)|};
						q.Where(predicate);
				""");

			return Verify.VerifyAsync(source, Expected("InSeconds", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task ReportsInReturnedExpressionTree()
		{
			// The same question for a return position rather than an initializer.
			var source = Source("""
						q.Where(Predicate());

						static System.Linq.Expressions.Expression<Func<Row, bool>> Predicate() =>
							r => {|#0:r.InSeconds == TimeSpan.FromSeconds(1.5)|};
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
		public Task DoesNotReportParseExactWithStyles()
		{
			// AssumeNegative makes the runtime produce -1.5s while this rule parses without the styles argument and
			// gets +1.5s. Representability is sign-invariant so the verdict would survive, but the message would
			// name a duration the source never writes - so the overload is left alone instead of half-supported.
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InSeconds == TimeSpan.ParseExact("00:00:01.5000000", "c", null, System.Globalization.TimeSpanStyles.AssumeNegative));
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
		public Task DoesNotReportCapturedInstanceMember()
		{
			// 'probe' is captured, so linq2db evaluates probe.InSeconds client-side into a constant and the
			// comparison is ordinary CLR equality - which a row holding 1.5s does match.
			return Verify.VerifyAsync(Source("""
						var probe = new Row { InSeconds = TimeSpan.FromSeconds(1.5) };
						q.Where(r => probe.InSeconds == TimeSpan.FromSeconds(1.5));
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
		public Task DoesNotReportWholeMicrosecondOnMicrosecondColumn()
		{
			// Paired with ReportsSubMicrosecondOnMicrosecondColumn: 20 ticks against 15, so a wrong ratio fails one
			// side or the other - 1 would silence the positive, 100 would redden this.
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InMicros == TimeSpan.FromTicks(20));
				"""));
		}

		[Test]
		public Task DoesNotReportRepresentableMicrosecondFactory()
		{
			// Paired with ReportsMicrosecondFactoryConstant: same factory, same column, only the value varies.
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InSeconds == TimeSpan.FromMicroseconds(2000000));
				"""));
		}

		[Test]
		public Task DoesNotReportRepresentableDoubleFactoryOnMillisecondColumn()
		{
			// Paired with ReportsDoubleFactoryOnMillisecondColumn: same factory, same column, only the value varies.
			return Verify.VerifyAsync(Source("""
						q.Where(r => r.InMillis == TimeSpan.FromSeconds(0.002));
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
		public Task DoesNotReportForEachOverCollectionExpression()
		{
			// Characterization, not a decision about the syntax: Roslyn 4.8 - the line this analyzer is pinned to,
			// matching the lowest supported SDK - exposes neither ICollectionExpressionOperation nor
			// OperationKind.CollectionExpression, so [..] cannot be recognized here while the same elements written
			// new[] { .. } are reported. Recall-only, never a wrong report. Should this go red, the pin was raised
			// and the arm can be added - update the wiki's recognised-constants list with it.
			return Verify.VerifyAsync(Source("""
						TimeSpan[] bounds = [TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(2.5)];

						foreach (var bound in bounds)
							q.Where(r => r.InSeconds == bound);
				"""));
		}

		[Test]
		public Task DoesNotReportForEachOverListInitializer()
		{
			// Paired with ReportsForEachWhenEveryCandidateFails: same elements, only the collection kind varies.
			// A List<T> can be mutated after it is built and nothing here can see that happen, so it is refused.
			return Verify.VerifyAsync(Source("""
						foreach (var bound in new List<TimeSpan> { TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(2.5) })
							q.Where(r => r.InSeconds == bound);
				"""));
		}

		[Test]
		public Task DoesNotReportForEachOverMethodCall()
		{
			// The other half of the same refusal: a sequence this rule cannot read at all.
			return Verify.VerifyAsync(Source("""
						foreach (var bound in Bounds())
							q.Where(r => r.InSeconds == bound);

						static IEnumerable<TimeSpan> Bounds() => new[] { TimeSpan.FromSeconds(1.5) };
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
		public Task DoesNotReportResultSelectorParameterOfAllowlistedOperator()
		{
			// The shape where the single-parameter rule is the only guard: SelectMany *is* on the operator
			// allowlist, so without it the walk resolves argument zero and reports 'wrong''s 1.5s - a duration
			// this predicate never compares. The Zip control above cannot show that, being rejected by name.
			return Verify.VerifyAsync(Source("""
						var wrong = new[] { TimeSpan.FromSeconds(1.5) };
						var right = new[] { TimeSpan.FromSeconds(2) };
						wrong.SelectMany(a => right, (a, b) => q.Where(r => r.InSeconds == b)).ToList();
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
		public Task DoesNotReportLocalWrittenThroughRefAlias()
		{
			// The write targets the alias, so a sweep that enumerates write shapes never sees it and folds 1.5s
			// while the code compares 2s. Paired with ReportsSingleAssignmentLocal: only the alias varies.
			return Verify.VerifyAsync(Source("""
						var bound = TimeSpan.FromSeconds(1.5);
						ref var alias = ref bound;
						alias = TimeSpan.FromSeconds(2);
						q.Where(r => r.InSeconds == bound);
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
		public Task DoesNotReportDerivedAttributeThatScopesItselfToAConfiguration()
		{
			// Paired with the test above: same scoping, declared through a derived attribute that assigns
			// Configuration from its own constructor rather than through a named argument. Reading only named
			// arguments makes such an attribute look unscoped, which is what lets it stand as the
			// all-configurations fallback and turns a scoped declaration into a report.
			const string source = """
				using System;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.Mapping;

				sealed class ScopedDurationAttribute : DurationAttribute
				{
					public ScopedDurationAttribute(DurationUnit unit, string configuration) : base(unit)
					{
						Configuration = configuration;
					}
				}

				class Scoped
				{
					[Column, ScopedDuration(DurationUnit.Second, "SqlServer")] public TimeSpan Elapsed { get; set; }
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
		public Task DoesNotReportWhenDeclaredUnitsDisagreeAcrossAnOverride()
		{
			// The same disagreement as the test above, split across an override. [Duration] is Inherited and
			// linq2db concatenates a member's own attributes with its base's before picking one per configuration
			// (MappingAttributesCache.GetMappingAttributesTreeInternal), so the millisecond declaration is live
			// under SqlServer and holds 1.5 seconds exactly. Stopping at the first level carrying an attribute
			// sees only the second declaration and reports.
			const string source = """
				using System;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.Mapping;

				class BaseRow
				{
					[Column]
					[Duration(DurationUnit.Millisecond, Configuration = "SqlServer")]
					public virtual TimeSpan Elapsed { get; set; }
				}

				class DerivedRow : BaseRow
				{
					[Duration(DurationUnit.Second)]
					public override TimeSpan Elapsed { get; set; }
				}

				class C
				{
					void M(IQueryable<DerivedRow> q)
					{
						q.Where(r => r.Elapsed == TimeSpan.FromSeconds(1.5));
					}
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task ReportsThroughDerivedDurationAttribute()
		{
			// DurationAttribute is public and unsealed, and the mapping resolves it by assignability
			// (MappingSchema.GetAttribute<DurationAttribute>), so a derived attribute really does declare the unit.
			const string source = """
				using System;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.Mapping;

				sealed class MyDurationAttribute : DurationAttribute
				{
					public MyDurationAttribute(DurationUnit unit) : base(unit) { }
				}

				class Derived
				{
					[Column, MyDuration(DurationUnit.Second)] public TimeSpan Elapsed { get; set; }
				}

				class C
				{
					void M(IQueryable<Derived> q)
					{
						q.Where(r => {|#0:r.Elapsed == TimeSpan.FromSeconds(1.5)|});
					}
				}
				""";

			return Verify.VerifyAsync(source, Expected("Elapsed", "seconds", "00:00:01.5000000", "can never match"));
		}

		[Test]
		public Task DoesNotReportDerivedAttributeThatHardCodesItsUnit()
		{
			// The unit is baked into the derived constructor's base call, so it is not in the attribute application
			// and an analyzer cannot see it. Silent, which is the honest answer rather than a guess.
			const string source = """
				using System;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.Mapping;

				sealed class SecondsAttribute : DurationAttribute
				{
					public SecondsAttribute() : base(DurationUnit.Second) { }
				}

				class Derived
				{
					[Column, Seconds] public TimeSpan Elapsed { get; set; }
				}

				class C
				{
					void M(IQueryable<Derived> q)
					{
						q.Where(r => r.Elapsed == TimeSpan.FromSeconds(1.5));
					}
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task DoesNotReadAnUnrelatedDerivedArgumentAsAUnit()
		{
			// Recognizing derived attributes means the first constructor argument is no longer necessarily a
			// DurationUnit. Matching this 5 against the enum's numeric values would name a unit nobody wrote -
			// minutes, which cannot hold the compared 1.5 seconds, so dropping the guard turns this into a report.
			// The value matters: 2 is Microsecond, which holds 1.5 seconds exactly, so the test would pass with
			// the guard and without it.
			const string source = """
				using System;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.Mapping;

				sealed class TaggedDurationAttribute : DurationAttribute
				{
					public TaggedDurationAttribute(int tag) : base(DurationUnit.Second) { Tag = tag; }

					public int Tag { get; }
				}

				class Derived
				{
					[Column, TaggedDuration(5)] public TimeSpan Elapsed { get; set; }
				}

				class C
				{
					void M(IQueryable<Derived> q)
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

		// The whole-millisecond rounding reading is decided from symbol presence, so the only thing that can vary it
		// is the target framework. The snippet declares its own LinqToDB.Mapping types because the real assembly is
		// the net8.0 build and cannot be referenced from a lower reference set; the analyzer resolves them through
		// GetTypeByMetadataName either way.
		const string SubMillisecondModel = """
			using System;
			using System.Linq;

			using LinqToDB.Mapping;

			namespace LinqToDB.Mapping
			{
				internal enum DurationUnit { Nanosecond, Tick, Microsecond, Millisecond, Second, Minute, Hour, Day }

				internal sealed class DurationAttribute : Attribute
				{
					public DurationAttribute(DurationUnit unit) { Unit = unit; }

					public DurationUnit Unit { get; set; }
				}
			}

			class Row
			{
				[Duration(DurationUnit.Millisecond)] public TimeSpan InMillis { get; set; }
			}

			class C
			{
				void M(IQueryable<Row> q)
				{
					q.Where(r => r.InMillis == TimeSpan.FromMilliseconds(0.5));
				}
			}
			""";

		const string SubMillisecondComparison = "r.InMillis == TimeSpan.FromMilliseconds(0.5)";

		[Test]
		public Task ReportsSubMillisecondDoubleFactoryOnNet60()
		{
			// .NET 6 truncates the double tick product exactly as .NET 7 does - Interval(double, double) ->
			// IntervalFromDoubleTicks - so the rounding reading must not be carried here either. TimeSpan
			// .FromMicroseconds is absent on this target, so System.Half is what rules .NET Framework out;
			// dropping that half of the test reddens this and nothing else.
			var source = SubMillisecondModel.Replace(
				SubMillisecondComparison,
				"{|#0:" + SubMillisecondComparison + "|}",
				StringComparison.Ordinal);

			return Verify.VerifyWithoutLinqToDBAsync(
				source,
				ReferenceAssemblies.Net.Net60,
				Expected("InMillis", "milliseconds", "00:00:00.0005000", "can never match"));
		}

		[Test]
		public Task DoesNotReportSubMillisecondDoubleFactoryOnNetStandard20()
		{
			// Paired with the two above: same value, same column, only the reference set varies. netstandard2.0 can
			// run on .NET Framework, where 0.5ms really is one whole millisecond, so the value is representable
			// under a reading this target can produce and the rule must stay silent.
			return Verify.VerifyWithoutLinqToDBAsync(
				SubMillisecondModel,
				ReferenceAssemblies.NetStandard.NetStandard20);
		}

		#endregion
	}
}
