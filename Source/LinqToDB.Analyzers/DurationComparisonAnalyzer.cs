using System;
using System.Collections.Immutable;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LinqToDB.Analyzers
{
	/// <summary>
	/// Reports an <c>==</c> / <c>!=</c> comparison, inside a query, between a member whose storage unit is declared
	/// with <c>[Duration(DurationUnit.X)]</c> and a constant <see cref="TimeSpan"/> that unit cannot represent. Such
	/// a comparison lowers to an empty range and returns no rows - the right answer for a duration the column cannot
	/// hold, but indistinguishable in the log from an ordinary range over two parameters.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class DurationComparisonAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>Diagnostic id for the unsatisfiable duration comparison rule.</summary>
		public const string DiagnosticId = "L2DB1002";

		const string DurationAttributeMetadataName = "LinqToDB.Mapping.DurationAttribute";
		const string DurationUnitMetadataName      = "LinqToDB.Mapping.DurationUnit";
		const string ExpressionMetadataName        = "System.Linq.Expressions.Expression`1";
		const string TimeSpanMetadataName          = "System.TimeSpan";
		const string EnumerableMetadataName        = "System.Linq.Enumerable";
		const string QueryableMetadataName         = "System.Linq.Queryable";
		const string HalfMetadataName              = "System.Half";

		const long TicksPerMicrosecond   = 10L;
		const int  MaxResolutionDepth    = 8;

		internal static readonly DiagnosticDescriptor Rule = new(
			id:                 DiagnosticId,
			title:              "Duration comparison a declared unit can never match",
			messageFormat:      "'{0}' stores whole {1} and cannot represent {2}, so this comparison {3}",
			category:           "LinqToDB",
			defaultSeverity:    DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description:        "A TimeSpan column declared with [Duration(DurationUnit.X)] holds a whole number of X units, so an equality against a duration that is not a whole number of them lowers to an empty range and matches nothing. The translation is correct - an empty range is what equality means for a value the column cannot hold - but neither the query nor the log says why no rows came back. Only a unit declared through the attribute is visible to an analyzer; one declared through HasDuration or a mapping schema is not diagnosed.",
			helpLinkUri:        "https://github.com/linq2db/linq2db/wiki/L2DB1002");

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				// Resolved once per compilation and by symbol presence rather than by version: the package carries no
				// linq2db dependency and no version floor, so it can sit next to a linq2db that lacks these entirely.
				var durationAttribute = startContext.Compilation.GetTypeByMetadataName(DurationAttributeMetadataName);
				var durationUnit      = startContext.Compilation.GetTypeByMetadataName(DurationUnitMetadataName);
				var timeSpan          = startContext.Compilation.GetTypeByMetadataName(TimeSpanMetadataName);
				var expressionOfT     = startContext.Compilation.GetTypeByMetadataName(ExpressionMetadataName);

				if (durationAttribute is null || durationUnit is null || timeSpan is null || expressionOfT is null)
					return;

				var analyzer = new CompilationAnalyzer(
					durationAttribute,
					durationUnit,
					timeSpan,
					expressionOfT,
					startContext.Compilation.GetTypeByMetadataName(EnumerableMetadataName),
					startContext.Compilation.GetTypeByMetadataName(QueryableMetadataName),
					startContext.Compilation.GetTypeByMetadataName(HalfMetadataName));

				startContext.RegisterOperationAction(analyzer.AnalyzeBinary, OperationKind.Binary);
			});
		}

		/// <summary>
		/// One <see cref="TimeSpan"/> the source could be asking for, read three ways because
		/// <c>TimeSpan.From*(double)</c> does not agree across runtimes.
		/// </summary>
		sealed class Candidate
		{
			public Candidate(long exact, long netFx, long truncated)
			{
				Exact     = exact;
				NetFx     = netFx;
				Truncated = truncated;
			}

			/// <summary>The value the source literally asks for, computed exactly.</summary>
			public long Exact     { get; }

			/// <summary>.NET Framework's reading: the product taken in whole milliseconds, rounded half away from zero.</summary>
			public long NetFx     { get; }

			/// <summary>Modern .NET's reading: the double tick product truncated toward zero.</summary>
			public long Truncated { get; }

			public static Candidate Exactly(long ticks) => new(ticks, ticks, ticks);

			/// <summary>A target that cannot be .NET Framework, so its whole-millisecond rounding is not a reading.</summary>
			public static Candidate Modern(long exact, long truncated) => new(exact, truncated, truncated);

			/// <summary>
			/// Whether the column can hold this duration. The analyzer cannot know which runtime the consumer
			/// targets, so a value any reading can represent is left alone.
			/// </summary>
			public bool IsRepresentable(long perUnit)
			{
				return Exact % perUnit == 0 || NetFx % perUnit == 0 || Truncated % perUnit == 0;
			}
		}

		/// <summary>Every unit declared on a member, plus the name of the one that applies to all configurations.</summary>
		sealed class DeclaredUnits
		{
			public DeclaredUnits(ImmutableArray<long> ticksPerUnit, string pluralName)
			{
				TicksPerUnit = ticksPerUnit;
				PluralName   = pluralName;
			}

			public ImmutableArray<long> TicksPerUnit { get; }
			public string               PluralName   { get; }
		}

		/// <summary>
		/// Per-compilation state. Kept off the analyzer instance (RS1008) and built once in the compilation-start
		/// action.
		/// </summary>
		sealed class CompilationAnalyzer
		{
			readonly INamedTypeSymbol  _durationAttribute;
			readonly INamedTypeSymbol  _durationUnit;
			readonly INamedTypeSymbol  _timeSpan;
			readonly INamedTypeSymbol  _expressionOfT;
			readonly INamedTypeSymbol? _enumerable;
			readonly INamedTypeSymbol? _queryable;

			// Whole-millisecond rounding is .NET Framework's alone: .NET 5 already truncates the double tick product
			// (Interval(double, double) -> IntervalFromDoubleTicks), so the reading only applies where the target can
			// be netfx. TimeSpan.FromMicroseconds arrived in .NET 7 and System.Half in .NET 5, and neither exists on
			// netfx or netstandard - so either one present rules netfx out, and their absence leaves netstandard and
			// netfx itself, where it still might round.
			readonly bool _netFxRoundingPossible;

			public CompilationAnalyzer(
				INamedTypeSymbol  durationAttribute,
				INamedTypeSymbol  durationUnit,
				INamedTypeSymbol  timeSpan,
				INamedTypeSymbol  expressionOfT,
				INamedTypeSymbol? enumerable,
				INamedTypeSymbol? queryable,
				INamedTypeSymbol? half)
			{
				_durationAttribute = durationAttribute;
				_durationUnit      = durationUnit;
				_timeSpan          = timeSpan;
				_expressionOfT     = expressionOfT;
				_enumerable        = enumerable;
				_queryable         = queryable;

				_netFxRoundingPossible = timeSpan.GetMembers("FromMicroseconds").IsEmpty && half is null;
			}

			// OperationKind.Binary fires on every binary operation in the compilation, so the gates run cheapest
			// first: operator kind, then operand type, then a member reference, and only then the attribute lookup,
			// the parent-chain walk and the candidate resolution.
			public void AnalyzeBinary(OperationAnalysisContext context)
			{
				var binary = (IBinaryOperation)context.Operation;

				if (binary.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
					return;

				var left  = Unwrap(binary.LeftOperand);
				var right = Unwrap(binary.RightOperand);

				if (!IsTimeSpan(left.Type) || !IsTimeSpan(right.Type))
					return;

				if (!TryReport(context, binary, left, right))
					TryReport(context, binary, right, left);
			}

			bool TryReport(OperationAnalysisContext context, IBinaryOperation binary, IOperation member, IOperation value)
			{
				if (GetReferencedMember(member) is not { } symbol)
					return false;

				// Before the attribute lookup: a pure walk of the operation tree, and it keeps a member read off
				// anything but the range variable from reaching the per-attribute allocation below.
				if (!IsOnRangeVariable(member))
					return false;

				if (GetDeclaredUnits(symbol) is not { } units)
					return false;

				if (!IsInsideExpressionTree(binary, _expressionOfT))
					return false;

				if (TryResolveCandidates(value, 0) is not { } candidates || candidates.IsDefaultOrEmpty)
					return false;

				var offending   = default(Candidate);
				var allCandidates = true;

				foreach (var candidate in candidates)
				{
					if (IsBlocked(candidate, units))
						offending ??= candidate;
					else
						allCandidates = false;
				}

				if (offending is null)
					return false;

				context.ReportDiagnostic(Diagnostic.Create(
					Rule,
					binary.Syntax.GetLocation(),
					symbol.Name,
					units.PluralName,
					FormatTicks(offending.Exact),
					Outcome(binary.OperatorKind, IsNullable(member.Type), allCandidates)));

				return true;
			}

			// Every declared unit has to agree. The effective one is chosen per mapping-schema configuration, so a
			// duration that a single configuration can hold is not a defect.
			static bool IsBlocked(Candidate candidate, DeclaredUnits units)
			{
				foreach (var perUnit in units.TicksPerUnit)
					if (candidate.IsRepresentable(perUnit))
						return false;

				return true;
			}

			static string FormatTicks(long ticks)
			{
				return new TimeSpan(ticks).ToString("c", CultureInfo.InvariantCulture);
			}

			// Spelled out per case rather than composed, so each phrase is exactly true. A '!=' against a nullable
			// member does not "always match": it leaves the declared-unit path and lands on the ticks comparison,
			// where a NULL column is excluded unless CompareNulls.LikeClr is in force.
			static string Outcome(BinaryOperatorKind op, bool nullableMember, bool allCandidates)
			{
				if (op == BinaryOperatorKind.Equals)
					return allCandidates ? "can never match" : "can never match when that value is compared";

				if (nullableMember)
					return allCandidates
						? "excludes no row that has a value"
						: "excludes no row that has a value when that value is compared";

				return allCandidates ? "always matches" : "always matches when that value is compared";
			}

			#region Declared unit

			DeclaredUnits? GetDeclaredUnits(ISymbol member)
			{
				var attributes = GetDurationAttributes(member);
				if (attributes.IsDefaultOrEmpty)
					return null;

				var builder      = ImmutableArray.CreateBuilder<long>(attributes.Length);
				var fallbackName = default(string);

				foreach (var attribute in attributes)
				{
					if (GetUnitName(attribute) is not { } name || !TryGetUnit(name, out var perUnit, out var plural))
						return null;

					builder.Add(perUnit);

					if (fallbackName is null && !HasConfiguration(attribute))
						fallbackName = plural;
				}

				// Without an attribute that applies to every configuration, a configuration none of them names
				// leaves the column with no duration semantics at all, and the comparison is an ordinary one.
				return fallbackName is null ? null : new DeclaredUnits(builder.MoveToImmutable(), fallbackName);
			}

			ImmutableArray<AttributeData> GetDurationAttributes(ISymbol member)
			{
				// [Duration] is Inherited, so a base declaration counts - and it may live in another assembly.
				for (var current = member; current is not null; current = GetOverriddenMember(current))
				{
					var builder = ImmutableArray.CreateBuilder<AttributeData>();

					foreach (var attribute in current.GetAttributes())
						if (IsDurationAttribute(attribute.AttributeClass))
							builder.Add(attribute);

					if (builder.Count > 0)
						return builder.ToImmutable();
				}

				return ImmutableArray<AttributeData>.Empty;
			}

			// DurationAttribute is public and unsealed, and MappingSchema.GetAttribute<DurationAttribute> resolves by
			// assignability - so a derived attribute declares the unit just as the base one does.
			bool IsDurationAttribute(INamedTypeSymbol? attributeClass)
			{
				for (var current = attributeClass; current is not null; current = current.BaseType)
					if (SymbolEqualityComparer.Default.Equals(current, _durationAttribute))
						return true;

				return false;
			}

			static ISymbol? GetOverriddenMember(ISymbol member)
			{
				return member is IPropertySymbol property ? property.OverriddenProperty : null;
			}

			// Unit is settable as well as a constructor parameter, so [Duration(Second, Unit = Hour)] is legal and
			// the named assignment is what runs last.
			string? GetUnitName(AttributeData attribute)
			{
				foreach (var named in attribute.NamedArguments)
					if (string.Equals(named.Key, "Unit", StringComparison.Ordinal))
						return UnitMemberName(named.Value);

				return attribute.ConstructorArguments.Length > 0 ? UnitMemberName(attribute.ConstructorArguments[0]) : null;
			}

			// The constant has to actually be a DurationUnit. A derived attribute may take something else in that
			// position, and matching an unrelated integer against the enum's values would name a unit nobody wrote.
			string? UnitMemberName(TypedConstant constant)
			{
				return SymbolEqualityComparer.Default.Equals(constant.Type, _durationUnit)
					? EnumMemberName(_durationUnit, constant.Value)
					: null;
			}

			static bool HasConfiguration(AttributeData attribute)
			{
				foreach (var named in attribute.NamedArguments)
					if (string.Equals(named.Key, "Configuration", StringComparison.Ordinal))
						return named.Value.Value is string { Length: > 0 };

				return false;
			}

			// Both sides are boxed underlying integers, so this compares with Equals rather than ==. Keying on the
			// name rather than the number is what keeps the table right for a linq2db whose members sit elsewhere.
			static string? EnumMemberName(INamedTypeSymbol enumType, object? value)
			{
				if (value is null)
					return null;

				foreach (var member in enumType.GetMembers())
					if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, value))
						return field.Name;

				return null;
			}

			// Mirrors SqlIntervalUnits.TryGetTicksRatio for the eight members DurationUnit actually has. Tick
			// represents every TimeSpan exactly and Nanosecond is finer than one, so nothing is unrepresentable in
			// either; an unrecognized name belongs to a newer linq2db and degrades to silence.
			static bool TryGetUnit(string name, out long ticksPerUnit, out string pluralName)
			{
				switch (name)
				{
					case "Microsecond": ticksPerUnit = TicksPerMicrosecond;          pluralName = "microseconds"; return true;
					case "Millisecond": ticksPerUnit = TimeSpan.TicksPerMillisecond; pluralName = "milliseconds"; return true;
					case "Second":      ticksPerUnit = TimeSpan.TicksPerSecond;      pluralName = "seconds";      return true;
					case "Minute":      ticksPerUnit = TimeSpan.TicksPerMinute;      pluralName = "minutes";      return true;
					case "Hour":        ticksPerUnit = TimeSpan.TicksPerHour;        pluralName = "hours";        return true;
					case "Day":         ticksPerUnit = TimeSpan.TicksPerDay;         pluralName = "days";         return true;
					default:            ticksPerUnit = 0L;                           pluralName = "";             return false;
				}
			}

			#endregion

			#region Expression-tree gate

			// The node carrying the Expression<T> type sits above the lambda, but which node kind it is differs
			// between a delegate target and an expression-tree target - so the type is tested, not the kind. Every
			// enclosing lambda is checked rather than the nearest, because an inner Any(x => ...) inside a query
			// predicate is converted to Func<> while the query itself is still an expression tree.
			static bool IsInsideExpressionTree(IOperation operation, INamedTypeSymbol expressionOfT)
			{
				for (var current = operation; current is not null; current = current.Parent)
				{
					if (current is not IAnonymousFunctionOperation)
						continue;

					for (var above = current.Parent; above is IConversionOperation or IDelegateCreationOperation; above = above.Parent)
						if (SymbolEqualityComparer.Default.Equals((above.Type as INamedTypeSymbol)?.OriginalDefinition, expressionOfT))
							return true;
				}

				return false;
			}

			#endregion

			#region Candidate resolution

			// Every step bails rather than guessing: a shape this does not recognize yields no diagnostic at all.
			ImmutableArray<Candidate>? TryResolveCandidates(IOperation operation, int depth)
			{
				if (depth > MaxResolutionDepth)
					return null;

				var unwrapped = Unwrap(operation);

				if (TryResolveSingle(unwrapped, depth) is { } single)
					return ImmutableArray.Create(single);

				return unwrapped switch
				{
					ILocalReferenceOperation     local     => TryResolveLocal(local, depth),
					IParameterReferenceOperation parameter => TryResolveRangeVariable(parameter, depth),
					_                                      => null,
				};
			}

			Candidate? TryResolveSingle(IOperation operation, int depth)
			{
				return operation switch
				{
					IObjectCreationOperation creation                              => FromConstructor(creation),
					IInvocationOperation     invocation                            => FromFactory(invocation),
					IFieldReferenceOperation field                                 => FromWellKnownField(field),
					IBinaryOperation         binary                                => FromArithmetic(binary, depth),
					IUnaryOperation { OperatorKind: UnaryOperatorKind.Minus } unary => Negate(ResolveExactlyOne(unary.Operand, depth + 1)),
					_                                                              => null,
				};
			}

			Candidate? ResolveExactlyOne(IOperation operation, int depth)
			{
				return TryResolveCandidates(operation, depth) is { Length: 1 } candidates ? candidates[0] : null;
			}

			Candidate? FromWellKnownField(IFieldReferenceOperation field)
			{
				if (!field.Field.IsStatic || !SymbolEqualityComparer.Default.Equals(field.Field.ContainingType, _timeSpan))
					return null;

				return field.Field.Name switch
				{
					"Zero"     => Candidate.Exactly(0L),
					"MinValue" => Candidate.Exactly(long.MinValue),
					"MaxValue" => Candidate.Exactly(long.MaxValue),
					_          => null,
				};
			}

			Candidate? FromConstructor(IObjectCreationOperation creation)
			{
				if (!SymbolEqualityComparer.Default.Equals(creation.Type, _timeSpan) || creation.Constructor is null)
					return null;

				var parts = new long[creation.Constructor.Parameters.Length];

				foreach (var argument in creation.Arguments)
				{
					if (argument.Parameter is null
						|| argument.Value.ConstantValue is not { HasValue: true, Value: { } raw }
						|| !TryToInt64(raw, out parts[argument.Parameter.Ordinal]))
					{
						return null;
					}
				}

				return FromParts(parts);
			}

			static Candidate? FromParts(long[] parts)
			{
				decimal days = 0m, hours = 0m, minutes = 0m, seconds = 0m, milliseconds = 0m, microseconds = 0m;

				switch (parts.Length)
				{
					case 1: return Candidate.Exactly(parts[0]);
					case 3: hours = parts[0]; minutes = parts[1]; seconds = parts[2]; break;
					case 4: days = parts[0]; hours = parts[1]; minutes = parts[2]; seconds = parts[3]; break;
					case 5: days = parts[0]; hours = parts[1]; minutes = parts[2]; seconds = parts[3]; milliseconds = parts[4]; break;
					case 6: days = parts[0]; hours = parts[1]; minutes = parts[2]; seconds = parts[3]; milliseconds = parts[4]; microseconds = parts[5]; break;
					default: return null;
				}

				var ticks = ((days * 86400m + hours * 3600m + minutes * 60m + seconds) * TimeSpan.TicksPerSecond)
					+ milliseconds * TimeSpan.TicksPerMillisecond
					+ microseconds * TicksPerMicrosecond;

				return TryTicksFromDecimal(ticks, out var value) ? Candidate.Exactly(value) : null;
			}

			Candidate? FromFactory(IInvocationOperation invocation)
			{
				var method = invocation.TargetMethod;

				if (!method.IsStatic || !SymbolEqualityComparer.Default.Equals(method.ContainingType, _timeSpan))
					return null;

				if (string.Equals(method.Name, "Parse", StringComparison.Ordinal)
					|| string.Equals(method.Name, "ParseExact", StringComparison.Ordinal))
				{
					return FromParse(invocation, method.Name);
				}

				if (string.Equals(method.Name, "FromTicks", StringComparison.Ordinal))
					return FromScaledArgument(invocation, 1L, allowDouble: false, netFxRounding: false);

				var scale = FactoryScale(method.Name);

				return scale == 0L ? null : FromScaledArgument(invocation, scale, allowDouble: true, _netFxRoundingPossible);
			}

			static long FactoryScale(string name)
			{
				return name switch
				{
					"FromDays"         => TimeSpan.TicksPerDay,
					"FromHours"        => TimeSpan.TicksPerHour,
					"FromMinutes"      => TimeSpan.TicksPerMinute,
					"FromSeconds"      => TimeSpan.TicksPerSecond,
					"FromMilliseconds" => TimeSpan.TicksPerMillisecond,
					"FromMicroseconds" => TicksPerMicrosecond,
					_                  => 0L,
				};
			}

			// Only the single-argument overloads are folded; the component overloads added in later runtimes take
			// integers and are left alone rather than half-supported.
			static Candidate? FromScaledArgument(IInvocationOperation invocation, long scaleTicks, bool allowDouble, bool netFxRounding)
			{
				if (invocation.Arguments.Length != 1
					|| invocation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: { } raw })
				{
					return null;
				}

				if (raw is double value)
					return allowDouble ? FromDoubleFactory(value, scaleTicks, netFxRounding) : null;

				if (!TryToInt64(raw, out var units))
					return null;

				return TryTicksFromDecimal((decimal)units * scaleTicks, out var ticks) ? Candidate.Exactly(ticks) : null;
			}

			// The two runtimes disagree twice over, so the value is carried as every reading the target could produce
			// and only reported when none of them lands on a whole unit.
			static Candidate? FromDoubleFactory(double value, long scaleTicks, bool netFxRounding)
			{
				if (double.IsNaN(value) || double.IsInfinity(value))
					return null;

				var product = value * scaleTicks;

				if (product is > long.MaxValue or < long.MinValue)
					return null;

				if (!TryTicksFromDecimal((decimal)value * scaleTicks, out var exact))
					return null;

				if (!netFxRounding)
					return Candidate.Modern(exact, (long)product);

				var millis = value * (scaleTicks / (double)TimeSpan.TicksPerMillisecond) + (value >= 0 ? 0.5 : -0.5);

				if (millis is > long.MaxValue / TimeSpan.TicksPerMillisecond or < long.MinValue / TimeSpan.TicksPerMillisecond)
					return null;

				return new Candidate(exact, (long)millis * TimeSpan.TicksPerMillisecond, (long)product);
			}

			// Parsed under the invariant culture only: the fraction separator is culture-sensitive and the consumer's
			// culture is not knowable here, so a string that does not parse invariantly is left alone.
			static Candidate? FromParse(IInvocationOperation invocation, string name)
			{
				if (invocation.Arguments.Length == 0
					|| invocation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string input })
				{
					return null;
				}

				if (string.Equals(name, "ParseExact", StringComparison.Ordinal))
				{
					// Only the overloads without a TimeSpanStyles argument: this parse does not read it, and
					// AssumeNegative flips the sign, so folding those would name a duration the source never writes.
					if (invocation.Arguments.Length is < 2 or > 3
						|| invocation.Arguments[1].Value.ConstantValue is not { HasValue: true, Value: string format }
						|| !TimeSpan.TryParseExact(input, format, CultureInfo.InvariantCulture, out var matched))
					{
						return null;
					}

					return Candidate.Exactly(matched.Ticks);
				}

				return TimeSpan.TryParse(input, CultureInfo.InvariantCulture, out var parsed)
					? Candidate.Exactly(parsed.Ticks)
					: null;
			}

			Candidate? FromArithmetic(IBinaryOperation binary, int depth)
			{
				if (binary.OperatorKind is not (BinaryOperatorKind.Add or BinaryOperatorKind.Subtract))
					return null;

				if (ResolveExactlyOne(binary.LeftOperand, depth + 1) is not { } left
					|| ResolveExactlyOne(binary.RightOperand, depth + 1) is not { } right)
				{
					return null;
				}

				var add = binary.OperatorKind == BinaryOperatorKind.Add;

				return TryCombine(left.Exact, right.Exact, add, out var exact)
					&& TryCombine(left.NetFx, right.NetFx, add, out var netFx)
					&& TryCombine(left.Truncated, right.Truncated, add, out var truncated)
						? new Candidate(exact, netFx, truncated)
						: null;
			}

			static Candidate? Negate(Candidate? candidate)
			{
				if (candidate is not { } value)
					return null;

				return TryTicksFromDecimal(-(decimal)value.Exact, out var exact)
					&& TryTicksFromDecimal(-(decimal)value.NetFx, out var netFx)
					&& TryTicksFromDecimal(-(decimal)value.Truncated, out var truncated)
						? new Candidate(exact, netFx, truncated)
						: null;
			}

			static bool TryCombine(long left, long right, bool add, out long result)
			{
				return TryTicksFromDecimal((decimal)left + (add ? right : -(decimal)right), out result);
			}

			static bool TryTicksFromDecimal(decimal ticks, out long value)
			{
				value = 0L;

				if (ticks < long.MinValue || ticks > long.MaxValue || ticks != decimal.Truncate(ticks))
					return false;

				value = (long)ticks;

				return true;
			}

			static bool TryToInt64(object raw, out long value)
			{
				switch (raw)
				{
					case long   l: value = l;  return true;
					case int    i: value = i;  return true;
					case short  s: value = s;  return true;
					case sbyte  y: value = y;  return true;
					case byte   b: value = b;  return true;
					case ushort h: value = h;  return true;
					case uint   u: value = u;  return true;
					default:       value = 0L; return false;
				}
			}

			#endregion

			#region Locals, loops and range variables

			ImmutableArray<Candidate>? TryResolveLocal(ILocalReferenceOperation reference, int depth)
			{
				var local = reference.Local;

				if (local.IsRef)
					return null;

				var root = GetRoot(reference);

				// A foreach control variable takes its values from the loop's collection. C# forbids assigning it,
				// so the read sweep below does not apply to this shape.
				foreach (var operation in root.Descendants())
				{
					if (operation is IForEachLoopOperation forEach
						&& forEach.LoopControlVariable is IVariableDeclaratorOperation declarator
						&& SymbolEqualityComparer.Default.Equals(declarator.Symbol, local))
					{
						return TryResolveCollection(forEach.Collection, depth + 1);
					}
				}

				if (!AllReferencesAreReads(root, local) || FindInitializer(root, local) is not { } initializer)
					return null;

				return TryResolveCandidates(initializer, depth + 1) is { Length: 1 } single ? single : null;
			}

			ImmutableArray<Candidate>? TryResolveCollection(IOperation? collection, int depth)
			{
				if (collection is null || depth > MaxResolutionDepth)
					return null;

				var unwrapped = Unwrap(collection);

				if (unwrapped is ILocalReferenceOperation reference)
				{
					var root = GetRoot(reference);

					return reference.Local.IsRef || !OnlySafelyEnumerated(root, reference.Local)
						? null
						: TryResolveCollection(FindInitializer(root, reference.Local), depth + 1);
				}

				// Only an array literal. A List<T> initializer would be a collection that can be mutated after it is
				// built, and nothing here can see that happen.
				if (unwrapped is not IArrayCreationOperation { Initializer: { } elements })
					return null;

				var builder = ImmutableArray.CreateBuilder<Candidate>(elements.ElementValues.Length);

				foreach (var element in elements.ElementValues)
				{
					if (ResolveExactlyOne(element, depth + 1) is not { } candidate)
						return null;

					builder.Add(candidate);
				}

				return builder.Count == 0 ? null : builder.ToImmutable();
			}

			// The LINQ shapes that are safe are the ones where the lambda's only parameter ranges over the sequence
			// in the first position. Zip, a Join key selector, Aggregate and a SelectMany result selector all take a
			// parameter drawn from somewhere else, and resolving the first argument for those would report a value
			// the predicate never compares.
			ImmutableArray<Candidate>? TryResolveRangeVariable(IParameterReferenceOperation reference, int depth)
			{
				// The lambda that *declares* the parameter, not the innermost one enclosing the reference: the
				// comparison usually sits inside a further nested query lambda, so the two are rarely the same.
				if (FindDeclaringLambda(reference) is not { } lambda || lambda.Symbol.Parameters.Length != 1)
					return null;

				var above = lambda.Parent;

				while (above is IConversionOperation or IDelegateCreationOperation)
					above = above.Parent;

				if (above is not IArgumentOperation argument
					|| argument.Parent is not IInvocationOperation invocation
					|| !IsSequenceOperator(invocation.TargetMethod)
					|| !IsElementSelector(invocation.TargetMethod.Name))
				{
					return null;
				}

				var arguments = invocation.Arguments;

				// The sequence the parameter ranges over is the operator's own source, which sits first - including
				// for a reduced extension call, where the receiver is argument zero. A lambda in that position is
				// not a selector at all.
				if (arguments.Length < 2 || ReferenceEquals(arguments[0], argument))
					return null;

				return TryResolveCollection(arguments[0].Value, depth + 1);
			}

			bool IsSequenceOperator(IMethodSymbol method)
			{
				return SymbolEqualityComparer.Default.Equals(method.ContainingType, _enumerable)
					|| SymbolEqualityComparer.Default.Equals(method.ContainingType, _queryable);
			}

			// Named rather than derived from an argument position, because a position cannot tell the two apart:
			// Join's inner key selector is also a one-parameter lambda in a later argument, but its parameter ranges
			// over the *second* sequence, so resolving the first would report a duration nothing compares. Zip,
			// Join, GroupJoin and Aggregate are absent for that reason.
			static bool IsElementSelector(string name)
			{
				return name switch
				{
					"Where" or "Select" or "SelectMany" or "Any" or "All"
						or "First" or "FirstOrDefault" or "Last" or "LastOrDefault"
						or "Single" or "SingleOrDefault" or "Count" or "LongCount"
						or "TakeWhile" or "SkipWhile"
						or "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending"
						or "GroupBy" or "Sum" or "Min" or "Max" or "Average" => true,
					_                                                        => false,
				};
			}

			static IAnonymousFunctionOperation? FindDeclaringLambda(IParameterReferenceOperation reference)
			{
				for (var current = reference.Parent; current is not null; current = current.Parent)
					if (current is IAnonymousFunctionOperation lambda
						&& SymbolEqualityComparer.Default.Equals(lambda.Symbol, reference.Parameter.ContainingSymbol))
					{
						return lambda;
					}

				return null;
			}

			// Every reference has to be a plain read. Enumerating the ways a local can be written instead would miss
			// whichever one nobody thought of - deconstruction, a ref local, an address-of - and a missed write means
			// folding a value the code no longer holds.
			static bool AllReferencesAreReads(IOperation root, ILocalSymbol local)
			{
				foreach (var operation in root.Descendants())
					if (operation is ILocalReferenceOperation reference
						&& SymbolEqualityComparer.Default.Equals(reference.Local, local)
						&& !IsPlainRead(reference))
					{
						return false;
					}

				return true;
			}

			// The positions a value can be read from, listed rather than the positions it can be written from: a
			// write shape nobody enumerated then degrades to silence instead of to a fold of a stale value.
			static bool IsPlainRead(IOperation reference)
			{
				var node = reference;

				// (bound, x) = (...) puts the local inside a tuple on the assignment's left.
				while (node.Parent is ITupleOperation tuple)
					node = tuple;

				return node.Parent switch
				{
					IAssignmentOperation assignment              => assignment.Target != node,
					IArgumentOperation { Parameter.RefKind: RefKind.None } => true,
					// 'ref var alias = ref bound' initializes a ref local, and a later write through the alias is a
					// write to this local that no sweep of its own references can see.
					IVariableInitializerOperation initializer    => initializer.Parent is IVariableDeclaratorOperation { Symbol.IsRef: false },
					IBinaryOperation                            => true,
					IUnaryOperation                             => true,
					IConversionOperation                        => true,
					ICoalesceOperation                          => true,
					IConditionalOperation                       => true,
					IParenthesizedOperation                     => true,
					IArrayInitializerOperation                  => true,
					IInterpolationOperation                     => true,
					IReturnOperation                            => true,
					IIsPatternOperation                         => true,
					ISwitchExpressionOperation                  => true,
					IPropertyReferenceOperation reference2       => ReferenceEquals(reference2.Instance, node),
					IFieldReferenceOperation field              => ReferenceEquals(field.Instance, node),
					IInvocationOperation invocation             => ReferenceEquals(invocation.Instance, node),
					_                                           => false,
				};
			}

			// An array local is only safe if nothing that touches it can change what it holds. A foreach reads it,
			// and so do the sequence operators the range-variable rule already trusts. Anything else - an element
			// assignment, Array.Sort, a call that keeps a reference - could reorder or rewrite the values this rule
			// is about to fold, so it is refused rather than guessed at.
			bool OnlySafelyEnumerated(IOperation root, ILocalSymbol local)
			{
				foreach (var operation in root.Descendants())
				{
					if (operation is not ILocalReferenceOperation reference
						|| !SymbolEqualityComparer.Default.Equals(reference.Local, local))
					{
						continue;
					}

					var parent = reference.Parent;

					while (parent is IConversionOperation)
						parent = parent.Parent;

					if (parent is IForEachLoopOperation)
						continue;

					if (parent is IArgumentOperation argument
						&& argument.Parent is IInvocationOperation invocation
						&& IsSequenceOperator(invocation.TargetMethod)
						&& IsElementSelector(invocation.TargetMethod.Name)
						&& invocation.Arguments.Length > 0
						&& ReferenceEquals(invocation.Arguments[0], argument))
					{
						continue;
					}

					return false;
				}

				return true;
			}

			static IOperation? FindInitializer(IOperation root, ILocalSymbol local)
			{
				foreach (var operation in root.Descendants())
					if (operation is IVariableDeclaratorOperation declarator
						&& SymbolEqualityComparer.Default.Equals(declarator.Symbol, local))
					{
						return declarator.Initializer?.Value;
					}

				return null;
			}

			static IOperation GetRoot(IOperation operation)
			{
				var current = operation;

				while (current.Parent is { } parent)
					current = parent;

				return current;
			}

			#endregion

			#region Types

			bool IsTimeSpan(ITypeSymbol? type)
			{
				return SymbolEqualityComparer.Default.Equals(UnwrapNullable(type), _timeSpan);
			}

			static bool IsNullable(ITypeSymbol? type)
			{
				return type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
			}

			static ITypeSymbol? UnwrapNullable(ITypeSymbol? type)
			{
				return type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
					? nullable.TypeArguments[0]
					: type;
			}

			static IOperation Unwrap(IOperation operation)
			{
				while (operation is IConversionOperation { Conversion.IsUserDefined: false, Operand: { } operand })
					operation = operand;

				return operation;
			}

			static ISymbol? GetReferencedMember(IOperation operation)
			{
				return operation switch
				{
					IPropertyReferenceOperation property => property.Property,
					IFieldReferenceOperation    field    => field.Field,
					_                                    => null,
				};
			}

			// A [Duration] member is a translated column only when it is read off the query's own range variable.
			// Read off anything else - a captured object, a static, a captured 'this' - linq2db evaluates that whole
			// subtree while the parameter value is produced, so the comparison is ordinary CLR equality and a row
			// holding the value does match. Saying "can never match" there would be false.
			static bool IsOnRangeVariable(IOperation operation)
			{
				var current = operation;

				while (true)
				{
					var instance = current switch
					{
						IPropertyReferenceOperation property => property.Instance,
						IFieldReferenceOperation    field    => field.Instance,
						_                                    => null,
					};

					if (instance is null)
						return false;

					current = Unwrap(instance);

					if (current is IParameterReferenceOperation parameter)
						return parameter.Parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction };
				}
			}

			#endregion
		}
	}
}
