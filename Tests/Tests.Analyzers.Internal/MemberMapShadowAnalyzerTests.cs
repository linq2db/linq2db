using System.Threading.Tasks;

using CodeGenerators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

namespace Tests.Analyzers.Internal
{
	[TestFixture]
	public class MemberMapShadowAnalyzerTests
	{
		// A miniature of the real model: Expressions with its M/N helpers, a common table registered under "" by
		// field, provider sections keyed by ProviderName constants, the registration extensions, a base translator in
		// the shared Translation namespace and one translator per provider namespace. Every placeholder is a slot a
		// fixture fills; the Math.Sin entry keeps the common table non-empty, so only the drift fixture that drops it
		// can reach the empty-model diagnostic.
		static string Source(
			string common    = "",
			string oracle    = "",
			string sqlite    = "",
			string sqliteMs  = "",
			string baseReg   = "",
			string oracleReg = "",
			string pgReg     = "",
			string sqliteReg = "",
			string rightAttr = "",
			string extra     = "",
			bool   sentinel  = true) => $$"""
			#nullable enable
			using System;
			using System.Collections.Generic;
			using System.Linq.Expressions;

			namespace LinqToDB
			{
				public static class ProviderName
				{
					public const string Oracle     = "Oracle";
					public const string PostgreSQL = "PostgreSQL";
					public const string SQLite     = "SQLite";
					public const string SQLiteMS   = "SQLite.MS";
				}

				public static partial class Sql
				{
					[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
					public class ExpressionAttribute : Attribute
					{
						public ExpressionAttribute(string? expression) { }
						public ExpressionAttribute(string configuration, string expression) { Configuration = configuration; }
						public string? Configuration { get; set; }
					}

					public class FunctionAttribute : ExpressionAttribute
					{
						public FunctionAttribute(string name) : base(name) { }
						public FunctionAttribute(string configuration, string name) : base(configuration, name) { }
					}

					{{rightAttr}}
					public static string? Right(string? str, int? length) => str;

					public static T Identity<T>(T value) => value;

					public static class ConvertTo<TTo>
					{
						public static TTo From<TFrom>(TFrom obj) => default!;
					}
				}
			}

			namespace LinqToDB.Linq
			{
				public interface IExpressionInfo { }
				public readonly struct MemberInfoWithType { }

				public static class Expressions
				{
					public static MemberInfoWithType M<T>(Expression<Func<T,object?>> func) => default;
					public static MemberInfoWithType M<T>(Expression<Func<T>> func) => default;
					public static IExpressionInfo N(Func<LambdaExpression?> func) => null!;

					// The public MapMember overloads forward a caller's lambda through M: not a table entry.
					public static void MapMember(Expression<Func<object>> memberInfo)
					{
						_ = M(memberInfo);
					}

					static readonly Dictionary<MemberInfoWithType,IExpressionInfo> _commonMembers = new()
					{
						{{(sentinel ? "{ M(() => Math.Sin(0)), N(() => null) }," : "")}}
						{{common}}
					};

					static Dictionary<string,Dictionary<MemberInfoWithType,IExpressionInfo>> LoadMembers()
					{
						return new Dictionary<string,Dictionary<MemberInfoWithType,IExpressionInfo>>
						{
							{ "", _commonMembers },
							{ ProviderName.Oracle,   new Dictionary<MemberInfoWithType,IExpressionInfo> { {{oracle}} } },
							{ ProviderName.SQLite,   new Dictionary<MemberInfoWithType,IExpressionInfo> { {{sqlite}} } },
							{ ProviderName.SQLiteMS, new Dictionary<MemberInfoWithType,IExpressionInfo> { {{sqliteMs}} } },
						};
					}

					{{extra}}
				}
			}

			namespace LinqToDB.Internal.DataProvider.Translation
			{
				public sealed class TranslationRegistration
				{
					public delegate object? TranslateFunc(object member);
				}

				public static class TranslationRegistrationExtensions
				{
					public static void RegisterMethod(this TranslationRegistration registration, Expression<Action> methodCallPattern, TranslationRegistration.TranslateFunc func, bool isGenericTypeMatch = false) { }
					public static void RegisterMethod<T,TResult>(this TranslationRegistration registration, Expression<Func<T,TResult>> methodCallPattern, TranslationRegistration.TranslateFunc func, bool isGenericTypeMatch = false) { }
					public static void RegisterMethod<T1,T2,TResult>(this TranslationRegistration registration, Expression<Func<T1,T2,TResult>> methodCallPattern, TranslationRegistration.TranslateFunc func, bool isGenericTypeMatch = false) { }
					public static void RegisterMember<TResult>(this TranslationRegistration registration, Expression<Func<TResult>> memberAccessPattern, TranslationRegistration.TranslateFunc func) { }
					public static void RegisterMember<T,TResult>(this TranslationRegistration registration, Expression<Func<T,TResult>> memberAccessPattern, TranslationRegistration.TranslateFunc func) { }
					public static void RegisterConstructor<T,TResult>(this TranslationRegistration registration, Expression<Func<T,TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc func) { }
				}

				public abstract class MemberTranslatorBase
				{
					protected TranslationRegistration Registration { get; } = new();
					protected static object? H(object member) => null;
				}

				public class MathMemberTranslatorBase : MemberTranslatorBase
				{
					public MathMemberTranslatorBase()
					{
						{{baseReg}}
					}
				}
			}

			namespace LinqToDB.Internal.DataProvider.Oracle.Translation
			{
				using LinqToDB.Internal.DataProvider.Translation;

				public class OracleMemberTranslator : MemberTranslatorBase
				{
					public OracleMemberTranslator()
					{
						{{oracleReg}}
					}
				}
			}

			namespace LinqToDB.Internal.DataProvider.PostgreSQL.Translation
			{
				using LinqToDB.Internal.DataProvider.Translation;

				public class PostgreSQLMemberTranslator : MemberTranslatorBase
				{
					public PostgreSQLMemberTranslator()
					{
						{{pgReg}}
					}
				}
			}

			namespace LinqToDB.Internal.DataProvider.SQLite.Translation
			{
				using LinqToDB.Internal.DataProvider.Translation;

				public class SQLiteMemberTranslator : MemberTranslatorBase
				{
					public SQLiteMemberTranslator()
					{
						{{sqliteReg}}
					}
				}
			}
			""";

