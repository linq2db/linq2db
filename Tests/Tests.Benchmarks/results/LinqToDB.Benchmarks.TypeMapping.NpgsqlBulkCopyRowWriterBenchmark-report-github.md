``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|       Method |              Runtime | Mean | Ratio |
|------------- |--------------------- |-----:|------:|
|   TypeMapper |             .NET 5.0 |   NA |     ? |
| DirectAccess |             .NET 5.0 |   NA |     ? |
|   TypeMapper |        .NET Core 3.1 |   NA |     ? |
| DirectAccess |        .NET Core 3.1 |   NA |     ? |
|   TypeMapper | .NET Framework 4.7.2 |   NA |     ? |
| DirectAccess | .NET Framework 4.7.2 |   NA |     ? |

Benchmarks with issues:
  NpgsqlBulkCopyRowWriterBenchmark.TypeMapper: Job-GUCTZK(Jit=RyuJit, Platform=X64, Runtime=.NET 5.0)
  NpgsqlBulkCopyRowWriterBenchmark.DirectAccess: Job-GUCTZK(Jit=RyuJit, Platform=X64, Runtime=.NET 5.0)
  NpgsqlBulkCopyRowWriterBenchmark.TypeMapper: Job-IOHEYN(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1)
  NpgsqlBulkCopyRowWriterBenchmark.DirectAccess: Job-IOHEYN(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1)
  NpgsqlBulkCopyRowWriterBenchmark.TypeMapper: Job-FWTWYQ(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2)
  NpgsqlBulkCopyRowWriterBenchmark.DirectAccess: Job-FWTWYQ(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2)
