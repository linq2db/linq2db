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
|          Linq |             .NET 5.0 | 3.438 ms | 3.433 ms |  0.83 |      2 MB |
|     LinqAsync |             .NET 5.0 | 3.695 ms | 3.680 ms |  0.89 |      2 MB |
|      Compiled |             .NET 5.0 | 3.399 ms | 3.402 ms |  0.82 |      2 MB |
| CompiledAsync |             .NET 5.0 | 3.468 ms | 3.462 ms |  0.83 |      2 MB |
|          Linq |        .NET Core 3.1 | 3.882 ms | 3.877 ms |  0.93 |      2 MB |
|     LinqAsync |        .NET Core 3.1 | 3.777 ms | 3.770 ms |  0.91 |      2 MB |
|      Compiled |        .NET Core 3.1 | 3.791 ms | 3.803 ms |  0.90 |      2 MB |
| CompiledAsync |        .NET Core 3.1 | 3.834 ms | 3.845 ms |  0.92 |      2 MB |
|          Linq | .NET Framework 4.7.2 | 4.509 ms | 4.459 ms |  1.08 |      2 MB |
|     LinqAsync | .NET Framework 4.7.2 | 4.492 ms | 4.393 ms |  1.06 |      2 MB |
|      Compiled | .NET Framework 4.7.2 | 4.223 ms | 4.168 ms |  1.00 |      2 MB |
| CompiledAsync | .NET Framework 4.7.2 | 4.313 ms | 4.285 ms |  1.02 |      2 MB |
