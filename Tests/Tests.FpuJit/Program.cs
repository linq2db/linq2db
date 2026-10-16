// Regression gate for #6042 / #6051: hosts that unmask x87/SSE floating-point exceptions (C++Builder, Delphi)
// make the x86 .NET Framework JIT fault while compiling methods that use NaN/Infinity constants.
// The exe JIT-compiles every method of every netfx-shipped linq2db assembly with those exceptions unmasked, generic
// ones once with constraint-satisfying arguments. Not covered: value-type instantiations other than the one tried,
// generic definitions no substitute closes (reported), and expression trees compiled at run time.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Tests.FpuJit
{
	static class Program
	{
		// Keep in sync with the ProjectReferences in Tests.FpuJit.csproj.
		static readonly string[] ExpectedAssemblies =
		{
			"linq2db",
			"linq2db.Compat",
			"linq2db.EntityFrameworkCore",
			"linq2db.Extensions",
			"linq2db.FSharp",
			"linq2db.Remote.Grpc",
			"linq2db.Remote.HttpClient.Client",
			"linq2db.Remote.SignalR.Client",
			"linq2db.Remote.SignalR.Server",
			"linq2db.Remote.Wcf",
			"linq2db.Scaffold",
			"linq2db.Tools",
		};

		// Methods that may be skipped because a dependency does not load: (assembly, message fragment, reason).
		// A skip does not restart the child, so whatever its prepare compiles after a control-word reset stays
		// compiled masked and is later counted OK; an entry here accepts that as well.
		static readonly (string Assembly, string MessagePart, string Reason)[] AllowedDependencySkips = [];

		// Methods that may stay unverified because compiling them resets the FPU control word even in a fresh process:
		// (assembly, method display prefix, reason).
		static readonly (string Assembly, string MethodPrefix, string Reason)[] AllowedUnverified =
		[
			("linq2db", "LinqToDB.Internal.DataProvider.DuckDB.", "DuckDB.NET has no .NET Framework build: the adapter's failed bind in a type initializer resets the control word on every attempt"),
		];

		const string CanaryAssembly   = "canary";
		const int    MaxRestarts      = 100;
		const int    ChildTimeoutMs   = 10 * 60 * 1000;
		const uint   FpuExceptionMask = 0x10 | 0x08 | 0x04; // _EM_INVALID | _EM_ZERODIVIDE | _EM_OVERFLOW

		const int ExitOk          = 0;
		const int ExitJitFaults   = 1;
		const int ExitEnvironment = 2;
		const int ExitHarness     = 3;
		const int ExitRestart     = 4;

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		static extern uint _control87(uint newValue, uint mask);

		[DllImport("kernel32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		static extern uint SetErrorMode(uint mode);

		static int Main(string[] args)
		{
			// SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX: a child killed by a JIT fault must not raise a WER dialog
			SetErrorMode(0x0001 | 0x0002);

			if (Environment.Is64BitProcess)
			{
				Console.WriteLine("ERROR: must run as x86 (32-bit) process; the fault does not exist on x64");
				return ExitEnvironment;
			}

			if (args.Length >= 2 && args[0] == "--child")
				return RunChild(args[1], args.Length >= 4 ? args[3] : null, args.Length >= 4 && args[2] == "--resume-at");

			return RunParent();
		}

		// Positive controls: each must fault under the unmasked FPU, or the sweep proves nothing; the generic ones
		// prove the instantiation path.
		[MethodImpl(MethodImplOptions.NoInlining)]
		static double Canary() => double.NaN;

		[MethodImpl(MethodImplOptions.NoInlining)]
		static object GenericCanary<T>() => double.NaN;

		static class GenericCanaryType<T>
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			public static double Canary() => double.NaN;
		}

		#region Parent

		static int RunParent()
		{
			var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fpujit-run");
			Directory.CreateDirectory(dir);

			var resultsPath = Path.Combine(dir, "results.txt");
			File.Delete(resultsPath);

			string? resumeId = null;
			var     resumeAt = false;

			for (var restart = 0; ; restart++)
			{
				if (restart > MaxRestarts)
				{
					Console.WriteLine($"ERROR: more than {MaxRestarts} child restarts");
					return ExitHarness;
				}

				var exitCode = SpawnChild(resultsPath, resumeId, resumeAt, out var timedOut);
				var lines    = File.ReadAllLines(resultsPath);

				var error = lines.FirstOrDefault(l => l.StartsWith("ERROR\t", StringComparison.Ordinal));
				if (error != null)
				{
					Console.WriteLine($"ERROR: {error.Substring(6)}");
					return exitCode == ExitHarness ? ExitHarness : ExitEnvironment;
				}

				if (lines.Contains("DONE"))
					return Report(lines);

				var last = lines.LastOrDefault();
				if (last != null && last.StartsWith("RESTART\t", StringComparison.Ordinal))
				{
					// RESTART <id> at|after: "at" retries the method in a fresh process, its type initializers already run;
					// if it resets again it cannot be verified.
					var restartParts = last.Split('\t');
					var id           = restartParts[1];

					if (restartParts[2] == "after")
					{
						resumeId = id;
						resumeAt = false;
					}
					else if (resumeAt && id == resumeId)
					{
						var reset = lines[lines.Length - 2].Split('\t');
						File.AppendAllText(resultsPath, $"UNVERIFIED\t{reset[1]}\t{reset[2]}\t{id}\n");
						resumeAt = false;
					}
					else
					{
						resumeId = id;
						resumeAt = true;
					}

					continue;
				}

				if (last == null || !last.StartsWith("PREP\t", StringComparison.Ordinal))
				{
					Console.WriteLine($"ERROR: child exited with 0x{exitCode:X8} outside a method prepare");
					return ExitHarness;
				}

				// PREP <asm> <id> <display>: the method being compiled when the child died
				var parts = last.Split('\t');
				File.AppendAllText(resultsPath, $"CRASH\t{parts[1]}\t{parts[2]}\t{parts[3]}\t{(timedOut ? "timeout" : $"process exit 0x{exitCode:X8}")}\n");

				resumeId = parts[2];
				resumeAt = false;
			}
		}

		static int SpawnChild(string resultsPath, string? resumeId, bool resumeAt, out bool timedOut)
		{
			var arguments = $"--child \"{resultsPath}\"";
			if (resumeId != null)
				arguments += $" {(resumeAt ? "--resume-at" : "--resume-after")} {resumeId}";

			var psi = new ProcessStartInfo(typeof(Program).Assembly.Location, arguments)
			{
				UseShellExecute = false,
				CreateNoWindow  = true,
			};

			using var process = Process.Start(psi)!;

			timedOut = !process.WaitForExit(ChildTimeoutMs);
			if (timedOut)
			{
				process.Kill();
				process.WaitForExit();
			}

			return process.ExitCode;
		}

		static int Report(string[] lines)
		{
			var records = lines
				.Select(l => l.Split('\t'))
				.ToList();

			var faults = records
				.Where(r => r[0] is "FAIL" or "CRASH")
				.ToList();

			// enumeration-time records are re-written by every restarted child
			List<string[]> Distinct(string kind) => lines
				.Where(l => l.StartsWith(kind + "\t", StringComparison.Ordinal))
				.Distinct(StringComparer.Ordinal)
				.Select(l => l.Split('\t'))
				.ToList();

			var dependencySkips = Distinct("SKIPDEP");
			var unclosed        = Distinct("SKIPGEN");
			var canaries        = Distinct("CANARY");

			var unverifiedIds = new HashSet<string>(records.Where(r => r[0] == "UNVERIFIED").Select(r => r[3]), StringComparer.Ordinal);

			var exitCode = ExitOk;

			Console.WriteLine("FPU JIT sweep: x86, FP invalid/zero-divide/overflow exceptions unmasked");
			Console.WriteLine();

			foreach (var asm in ExpectedAssemblies)
			{
				// distinct methods: a method retried after a control-word reset writes OK twice; unverified ones do not count
				var ok      = records.Where(r => r[0] == "OK" && r[1] == asm && !unverifiedIds.Contains(r[2])).Select(r => r[2]).Distinct(StringComparer.Ordinal).Count();
				var failed  = faults.Count(r => r[1] == asm);
				var skips   = dependencySkips.Where(r => r[1] == asm).ToList();
				var allowed = skips.Count(r => IsAllowedSkip(asm, r[4]));
				var generic = unclosed.Count(r => r[1] == asm);

				Console.WriteLine($"  {asm,-34} ok={ok,6} faults={failed,3} dependency-skips={skips.Count,4} (allowed {allowed}) generic-not-closable={generic,5}");

				if (ok == 0)
				{
					Console.WriteLine($"ERROR: no method of {asm} was compiled");
					exitCode = ExitEnvironment;
				}

				foreach (var skip in skips.Where(r => !IsAllowedSkip(asm, r[4])))
				{
					Console.WriteLine($"ERROR: dependency-load skip in {asm}: {skip[3]} — {skip[4]}");
					exitCode = ExitEnvironment;
				}
			}

			Console.WriteLine();

			if (canaries.Count == 0)
			{
				Console.WriteLine("ERROR: no canary was enumerated");
				exitCode = ExitHarness;
			}

			foreach (var canary in canaries)
			{
				var fault = faults.FirstOrDefault(r => r[1] == CanaryAssembly && r[2] == canary[1]);
				if (fault == null)
				{
					Console.WriteLine($"ERROR: canary did not fault: {canary[2]} — this environment or instantiation path does not reproduce the FPU JIT fault, so the sweep proves nothing");
					exitCode = ExitEnvironment;
				}
				else
					Console.WriteLine($"canary faulted as expected: {canary[2]} — {fault[4]}");
			}

			var resets = records.Where(r => r[0] == "FPURESET").ToList();
			if (resets.Count > 0)
				Console.WriteLine($"FPU control word was reset while preparing {resets.Count} method(s); first: {resets[0][1]} {resets[0][2]}");

			foreach (var unverified in records.Where(r => r[0] == "UNVERIFIED"))
			{
				var allowed = AllowedUnverified.FirstOrDefault(a => a.Assembly == unverified[1] && unverified[2].StartsWith(a.MethodPrefix, StringComparison.Ordinal));
				if (allowed.Reason != null)
					Console.WriteLine($"unverified (allowed) {unverified[1]} {unverified[2]}");
				else
				{
					Console.WriteLine($"ERROR: unverified {unverified[1]} {unverified[2]} — compiling it resets the FPU control word even in a fresh process");
					exitCode = ExitEnvironment;
				}
			}

			var real = faults.Where(r => r[1] != CanaryAssembly).ToList();

			foreach (var fault in real)
				Console.WriteLine($"{fault[0]} {fault[1]} {fault[3]} — {fault[4]}");

			Console.WriteLine();
			Console.WriteLine($"result: {real.Count} JIT fault(s)");

			if (exitCode == ExitOk && real.Count > 0)
				exitCode = ExitJitFaults;

			return exitCode;
		}

		static bool IsAllowedSkip(string asm, string message)
		{
			return AllowedDependencySkips.Any(a => a.Assembly == asm && message.Contains(a.MessagePart));
		}

		#endregion

		#region Child

		sealed class Item(string asm, string id, string display, MethodBase method, Type type, RuntimeTypeHandle[]? instantiation)
		{
			public string               Asm           { get; } = asm;
			public string               Id            { get; } = id;
			public string               Display       { get; } = display;
			public MethodBase           Method        { get; } = method;
			// the declaring type, closed when generic
			public Type                 Type          { get; } = type;
			// type then method arguments of a generic method or a method of a generic type
			public RuntimeTypeHandle[]? Instantiation { get; } = instantiation;
		}

		[HandleProcessCorruptedStateExceptions, SecurityCritical]
		[SuppressMessage("Security", "CA2153:Do Not Catch Corrupted State Exceptions", Justification = "Catching the JIT's FP fault is the purpose of the sweep")]
		static int RunChild(string resultsPath, string? resumeId, bool resumeAt)
		{
			// methods whose compilation reset the control word in an earlier child: their type initializers are run
			// up front, so this child compiles what follows unmasked
			var warmIds = new HashSet<string>(
				File.Exists(resultsPath)
					? File.ReadAllLines(resultsPath).Where(l => l.StartsWith("RESTART\t", StringComparison.Ordinal)).Select(l => l.Split('\t')[1])
					: [],
				StringComparer.Ordinal);

			using var writer = new StreamWriter(resultsPath, append: true) { AutoFlush = true };

			void Write(string line) => writer.Write(line + "\n");

			_control87(0, FpuExceptionMask);

			if (!IsUnmasked())
			{
				Write($"ERROR\tFPU control word unchanged after _control87 (0x{_control87(0, 0):X8})");
				return ExitEnvironment;
			}

			var baseDir = AppDomain.CurrentDomain.BaseDirectory;
			foreach (var asm in ExpectedAssemblies)
			{
				if (!File.Exists(Path.Combine(baseDir, asm + ".dll")))
				{
					Write($"ERROR\texpected assembly missing: {asm}.dll");
					return ExitEnvironment;
				}
			}

			var skipping = resumeId != null;

			foreach (var item in Enumerate(baseDir, Write))
			{
				if (skipping)
				{
					if (warmIds.Contains(item.Id))
						RunTypeInitializers(item.Type);

					if (item.Id != resumeId)
						continue;

					skipping = false;
					if (!resumeAt)
						continue;
				}

				// Code run while preparing (type initializers, native DLL init) can reset the control word,
				// so re-arm before every method rather than trusting the previous state.
				_control87(0, FpuExceptionMask);

				if (!IsUnmasked())
				{
					Write($"ERROR\tFPU control word masked (0x{_control87(0, 0):X8}) before {item.Display}");
					return ExitEnvironment;
				}

				Write($"PREP\t{item.Asm}\t{item.Id}\t{item.Display}");

				string? restart = null;
				try
				{
					if (item.Instantiation == null)
						RuntimeHelpers.PrepareMethod(item.Method.MethodHandle);
					else
						RuntimeHelpers.PrepareMethod(item.Method.MethodHandle, item.Instantiation);
					Write($"OK\t{item.Asm}\t{item.Id}");
					restart = "at";
				}
				catch (Exception ex) when (IsDependencyFailure(ex))
				{
					// a failed bind resets the control word itself; any such skip fails the sweep
					Write($"SKIPDEP\t{item.Asm}\t{item.Id}\t{item.Display}\t{Describe(ex)}");
				}
				catch (Exception ex)
				{
					Write($"FAIL\t{item.Asm}\t{item.Id}\t{item.Display}\t{ex.GetType().Name}: {OneLine(ex.Message)}");
					restart = "after";
				}

				// Whatever compiled after the reset was compiled masked, and a later PrepareMethod on it is a no-op:
				// continue in a fresh process, retrying this method unless it already faulted.
				if (!IsUnmasked())
				{
					Write($"FPURESET\t{item.Asm}\t{item.Display}");

					if (restart != null)
					{
						Write($"RESTART\t{item.Id}\t{restart}");
						return ExitRestart;
					}
				}
			}

			if (skipping)
			{
				Write($"ERROR\tresume point {resumeId} not found");
				return ExitHarness;
			}

			Write("DONE");
			return ExitOk;
		}

		static IEnumerable<Item> Enumerate(string baseDir, Action<string> write)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

			var canaries = new[]
			{
				typeof(Program).GetMethod(nameof(Canary), flags)!,
				typeof(Program).GetMethod(nameof(GenericCanary), flags)!,
				typeof(GenericCanaryType<>).GetMethod(nameof(GenericCanaryType<>.Canary), flags)!,
			};

			var known = new Dictionary<Type, Type[]>();

			foreach (var canary in canaries)
			{
				var item = CreateItem(CanaryAssembly, canary.DeclaringType!, canary, known, out var notClosable)
					?? throw new InvalidOperationException($"canary {canary} not closable: {notClosable}");

				write($"CANARY\t{item.Id}\t{item.Display}");
				yield return item;
			}

			foreach (var asmName in ExpectedAssemblies)
			{
				var asm = Assembly.Load(AssemblyName.GetAssemblyName(Path.Combine(baseDir, asmName + ".dll")));

				Type[] types;
				try
				{
					types = asm.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					foreach (var loaderException in ex.LoaderExceptions.Where(e => e != null).Select(e => Describe(e!)).Distinct(StringComparer.Ordinal))
						write($"SKIPDEP\t{asmName}\t{asmName}:types\t(type load)\t{loaderException}");

					types = ex.Types.Where(t => t != null).ToArray()!;
				}

				CollectInstantiations(types, known);

				foreach (var type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
					foreach (var item in MethodsOf(asmName, type, known, write))
						yield return item;
			}
		}

		// Inspecting a method (body, signature text) resolves its parameter types, so a missing dependency
		// surfaces here rather than in PrepareMethod.
		static List<Item> MethodsOf(string asmName, Type type, Dictionary<Type, Type[]> known, Action<string> write)
		{
			const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

			var items = new List<Item>();

			MethodBase[] methods;
			try
			{
				methods = type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)).ToArray();
			}
			catch (Exception ex) when (IsDependencyFailure(ex))
			{
				write($"SKIPDEP\t{asmName}\t{asmName}:type:{type.FullName}\t{type.FullName}\t{Describe(ex)}");
				return items;
			}

			foreach (var method in methods.OrderBy(m => m.MetadataToken))
			{
				try
				{
					if (method.IsAbstract || method.GetMethodBody() == null)
						continue;

					var item = CreateItem(asmName, type, method, known, out var notClosable);
					if (item != null)
						items.Add(item);
					else
						write($"SKIPGEN\t{asmName}\t{asmName}:{method.MetadataToken:X8}\t{type.FullName}::{method}\t{notClosable}");
				}
				catch (Exception ex) when (IsDependencyFailure(ex))
				{
					write($"SKIPDEP\t{asmName}\t{asmName}:{method.MetadataToken:X8}\t{type.FullName}::{method.Name}\t{Describe(ex)}");
				}
			}

			return items;
		}

		// Generic code is swept once per definition: reference-type arguments share one compiled body, so closing a
		// parameter with object (or its single class/interface constraint) covers every reference-type instantiation.
		// Value-type instantiations each get their own body; only int (or an enum) is tried for struct-constrained
		// parameters. Self-referential constraints fall back to an instantiation the swept assemblies themselves use.
		static Item? CreateItem(string asmName, Type type, MethodBase method, Dictionary<Type, Type[]> known, out string? notClosable)
		{
			notClosable = null;

			var closedType = type;
			var typeArgs   = Type.EmptyTypes;
			var closed     = method;
			var methodArgs = Type.EmptyTypes;

			try
			{
				if (type.ContainsGenericParameters)
				{
					foreach (var candidate in TypeArgumentCandidates(type, known))
					{
						try
						{
							closedType = type.MakeGenericType(candidate);
							typeArgs   = candidate;
							break;
						}
						catch (ArgumentException ex)
						{
							notClosable = $"{ex.GetType().Name}: {OneLine(ex.Message)}";
						}
					}

					if (typeArgs.Length == 0)
						return null;

					notClosable = null;
					closed      = MethodBase.GetMethodFromHandle(method.MethodHandle, closedType.TypeHandle)!;
				}

				if (method.IsGenericMethodDefinition)
				{
					var definition = (MethodInfo)closed;
					var parameters = method.GetGenericArguments();

					foreach (var candidate in SelfReferenceFallbacks.Select(f => parameters.Select(p => Substitute(p, f)).ToArray()))
					{
						try
						{
							closed     = definition.MakeGenericMethod(candidate);
							methodArgs = candidate;
							break;
						}
						catch (ArgumentException ex)
						{
							notClosable = $"{ex.GetType().Name}: {OneLine(ex.Message)}";
						}
					}

					if (methodArgs.Length == 0)
						return null;

					notClosable = null;
				}
			}
			catch (ArgumentException ex)
			{
				notClosable = $"{ex.GetType().Name}: {OneLine(ex.Message)}";
				return null;
			}

			var instantiation = typeArgs.Length + methodArgs.Length == 0
				? null
				: typeArgs.Concat(methodArgs).Select(t => t.TypeHandle).ToArray();

			return new Item(asmName, $"{asmName}:{method.MetadataToken:X8}", $"{closedType}::{closed}", method, closedType, instantiation);
		}

		static IEnumerable<Type[]> TypeArgumentCandidates(Type type, Dictionary<Type, Type[]> known)
		{
			var parameters = type.GetGenericArguments();

			foreach (var fallback in SelfReferenceFallbacks)
				yield return parameters.Select(p => Substitute(p, fallback)).ToArray();

			// a nested type of a generic type (closures, iterators) leads with its outer type's parameters
			for (var outer = type; outer != null; outer = outer.DeclaringType)
			{
				if (outer.IsGenericTypeDefinition && known.TryGetValue(outer, out var args) && args.Length <= parameters.Length)
					yield return args.Concat(parameters.Skip(args.Length).Select(p => Substitute(p, null))).ToArray();
			}
		}

		static void CollectInstantiations(IEnumerable<Type> types, Dictionary<Type, Type[]> known)
		{
			void Add(Type? type)
			{
				if (type is { IsConstructedGenericType: true, ContainsGenericParameters: false } && !known.ContainsKey(type.GetGenericTypeDefinition()))
					known.Add(type.GetGenericTypeDefinition(), type.GetGenericArguments());
			}

			foreach (var type in types.Where(t => !t.ContainsGenericParameters))
			{
				try
				{
					for (var b = type.BaseType; b != null; b = b.BaseType)
						Add(b);

					foreach (var i in type.GetInterfaces())
						Add(i);
				}
				catch (Exception ex) when (IsDependencyFailure(ex))
				{
					// reported by the method enumeration of the same type
				}
			}
		}

		// substitutes tried, in order, for a parameter constrained by itself (T : IComparable<T>)
		static readonly Type?[] SelfReferenceFallbacks = [null, typeof(string), typeof(int)];

		static Type Substitute(Type parameter, Type? selfReferenceFallback)
		{
			var all         = parameter.GetGenericParameterConstraints();
			var constraints = all.Where(c => !c.ContainsGenericParameters).ToArray();

			if (constraints.Contains(typeof(Enum)))
				return typeof(DayOfWeek);

			if (selfReferenceFallback != null && all.Length > constraints.Length)
				return selfReferenceFallback;

			if (parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
				return typeof(int);

			return constraints.Length == 1 ? constraints[0] : typeof(object);
		}

		static void RunTypeInitializers(Type declaringType)
		{
			_control87(FpuExceptionMask, FpuExceptionMask);

			for (var type = declaringType; type != null; type = type.DeclaringType)
			{
				if (type.ContainsGenericParameters)
					continue;

				try
				{
					RuntimeHelpers.RunClassConstructor(type.TypeHandle);
				}
				catch (TypeInitializationException)
				{
				}
			}
		}

		static bool IsUnmasked() => (_control87(0, 0) & FpuExceptionMask) == 0;

		static bool IsDependencyFailure(Exception ex) =>
			ex is TypeLoadException or FileNotFoundException or FileLoadException or BadImageFormatException or MissingMemberException;

		static string Describe(Exception ex) => ex switch
		{
			FileNotFoundException fnf => $"{ex.GetType().Name}: {fnf.FileName}",
			TypeLoadException     tle => $"{ex.GetType().Name}: {tle.TypeName}: {OneLine(ex.Message)}",
			_                         => $"{ex.GetType().Name}: {OneLine(ex.Message)}",
		};

		static string OneLine(string message) => message.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');

		#endregion
	}
}
