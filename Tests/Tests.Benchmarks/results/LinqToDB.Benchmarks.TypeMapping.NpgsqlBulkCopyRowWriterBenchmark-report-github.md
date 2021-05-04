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
|       Method |              Runtime |      Mean |    Median | Ratio | Allocated |
|------------- |--------------------- |----------:|----------:|------:|----------:|
|   TypeMapper |             .NET 5.0 |  59.66 ns |  58.55 ns |  0.52 |      24 B |
| DirectAccess |             .NET 5.0 |  83.84 ns |  78.57 ns |  0.74 |      24 B |
|   TypeMapper |        .NET Core 3.1 |  53.47 ns |  53.50 ns |  0.47 |      24 B |
| DirectAccess |        .NET Core 3.1 |  51.83 ns |  51.50 ns |  0.45 |      24 B |
|   TypeMapper | .NET Framework 4.7.2 | 196.34 ns | 189.37 ns |  1.72 |      24 B |
| DirectAccess | .NET Framework 4.7.2 | 115.93 ns | 117.42 ns |  1.00 |      24 B |
