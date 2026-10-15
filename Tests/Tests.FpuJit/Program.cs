// Regression gate for #6042 / #6051: hosts that unmask x87/SSE floating-point exceptions (C++Builder, Delphi)
// make the x86 .NET Framework JIT fault while compiling methods that use NaN/Infinity constants.
// The exe JIT-compiles every non-generic method of every netfx-shipped linq2db assembly with those exceptions
// unmasked. Not covered: generic instantiations and expression trees compiled at run time.
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
		static readonly (string Assembly, string MessagePart, string Reason)[] AllowedDependencySkips = [];

		const string CanaryAssembly   = "canary";
		const int    MaxRestarts      = 100;
		const int    ChildTimeoutMs   = 10 * 60 * 1000;
		const uint   FpuExceptionMask = 0x10 | 0x08 | 0x04; // _EM_INVALID | _EM_ZERODIVIDE | _EM_OVERFLOW

		const int ExitOk          = 0;
		const int ExitJitFaults   = 1;
		const int ExitEnvironment = 2;
		const int ExitHarness     = 3;

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
				return RunChild(args[1], args.Length >= 4 && args[2] == "--resume-after" ? args[3] : null);

			return RunParent();
		}

		// Positive control: must fault under the unmasked FPU, or the sweep proves nothing.
		[MethodImpl(MethodImplOptions.NoInlining)]
		static double Canary() => double.NaN;

		#region Parent

		static int RunParent()
		{
			var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fpujit-run");
			Directory.CreateDirectory(dir);

			var resultsPath = Path.Combine(dir, "results.txt");
			File.Delete(resultsPath);

			string? resumeAfter = null;

			for (var restart = 0; ; restart++)
			{
				if (restart > MaxRestarts)
				{
					Console.WriteLine($"ERROR: more than {MaxRestarts} child restarts");
					return ExitHarness;
				}

				var exitCode = SpawnChild(resultsPath, resumeAfter, out var timedOut);
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
				if (last == null || !last.StartsWith("PREP\t", StringComparison.Ordinal))
				{
					Console.WriteLine($"ERROR: child exited with 0x{exitCode:X8} outside a method prepare");
					return ExitHarness;
				}

				// PREP <asm> <id> <display>: the method being compiled when the child died
				var parts = last.Split('\t');
				File.AppendAllText(resultsPath, $"CRASH\t{parts[1]}\t{parts[2]}\t{parts[3]}\t{(timedOut ? "timeout" : $"process exit 0x{exitCode:X8}")}\n");

				resumeAfter = parts[2];
			}
		}

		static int SpawnChild(string resultsPath, string? resumeAfter, out bool timedOut)
		{
			var arguments = $"--child \"{resultsPath}\"";
			if (resumeAfter != null)
				arguments += $" --resume-after {resumeAfter}";

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

			// enumeration-time skips are re-written by every restarted child
			var dependencySkips = lines
				.Where(l => l.StartsWith("SKIPDEP\t", StringComparison.Ordinal))
				.Distinct(StringComparer.Ordinal)
				.Select(l => l.Split('\t'))
				.ToList();

			var exitCode = ExitOk;

			Console.WriteLine("FPU JIT sweep: x86, FP invalid/zero-divide/overflow exceptions unmasked");
			Console.WriteLine();

			foreach (var asm in ExpectedAssemblies)
			{
				var ok      = records.Count(r => r[0] == "OK" && r[1] == asm);
				var failed  = faults.Count(r => r[1] == asm);
				var skips   = dependencySkips.Where(r => r[1] == asm).ToList();
				var allowed = skips.Count(r => IsAllowedSkip(asm, r[4]));

				Console.WriteLine($"  {asm,-34} ok={ok,6} faults={failed,3} dependency-skips={skips.Count,4} (allowed {allowed})");

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

			var canary = faults.FirstOrDefault(r => r[1] == CanaryAssembly);
			if (canary == null)
			{
				Console.WriteLine("ERROR: canary did not fault — this environment does not reproduce the FPU JIT fault, so the sweep proves nothing");
				exitCode = ExitEnvironment;
			}
			else
				Console.WriteLine($"canary faulted as expected: {canary[4]}");

			var resets = records.Where(r => r[0] == "FPURESET").ToList();
			if (resets.Count > 0)
				Console.WriteLine($"FPU control word was reset while preparing {resets.Count} method(s), re-armed before the next; first: {resets[0][1]} {resets[0][2]}");

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

		sealed class Item(string asm, string id, string display, MethodBase method)
		{
			public string     Asm     { get; } = asm;
			public string     Id      { get; } = id;
			public string     Display { get; } = display;
			public MethodBase Method  { get; } = method;
		}

		[HandleProcessCorruptedStateExceptions, SecurityCritical]
		[SuppressMessage("Security", "CA2153:Do Not Catch Corrupted State Exceptions", Justification = "Catching the JIT's FP fault is the purpose of the sweep")]
		static int RunChild(string resultsPath, string? resumeAfter)
		{
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

			var skipping = resumeAfter != null;

			foreach (var item in Enumerate(baseDir, Write))
			{
				if (skipping)
				{
					if (item.Id == resumeAfter)
						skipping = false;
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

				try
				{
					RuntimeHelpers.PrepareMethod(item.Method.MethodHandle);
					Write($"OK\t{item.Asm}");
				}
				catch (Exception ex) when (IsDependencyFailure(ex))
				{
					Write($"SKIPDEP\t{item.Asm}\t{item.Id}\t{item.Display}\t{Describe(ex)}");
				}
				catch (Exception ex)
				{
					Write($"FAIL\t{item.Asm}\t{item.Id}\t{item.Display}\t{ex.GetType().Name}: {OneLine(ex.Message)}");
				}

				if (!IsUnmasked())
					Write($"FPURESET\t{item.Asm}\t{item.Display}");
			}

			if (skipping)
			{
				Write($"ERROR\tresume point {resumeAfter} not found");
				return ExitHarness;
			}

			Write("DONE");
			return ExitOk;
		}

		static IEnumerable<Item> Enumerate(string baseDir, Action<string> write)
		{
			var canary = typeof(Program).GetMethod(nameof(Canary), BindingFlags.NonPublic | BindingFlags.Static)!;
			yield return new Item(CanaryAssembly, $"{CanaryAssembly}:{canary.MetadataToken:X8}", $"{typeof(Program).FullName}::{canary}", canary);

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

				foreach (var type in types.Where(t => !t.ContainsGenericParameters).OrderBy(t => t.FullName, StringComparer.Ordinal))
					foreach (var item in MethodsOf(asmName, type, write))
						yield return item;
			}
		}

		// Inspecting a method (body, signature text) resolves its parameter types, so a missing dependency
		// surfaces here rather than in PrepareMethod.
		static List<Item> MethodsOf(string asmName, Type type, Action<string> write)
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
					if (!method.IsAbstract && !method.ContainsGenericParameters && method.GetMethodBody() != null)
						items.Add(new Item(asmName, $"{asmName}:{method.MetadataToken:X8}", $"{type.FullName}::{method}", method));
				}
				catch (Exception ex) when (IsDependencyFailure(ex))
				{
					write($"SKIPDEP\t{asmName}\t{asmName}:{method.MetadataToken:X8}\t{type.FullName}::{method.Name}\t{Describe(ex)}");
				}
			}

			return items;
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
