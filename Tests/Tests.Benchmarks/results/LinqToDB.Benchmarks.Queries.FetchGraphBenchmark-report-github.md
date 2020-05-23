``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417996 Hz, Resolution=292.5691 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-OGAWJV : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZLSLVN : .NET Core 2.1.18 (CoreCLR 4.6.28801.04, CoreFX 4.6.28802.05), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.4 (CoreCLR 4.700.20.20201, CoreFX 4.700.20.22101), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|        Method |       Runtime |     Mean |   Median | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------- |-------------- |---------:|---------:|------:|------:|------:|------:|----------:|
|          Linq |    .NET 4.6.2 | 2.399 ms | 2.344 ms |  1.10 |     - |     - |     - |   1.54 MB |
|     LinqAsync |    .NET 4.6.2 | 2.431 ms | 2.315 ms |  1.12 |     - |     - |     - |   1.54 MB |
|      Compiled |    .NET 4.6.2 | 2.194 ms | 2.122 ms |  1.00 |     - |     - |     - |   1.52 MB |
| CompiledAsync |    .NET 4.6.2 | 2.228 ms | 2.199 ms |  1.02 |     - |     - |     - |   1.52 MB |
|          Linq | .NET Core 2.1 | 1.610 ms | 1.567 ms |  0.74 |     - |     - |     - |   1.52 MB |
|     LinqAsync | .NET Core 2.1 | 1.854 ms | 1.814 ms |  0.85 |     - |     - |     - |   1.52 MB |
|      Compiled | .NET Core 2.1 | 1.808 ms | 1.640 ms |  0.83 |     - |     - |     - |   1.51 MB |
| CompiledAsync | .NET Core 2.1 | 1.451 ms | 1.438 ms |  0.67 |     - |     - |     - |   1.51 MB |
|          Linq | .NET Core 3.1 | 1.997 ms | 1.874 ms |  0.92 |     - |     - |     - |   1.51 MB |
|     LinqAsync | .NET Core 3.1 | 1.924 ms | 1.782 ms |  0.88 |     - |     - |     - |   1.52 MB |
|      Compiled | .NET Core 3.1 | 1.768 ms | 1.638 ms |  0.81 |     - |     - |     - |    1.5 MB |
| CompiledAsync | .NET Core 3.1 | 2.240 ms | 1.860 ms |  1.04 |     - |     - |     - |   1.51 MB |
