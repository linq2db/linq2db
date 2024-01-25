```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                 | Runtime              | Mean       | Allocated |
|--------------------------------------- |--------------------- |-----------:|----------:|
| TypeMapperReadOracleTimeStampTZ        | .NET 6.0             |  44.276 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 6.0             |  43.271 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 6.0             |  45.008 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 6.0             |  45.254 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 6.0             |  12.817 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 6.0             |   9.358 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 6.0             |   6.999 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 6.0             |   4.210 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 6.0             |  11.167 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 6.0             |   8.450 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 6.0             |  11.191 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 6.0             |   8.139 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET 7.0             |  46.701 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 7.0             |  49.139 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 7.0             |  50.560 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 7.0             |  44.430 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 7.0             |  13.113 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 7.0             |   4.214 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 7.0             |   6.150 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 7.0             |   1.474 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 7.0             |   9.072 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 7.0             |   7.952 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 7.0             |   9.824 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 7.0             |   7.917 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET Core 3.1        |  70.786 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET Core 3.1        |  64.573 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET Core 3.1        |  72.816 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET Core 3.1        |  66.676 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET Core 3.1        |  13.521 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET Core 3.1        |  11.537 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET Core 3.1        |   7.433 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 3.1        |   4.166 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET Core 3.1        |   9.833 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET Core 3.1        |   7.972 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET Core 3.1        |   3.611 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET Core 3.1        |   7.334 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET Framework 4.7.2 | 138.274 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET Framework 4.7.2 |  67.718 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET Framework 4.7.2 | 145.694 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET Framework 4.7.2 |  28.779 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET Framework 4.7.2 |  47.477 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET Framework 4.7.2 |   8.723 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET Framework 4.7.2 |  15.515 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   6.594 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET Framework 4.7.2 |  37.206 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET Framework 4.7.2 |  12.869 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET Framework 4.7.2 |  36.980 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET Framework 4.7.2 |  11.638 ns |         - |
