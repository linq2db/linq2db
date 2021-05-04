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
|        Method |              Runtime |     Mean |   Median | Ratio | Allocated |
|-------------- |--------------------- |---------:|---------:|------:|----------:|
|          Linq |             .NET 5.0 | 3.984 ms | 3.950 ms |  0.88 |      1 MB |
|     LinqAsync |             .NET 5.0 | 3.974 ms | 4.009 ms |  0.87 |      1 MB |
|      Compiled |             .NET 5.0 | 4.104 ms | 4.090 ms |  0.90 |      1 MB |
| CompiledAsync |             .NET 5.0 | 3.894 ms | 3.917 ms |  0.85 |      1 MB |
|          Linq |        .NET Core 3.1 | 4.306 ms | 4.351 ms |  0.95 |      1 MB |
|     LinqAsync |        .NET Core 3.1 | 4.559 ms | 4.541 ms |  1.01 |      1 MB |
|      Compiled |        .NET Core 3.1 | 4.343 ms | 4.331 ms |  0.95 |      1 MB |
| CompiledAsync |        .NET Core 3.1 | 4.450 ms | 4.413 ms |  0.99 |      1 MB |
|          Linq | .NET Framework 4.7.2 | 4.326 ms | 4.290 ms |  0.96 |      2 MB |
|     LinqAsync | .NET Framework 4.7.2 | 4.668 ms | 4.558 ms |  1.04 |      2 MB |
|      Compiled | .NET Framework 4.7.2 | 4.511 ms | 4.470 ms |  1.00 |      2 MB |
| CompiledAsync | .NET Framework 4.7.2 | 4.549 ms | 4.493 ms |  1.01 |      2 MB |
