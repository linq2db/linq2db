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
|        Method |              Runtime |     Mean | Ratio | Allocated |
|-------------- |--------------------- |---------:|------:|----------:|
|          Linq |             .NET 5.0 | 3.770 ms |  0.78 |      1 MB |
|     LinqAsync |             .NET 5.0 | 5.114 ms |  1.05 |      1 MB |
|      Compiled |             .NET 5.0 | 3.703 ms |  0.76 |      1 MB |
| CompiledAsync |             .NET 5.0 | 3.910 ms |  0.80 |      1 MB |
|          Linq |        .NET Core 3.1 | 4.226 ms |  0.88 |      1 MB |
|     LinqAsync |        .NET Core 3.1 | 4.450 ms |  0.92 |      1 MB |
|      Compiled |        .NET Core 3.1 | 4.160 ms |  0.86 |      1 MB |
| CompiledAsync |        .NET Core 3.1 | 5.019 ms |  1.04 |      1 MB |
|          Linq | .NET Framework 4.7.2 | 4.600 ms |  0.95 |      2 MB |
|     LinqAsync | .NET Framework 4.7.2 | 4.751 ms |  0.98 |      2 MB |
|      Compiled | .NET Framework 4.7.2 | 4.883 ms |  1.00 |      2 MB |
| CompiledAsync | .NET Framework 4.7.2 | 6.035 ms |  1.24 |      2 MB |
