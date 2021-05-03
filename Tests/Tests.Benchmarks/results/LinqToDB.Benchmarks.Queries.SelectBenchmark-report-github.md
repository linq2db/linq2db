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
|                Method |              Runtime |         Mean |       Median |  Ratio | Allocated |
|---------------------- |--------------------- |-------------:|-------------:|-------:|----------:|
|                  Linq |             .NET 5.0 | 195,321.1 ns | 162,668.5 ns | 332.42 |   9,352 B |
|              Compiled |             .NET 5.0 |  60,845.2 ns |  47,103.6 ns |  97.92 |         - |
| FromSql_Interpolation |             .NET 5.0 | 111,580.9 ns |  99,181.0 ns | 185.18 |         - |
|   FromSql_Formattable |             .NET 5.0 |  21,591.2 ns |  21,380.4 ns |  36.04 |   5,920 B |
|                 Query |             .NET 5.0 |   1,002.0 ns |   1,003.6 ns |   1.74 |     568 B |
|               Execute |             .NET 5.0 |     933.5 ns |     923.7 ns |   1.56 |     464 B |
|             RawAdoNet |             .NET 5.0 |     299.3 ns |     299.0 ns |   0.52 |     328 B |
|                  Linq |        .NET Core 3.1 | 219,147.0 ns | 170,421.6 ns | 340.95 |  11,144 B |
|              Compiled |        .NET Core 3.1 |  55,388.7 ns |  45,787.1 ns |  88.73 |         - |
| FromSql_Interpolation |        .NET Core 3.1 | 189,795.6 ns | 168,519.9 ns | 312.14 |         - |
|   FromSql_Formattable |        .NET Core 3.1 |  24,962.1 ns |  24,671.0 ns |  42.97 |   5,888 B |
|                 Query |        .NET Core 3.1 |   1,287.4 ns |   1,284.5 ns |   2.24 |     568 B |
|               Execute |        .NET Core 3.1 |   1,218.7 ns |   1,203.4 ns |   2.11 |     464 B |
|             RawAdoNet |        .NET Core 3.1 |     476.7 ns |     477.4 ns |   0.83 |     328 B |
|                  Linq | .NET Framework 4.7.2 |  66,220.5 ns |  66,013.1 ns | 111.00 |   9,921 B |
|              Compiled | .NET Framework 4.7.2 |   9,465.2 ns |   9,427.5 ns |  16.38 |   3,065 B |
| FromSql_Interpolation | .NET Framework 4.7.2 |  28,825.0 ns |  28,815.4 ns |  48.28 |   5,376 B |
|   FromSql_Formattable | .NET Framework 4.7.2 |  31,460.3 ns |  31,167.1 ns |  54.43 |   5,842 B |
|                 Query | .NET Framework 4.7.2 |   1,538.8 ns |   1,543.3 ns |   2.66 |     586 B |
|               Execute | .NET Framework 4.7.2 |   1,419.3 ns |   1,416.6 ns |   2.39 |     481 B |
|             RawAdoNet | .NET Framework 4.7.2 |     594.9 ns |     588.1 ns |   1.00 |     393 B |
