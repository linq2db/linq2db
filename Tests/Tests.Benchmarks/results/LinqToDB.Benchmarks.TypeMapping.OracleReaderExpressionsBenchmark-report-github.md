``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                                 Method |       Runtime |       Mean | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------:|------:|------:|------:|------:|----------:|
|        TypeMapperReadOracleTimeStampTZ |    .NET 4.6.2 | 129.497 ns |  2.14 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ |    .NET 4.6.2 |  60.647 ns |  1.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ |    .NET 4.6.2 | 138.949 ns |  2.29 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ |    .NET 4.6.2 |  61.201 ns |  1.01 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv |    .NET 4.6.2 |  39.916 ns |  0.66 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv |    .NET 4.6.2 |   6.384 ns |  0.11 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal |    .NET 4.6.2 |  26.224 ns |  0.43 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal |    .NET 4.6.2 |   5.968 ns |  0.10 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt |    .NET 4.6.2 |  29.067 ns |  0.48 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt |    .NET 4.6.2 |   8.924 ns |  0.15 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong |    .NET 4.6.2 |  29.713 ns |  0.49 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong |    .NET 4.6.2 |   9.005 ns |  0.15 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 2.1 |  64.720 ns |  1.07 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 2.1 |  61.070 ns |  1.01 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 2.1 |  64.598 ns |  1.07 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 2.1 |  62.522 ns |  1.03 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 2.1 |  20.838 ns |  0.34 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 2.1 |   9.333 ns |  0.15 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 2.1 |  10.606 ns |  0.18 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 2.1 |   8.698 ns |  0.14 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 2.1 |  12.354 ns |  0.20 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 2.1 |  10.795 ns |  0.18 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 2.1 |  12.243 ns |  0.20 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 2.1 |  10.768 ns |  0.18 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 3.1 |  68.432 ns |  1.13 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 3.1 |  61.173 ns |  1.01 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 3.1 |  69.777 ns |  1.15 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 3.1 |  63.957 ns |  1.06 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 3.1 |  12.739 ns |  0.21 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 3.1 |   7.887 ns |  0.13 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 3.1 |   7.059 ns |  0.12 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 3.1 |   3.538 ns |  0.06 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 3.1 |   8.981 ns |  0.15 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 3.1 |   7.253 ns |  0.12 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 3.1 |   9.911 ns |  0.16 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 3.1 |   6.173 ns |  0.10 |     - |     - |     - |         - |
