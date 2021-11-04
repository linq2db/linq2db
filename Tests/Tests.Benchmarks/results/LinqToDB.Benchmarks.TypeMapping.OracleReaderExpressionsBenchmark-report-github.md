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
|                                 Method |              Runtime |       Mean | Ratio | Allocated |
|--------------------------------------- |--------------------- |-----------:|------:|----------:|
|        TypeMapperReadOracleTimeStampTZ |             .NET 5.0 |  47.123 ns |  0.79 |         - |
|      DirectAccessReadOracleTimeStampTZ |             .NET 5.0 |  43.641 ns |  0.73 |         - |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 5.0 |  50.234 ns |  0.84 |         - |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 5.0 |  45.943 ns |  0.77 |         - |
|         TypeMapperReadOracleDecimalAdv |             .NET 5.0 |  12.268 ns |  0.21 |         - |
|       DirectAccessReadOracleDecimalAdv |             .NET 5.0 |   6.948 ns |  0.12 |         - |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 5.0 |   6.981 ns |  0.12 |         - |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 5.0 |   3.228 ns |  0.05 |         - |
|       TypeMapperReadOracleDecimalAsInt |             .NET 5.0 |   9.071 ns |  0.15 |         - |
|     DirectAccessReadOracleDecimalAsInt |             .NET 5.0 |   6.416 ns |  0.11 |         - |
|      TypeMapperReadOracleDecimalAsLong |             .NET 5.0 |   9.045 ns |  0.15 |         - |
|    DirectAccessReadOracleDecimalAsLong |             .NET 5.0 |   6.486 ns |  0.11 |         - |
|        TypeMapperReadOracleTimeStampTZ |        .NET Core 3.1 |  63.677 ns |  1.06 |         - |
|      DirectAccessReadOracleTimeStampTZ |        .NET Core 3.1 |  61.556 ns |  1.03 |         - |
|       TypeMapperReadOracleTimeStampLTZ |        .NET Core 3.1 |  67.348 ns |  1.12 |         - |
|     DirectAccessReadOracleTimeStampLTZ |        .NET Core 3.1 |  63.191 ns |  1.06 |         - |
|         TypeMapperReadOracleDecimalAdv |        .NET Core 3.1 |  11.780 ns |  0.20 |         - |
|       DirectAccessReadOracleDecimalAdv |        .NET Core 3.1 |   6.969 ns |  0.12 |         - |
|   TypeMapperReadOracleDecimalAsDecimal |        .NET Core 3.1 |   7.072 ns |  0.12 |         - |
| DirectAccessReadOracleDecimalAsDecimal |        .NET Core 3.1 |   2.956 ns |  0.05 |         - |
|       TypeMapperReadOracleDecimalAsInt |        .NET Core 3.1 |   9.335 ns |  0.16 |         - |
|     DirectAccessReadOracleDecimalAsInt |        .NET Core 3.1 |   6.725 ns |  0.11 |         - |
|      TypeMapperReadOracleDecimalAsLong |        .NET Core 3.1 |   8.884 ns |  0.15 |         - |
|    DirectAccessReadOracleDecimalAsLong |        .NET Core 3.1 |   6.487 ns |  0.11 |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Framework 4.7.2 | 123.218 ns |  2.06 |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Framework 4.7.2 |  59.855 ns |  1.00 |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Framework 4.7.2 | 133.455 ns |  2.23 |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Framework 4.7.2 |  58.445 ns |  0.98 |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Framework 4.7.2 |  40.256 ns |  0.67 |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Framework 4.7.2 |   6.837 ns |  0.11 |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |  26.124 ns |  0.44 |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   5.709 ns |  0.10 |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Framework 4.7.2 |  28.591 ns |  0.48 |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Framework 4.7.2 |   8.889 ns |  0.15 |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Framework 4.7.2 |  29.450 ns |  0.50 |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Framework 4.7.2 |   9.223 ns |  0.15 |         - |
