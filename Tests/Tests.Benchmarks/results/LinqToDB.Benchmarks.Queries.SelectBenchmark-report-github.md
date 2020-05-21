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
|                Method |       Runtime |         Mean |       Median |    Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |-------------- |-------------:|-------------:|---------:|-------:|------:|------:|----------:|
|                  Linq |    .NET 4.6.2 | 184,226.9 ns | 169,104.9 ns |   951.46 |      - |     - |     - |   16384 B |
|              Compiled |    .NET 4.6.2 |  52,584.8 ns |  45,787.1 ns |   252.04 |      - |     - |     - |         - |
| FromSql_Interpolation |    .NET 4.6.2 | 100,313.2 ns |  88,355.9 ns |   498.90 |      - |     - |     - |         - |
|   FromSql_Formattable |    .NET 4.6.2 | 126,602.6 ns | 109,128.3 ns |   637.26 |      - |     - |     - |         - |
|                 Query |    .NET 4.6.2 |     584.0 ns |     576.5 ns |     2.86 | 0.1011 |     - |     - |     425 B |
|               Execute |    .NET 4.6.2 |     528.1 ns |     527.2 ns |     2.60 | 0.0763 |     - |     - |     321 B |
|             RawAdoNet |    .NET 4.6.2 |     201.3 ns |     198.8 ns |     1.00 | 0.0496 |     - |     - |     209 B |
|                  Linq | .NET Core 2.1 | 264,515.0 ns | 235,810.7 ns | 1,280.17 |      - |     - |     - |    8456 B |
|              Compiled | .NET Core 2.1 |  24,524.2 ns |  19,894.7 ns |   119.27 |      - |     - |     - |         - |
| FromSql_Interpolation | .NET Core 2.1 |  81,085.7 ns |  60,561.8 ns |   414.89 |      - |     - |     - |         - |
|   FromSql_Formattable | .NET Core 2.1 |  69,952.6 ns |  59,099.0 ns |   352.61 |      - |     - |     - |         - |
|                 Query | .NET Core 2.1 |     494.6 ns |     488.4 ns |     2.43 | 0.0968 |     - |     - |     408 B |
|               Execute | .NET Core 2.1 |     446.3 ns |     445.0 ns |     2.18 | 0.0725 |     - |     - |     304 B |
|             RawAdoNet | .NET Core 2.1 |     138.9 ns |     137.5 ns |     0.70 | 0.0436 |     - |     - |     184 B |
|                  Linq | .NET Core 3.1 | 134,381.1 ns | 112,639.1 ns |   680.44 |      - |     - |     - |    8360 B |
|              Compiled | .NET Core 3.1 |  41,847.0 ns |  26,623.8 ns |   208.17 |      - |     - |     - |         - |
| FromSql_Interpolation | .NET Core 3.1 | 123,562.7 ns | 102,252.9 ns |   579.39 |      - |     - |     - |         - |
|   FromSql_Formattable | .NET Core 3.1 |  24,466.1 ns |  24,477.9 ns |   117.09 | 1.4648 |     - |     - |    6192 B |
|                 Query | .NET Core 3.1 |     485.5 ns |     481.6 ns |     2.36 | 0.0973 |     - |     - |     408 B |
|               Execute | .NET Core 3.1 |     429.9 ns |     424.7 ns |     2.14 | 0.0725 |     - |     - |     304 B |
|             RawAdoNet | .NET Core 3.1 |     121.6 ns |     121.2 ns |     0.58 | 0.0439 |     - |     - |     184 B |
