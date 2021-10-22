``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |         Mean |       Median |  Ratio | Allocated |
|---------------------- |--------------------- |-------------:|-------------:|-------:|----------:|
|                  Linq |             .NET 5.0 | 173,470.3 ns | 155,061.7 ns | 313.27 |  17,904 B |
|              Compiled |             .NET 5.0 |  36,464.2 ns |  28,086.7 ns |  62.08 |   2,688 B |
| FromSql_Interpolation |             .NET 5.0 |  93,484.1 ns |  81,041.7 ns | 168.07 |   9,576 B |
|   FromSql_Formattable |             .NET 5.0 | 118,523.1 ns | 100,643.8 ns | 190.63 |  10,440 B |
|                 Query |             .NET 5.0 |   1,120.0 ns |   1,111.4 ns |   1.89 |     464 B |
|               Execute |             .NET 5.0 |   1,059.9 ns |   1,042.5 ns |   1.81 |     344 B |
|             RawAdoNet |             .NET 5.0 |     301.5 ns |     302.0 ns |   0.51 |     328 B |
|                  Linq |        .NET Core 3.1 |  78,256.5 ns |  78,171.1 ns | 131.83 |  16,613 B |
|              Compiled |        .NET Core 3.1 |   7,673.1 ns |   7,676.9 ns |  12.93 |   2,656 B |
| FromSql_Interpolation |        .NET Core 3.1 | 178,583.0 ns | 143,358.9 ns | 221.42 |   9,528 B |
|   FromSql_Formattable |        .NET Core 3.1 | 214,465.7 ns | 181,100.4 ns | 266.62 |  10,392 B |
|                 Query |        .NET Core 3.1 |   1,378.7 ns |   1,379.5 ns |   2.32 |     464 B |
|               Execute |        .NET Core 3.1 |   1,239.1 ns |   1,241.4 ns |   2.09 |     344 B |
|             RawAdoNet |        .NET Core 3.1 |     451.5 ns |     451.0 ns |   0.76 |     328 B |
|                  Linq | .NET Framework 4.7.2 | 104,997.2 ns | 103,899.2 ns | 178.04 |  19,006 B |
|              Compiled | .NET Framework 4.7.2 |  10,443.9 ns |  10,441.9 ns |  17.59 |   2,873 B |
| FromSql_Interpolation | .NET Framework 4.7.2 |  45,442.8 ns |  45,427.7 ns |  76.55 |   8,746 B |
|   FromSql_Formattable | .NET Framework 4.7.2 |  52,130.9 ns |  52,098.1 ns |  87.82 |  10,255 B |
|                 Query | .NET Framework 4.7.2 |   1,782.8 ns |   1,779.4 ns |   3.00 |     481 B |
|               Execute | .NET Framework 4.7.2 |   1,744.2 ns |   1,742.3 ns |   2.94 |     361 B |
|             RawAdoNet | .NET Framework 4.7.2 |     593.6 ns |     593.1 ns |   1.00 |     393 B |
