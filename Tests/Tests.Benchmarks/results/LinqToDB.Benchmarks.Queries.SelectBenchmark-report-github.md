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
|                Method |       Runtime |         Mean |       Median |    Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |-------------- |-------------:|-------------:|---------:|-------:|------:|------:|----------:|
|                  Linq |    .NET 4.6.2 | 112,518.3 ns |  84,991.3 ns |   591.51 |      - |     - |     - |   16384 B |
|              Compiled |    .NET 4.6.2 |  36,272.5 ns |  30,719.8 ns |   226.53 |      - |     - |     - |         - |
| FromSql_Interpolation |    .NET 4.6.2 | 121,640.8 ns |  91,866.7 ns |   575.42 |      - |     - |     - |         - |
|   FromSql_Formattable |    .NET 4.6.2 | 154,965.2 ns | 136,337.2 ns |   943.73 |      - |     - |     - |         - |
|                 Query |    .NET 4.6.2 |     565.0 ns |     564.4 ns |     2.99 | 0.1087 |     - |     - |     457 B |
|               Execute |    .NET 4.6.2 |     509.5 ns |     509.6 ns |     2.69 | 0.0839 |     - |     - |     353 B |
|             RawAdoNet |    .NET 4.6.2 |     189.7 ns |     189.9 ns |     1.00 | 0.0439 |     - |     - |     185 B |
|                  Linq | .NET Core 2.1 | 133,078.1 ns | 105,617.5 ns |   739.17 |      - |     - |     - |    9016 B |
|              Compiled | .NET Core 2.1 |  27,228.9 ns |  23,113.0 ns |   174.79 |      - |     - |     - |         - |
| FromSql_Interpolation | .NET Core 2.1 |  65,871.8 ns |  56,173.3 ns |   347.34 |      - |     - |     - |         - |
|   FromSql_Formattable | .NET Core 2.1 |  69,683.5 ns |  59,245.3 ns |   364.61 |      - |     - |     - |         - |
|                 Query | .NET Core 2.1 |     504.2 ns |     497.5 ns |     2.67 | 0.1040 |     - |     - |     440 B |
|               Execute | .NET Core 2.1 |     442.0 ns |     440.1 ns |     2.33 | 0.0801 |     - |     - |     336 B |
|             RawAdoNet | .NET Core 2.1 |     127.7 ns |     127.6 ns |     0.67 | 0.0379 |     - |     - |     160 B |
|                  Linq | .NET Core 3.1 | 236,650.3 ns | 162,522.2 ns | 1,493.44 |      - |     - |     - |    8904 B |
|              Compiled | .NET Core 3.1 |  44,452.4 ns |  37,448.9 ns |   259.75 |      - |     - |     - |         - |
| FromSql_Interpolation | .NET Core 3.1 |  21,784.8 ns |  21,819.0 ns |   115.22 | 1.4648 |     - |     - |    6161 B |
|   FromSql_Formattable | .NET Core 3.1 |  26,217.7 ns |  25,866.7 ns |   136.11 | 1.6174 |     - |     - |    6848 B |
|                 Query | .NET Core 3.1 |     547.8 ns |     543.6 ns |     2.90 | 0.1049 |     - |     - |     440 B |
|               Execute | .NET Core 3.1 |     433.3 ns |     422.7 ns |     2.29 | 0.0801 |     - |     - |     336 B |
|             RawAdoNet | .NET Core 3.1 |     123.1 ns |     123.1 ns |     0.65 | 0.0381 |     - |     - |     160 B |
