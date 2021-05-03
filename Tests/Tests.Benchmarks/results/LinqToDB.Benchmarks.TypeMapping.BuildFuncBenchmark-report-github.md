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
|       Method |              Runtime |      Mean | Ratio | Allocated |
|------------- |--------------------- |----------:|------:|----------:|
|    BuildFunc |             .NET 5.0 |  3.808 ns |  1.78 |         - |
| DirectAccess |             .NET 5.0 |  2.126 ns |  0.99 |         - |
|    BuildFunc |        .NET Core 3.1 |  3.500 ns |  1.63 |         - |
| DirectAccess |        .NET Core 3.1 |  1.928 ns |  0.90 |         - |
|    BuildFunc | .NET Framework 4.7.2 | 10.419 ns |  4.87 |         - |
| DirectAccess | .NET Framework 4.7.2 |  2.142 ns |  1.00 |         - |
