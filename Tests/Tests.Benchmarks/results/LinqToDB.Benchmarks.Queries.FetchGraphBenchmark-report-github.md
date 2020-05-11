``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-OGAWJV : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZLSLVN : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|        Method |       Runtime |     Mean |   Median | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------- |-------------- |---------:|---------:|------:|------:|------:|------:|----------:|
|          Linq |    .NET 4.6.2 | 2.522 ms | 2.424 ms |  0.96 |     - |     - |     - |   1.54 MB |
|     LinqAsync |    .NET 4.6.2 | 3.411 ms | 3.004 ms |  1.31 |     - |     - |     - |   1.54 MB |
|      Compiled |    .NET 4.6.2 | 2.778 ms | 2.384 ms |  1.00 |     - |     - |     - |   1.52 MB |
| CompiledAsync |    .NET 4.6.2 | 2.672 ms | 2.296 ms |  1.02 |     - |     - |     - |   1.52 MB |
|          Linq | .NET Core 2.1 | 1.948 ms | 1.730 ms |  0.75 |     - |     - |     - |   1.52 MB |
|     LinqAsync | .NET Core 2.1 | 2.419 ms | 2.163 ms |  0.93 |     - |     - |     - |   1.52 MB |
|      Compiled | .NET Core 2.1 | 1.605 ms | 1.549 ms |  0.61 |     - |     - |     - |   1.51 MB |
| CompiledAsync | .NET Core 2.1 | 2.161 ms | 1.807 ms |  0.81 |     - |     - |     - |   1.51 MB |
|          Linq | .NET Core 3.1 | 3.163 ms | 2.934 ms |  1.22 |     - |     - |     - |   1.51 MB |
|     LinqAsync | .NET Core 3.1 | 2.913 ms | 2.705 ms |  1.12 |     - |     - |     - |   1.52 MB |
|      Compiled | .NET Core 3.1 | 2.144 ms | 1.847 ms |  0.82 |     - |     - |     - |    1.5 MB |
| CompiledAsync | .NET Core 3.1 | 2.013 ms | 1.743 ms |  0.78 |     - |     - |     - |   1.51 MB |