		static Task Verify(string source, params DiagnosticResult[] expected) => AnalyzerVerifier<MemberMapShadowAnalyzer>.VerifyAsync(source, expected);

		const string PowRegistration   = "Registration.RegisterMethod((double x, double y) => Math.Pow(x, y), H);";
		const string RightRegistration = "Registration.RegisterMethod((string? s, int? n) => Sql.Right(s, n), H);";

		#region LINQ2DB0007 - common table

		// The #5922 instance itself: Math.Pow mapped in the common table and registered by the base math translator.
		// Pins the message arguments too, because they are what tells a reader which registration is dead and where.
		[Test]
		public Task PowMirrorIsReported() => Verify(
			Source(
				common:  "{ {|#0:M(() => Math.Pow(0, 0))|}, N(() => null) },",
				baseReg: PowRegistration),
			new DiagnosticResult("LINQ2DB0007", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("System.Math.Pow(double, double)", "common", "LinqToDB.Internal.DataProvider.Translation.MathMemberTranslatorBase"));

		// The key is the overload, not the name: Math.Round(double) mapped beside a Math.Round(decimal) registration
		// shadows nothing.
		[Test]
		public Task DifferentOverloadIsNotReported() => Verify(
			Source(
				common:  "{ M(() => Math.Round(0.0)), N(() => null) },",
				baseReg: "Registration.RegisterMethod((decimal v) => Math.Round(v), H);"));

		// The issue's cooperative chains: the map expands INTO a registered member, but never names it as its key.
		[Test]
		public Task CooperativeChainsAreNotReported() => Verify(
			Source(
				common:  """
					{ M(() => string.Compare("", "")), N(() => null) },
					{ M(() => Sql.ConvertTo<string>.From(Guid.Empty)), N(() => null) },
					""",
				baseReg: """
					Registration.RegisterMethod((string a, string b) => a.CompareTo(b), H);
					Registration.RegisterMethod((Guid g) => g.ToString(), H);
					"""));

		// A property access pattern goes through RegisterMember, and its key carries the receiver type.
		[Test]
		public Task RegisteredPropertyIsReported() => Verify(
			Source(
				common:  "{ {|LINQ2DB0007:M(() => \"\".Length)|}, N(() => null) },",
				baseReg: "Registration.RegisterMember((string s) => s.Length, H);"));

		// Same member name on a different receiver type is a different key.
		[Test]
		public Task SameNameOnAnotherReceiverIsNotReported() => Verify(
			Source(
				common:  "{ M(() => ((int[])null!).Length), N(() => null) },",
				baseReg: "Registration.RegisterMember((string s) => s.Length, H);"));

		[Test]
		public Task RegisteredConstructorIsReported() => Verify(
			Source(
				common:  "{ {|LINQ2DB0007:M(() => new DateTime(0L))|}, N(() => null) },",
				baseReg: "Registration.RegisterConstructor((long ticks) => new DateTime(ticks), H);"));

		// A generic-match registration covers every instantiation of the method, as TranslationRegistration stores
		// the generic definition.
		[Test]
		public Task GenericMatchRegistrationCoversOtherInstantiations() => Verify(
			Source(
				common:  "{ {|LINQ2DB0007:M(() => Sql.Identity(\"\"))|}, N(() => null) },",
				baseReg: "Registration.RegisterMethod((int v) => Sql.Identity(v), H, isGenericTypeMatch: true);"));

		// Control for the arm above: without the flag the registration covers its own instantiation only.
		[Test]
		public Task ExactRegistrationDoesNotCoverOtherInstantiations() => Verify(
			Source(
				common:  "{ M(() => Sql.Identity(\"\")), N(() => null) },",
				baseReg: "Registration.RegisterMethod((int v) => Sql.Identity(v), H);"));

		// Look-alikes outside the model: another Expressions type with its own M, and a RegisterMethod extension on
		// another registration type. Neither may be collected, so neither side can pair with the real one.
		[Test]
		public Task ForeignLookAlikesAreNotMatched() => Verify(
			Source(
				common:  "{ M(() => Math.Ceiling(0.0)), N(() => null) },",
				baseReg: "Registration.RegisterMethod((double v) => Math.Floor(v), H);") + """

			namespace Other
			{
				public static class Expressions
				{
					public static object M<T>(System.Linq.Expressions.Expression<Func<T>> func) => func;

					public static readonly object[] Entries = { M(() => Math.Floor(0.0)) };
				}

				public sealed class Registration { }

				public static class RegistrationExtensions
				{
					public static void RegisterMethod<T,TResult>(this Registration registration, System.Linq.Expressions.Expression<Func<T,TResult>> pattern, Func<object,object?> func) { }
				}

				class Translator
				{
					Translator()
					{
						new Registration().RegisterMethod((double v) => Math.Ceiling(v), _ => null);
					}
				}
			}
			""");

		#endregion

		#region LINQ2DB0007 - provider sections

		// A base registration is inherited by every provider translator, so it covers every section.
		[Test]
		public Task ProviderEntryAgainstBaseRegistrationIsReported() => Verify(
			Source(
				oracle:  "{ {|#0:M(() => Sql.Right(\"\", 0))|}, N(() => null) },",
				baseReg: RightRegistration),
			new DiagnosticResult("LINQ2DB0007", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("LinqToDB.Sql.Right(string?, int?)", "'Oracle'", "LinqToDB.Internal.DataProvider.Translation.MathMemberTranslatorBase"));

		[Test]
		public Task ProviderEntryAgainstItsOwnProviderRegistrationIsReported() => Verify(
			Source(
				oracle:    "{ {|LINQ2DB0007:M(() => Sql.Right(\"\", 0))|}, N(() => null) },",
				oracleReg: RightRegistration));

		// Control for the arm above: a registration in another provider's namespace never runs for Oracle.
		[Test]
		public Task ProviderEntryAgainstAnotherProviderRegistrationIsNotReported() => Verify(
			Source(
				oracle: "{ M(() => Sql.Right(\"\", 0)), N(() => null) },",
				pgReg:  RightRegistration));

		// A dotted section key resolves to its provider root: SQLite.MS is served by the SQLite translator.
		[Test]
		public Task DottedSectionResolvesToItsProviderRoot() => Verify(
			Source(
				sqliteMs:  "{ {|LINQ2DB0007:M(() => Math.Floor(0.0))|}, N(() => null) },",
				sqliteReg: "Registration.RegisterMethod((double v) => Math.Floor(v), H);"));

		// The common table runs for every provider, so any registration - provider-scoped included - is dead behind it.
		[Test]
		public Task CommonEntryAgainstProviderRegistrationIsReported() => Verify(
			Source(
				common: "{ {|LINQ2DB0007:M(() => Sql.Right(\"\", 0))|}, N(() => null) },",
				pgReg:  RightRegistration));

		#endregion

		#region LINQ2DB0008

		[Test]
		public Task ProviderEntryKillsSameConfigurationAttribute() => Verify(
			Source(
				oracle:    "{ {|#0:M(() => Sql.Right(\"\", 0))|}, N(() => null) },",
				rightAttr: "[Function(ProviderName.Oracle, \"SUBSTR\")]"),
			new DiagnosticResult("LINQ2DB0008", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("LinqToDB.Sql.Right(string?, int?)", "'Oracle'", "FunctionAttribute", "Oracle"));

		// Configuration set through the named property rather than the constructor.
		[Test]
		public Task ConfigurationSetByPropertyIsRead() => Verify(
			Source(
				oracle:    "{ {|LINQ2DB0008:M(() => Sql.Right(\"\", 0))|}, N(() => null) },",
				rightAttr: "[Function(\"SUBSTR\", Configuration = ProviderName.Oracle)]"));

		// A sub-configuration of the section's provider sits behind the same map entry in ConfigurationList.
		[Test]
		public Task ProviderEntryKillsSubConfigurationAttribute() => Verify(
			Source(
				sqlite:    "{ {|LINQ2DB0008:M(() => Sql.Right(\"\", 0))|}, N(() => null) },",
				rightAttr: "[Function(ProviderName.SQLiteMS, \"SUBSTR\")]"));

		// Controls: an unconfigured attribute still serves every other provider, another provider's attribute is
		// untouched, and the parent of a dotted section is not its child.
		[Test]
		public Task ProviderEntryLeavesOtherAttributesLive() => Verify(
			Source(
				oracle:    "{ M(() => Sql.Right(\"\", 0)), N(() => null) },",
				sqliteMs:  "{ M(() => Sql.Right(\"\", 0)), N(() => null) },",
				rightAttr: """
					[Function("RIGHT")]
					[Function(ProviderName.PostgreSQL, "RIGHT")]
					[Function(ProviderName.SQLite, "RIGHT")]
					"""));

		// The common table runs for every configuration, so even the unconfigured attribute is dead - the AltStuff case.
		[Test]
		public Task CommonEntryKillsUnconfiguredAttribute() => Verify(
			Source(
				common:    "{ {|LINQ2DB0008:M(() => Sql.Right(\"\", 0))|}, N(() => null) },",
				rightAttr: "[Function(\"RIGHT\")]"));

		#endregion

		#region LINQ2DB0009

		// An M call outside a table initializer has no section: the reader reports it rather than guessing one.
		[Test]
		public Task EntryOutsideAnInitializerIsDrift() => Verify(
			Source(extra: "static MemberInfoWithType Helper() => {|LINQ2DB0009:M(() => Math.Abs(0))|};"));

		// An entry whose lambda names no member cannot be keyed.
		[Test]
		public Task EntryWithoutAMemberIsDrift() => Verify(
			Source(common: "{ {|LINQ2DB0009:M(() => 1 + 2)|}, N(() => null) },"));

		// No entries at all means the reader no longer recognises the tables, which must not read as "no shadows".
		[Test]
		public Task EmptyModelIsDrift() => Verify(
			Source(sentinel: false)
				.Replace("public static class Expressions", "public static class {|LINQ2DB0009:Expressions|}", System.StringComparison.Ordinal));

		#endregion

		// The unchanged path: a model with no overlap at all - the common sentinel, the forwarding MapMember and an
		// unrelated registration - analyzes clean.
		[Test]
		public Task CleanModelIsNotReported() => Verify(
			Source(baseReg: PowRegistration));
	}
}
