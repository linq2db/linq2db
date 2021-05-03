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
|       Method |              Runtime |     Mean | Ratio | Allocated |
|------------- |--------------------- |---------:|------:|----------:|
|  BuildAction |             .NET 5.0 | 1.605 ns |  1.51 |         - |
| DirectAccess |             .NET 5.0 | 1.068 ns |  1.02 |         - |
|  BuildAction |        .NET Core 3.1 | 1.538 ns |  1.47 |         - |
| DirectAccess |        .NET Core 3.1 | 1.079 ns |  1.02 |         - |
|  BuildAction | .NET Framework 4.7.2 | 7.959 ns |  7.63 |         - |
| DirectAccess | .NET Framework 4.7.2 | 1.055 ns |  1.00 |         - |
