```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.17763.7553/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X 3.39GHz, 2 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FTOCRB : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-DHTNJT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-QIENBV : .NET Framework 4.8 (4.8.4795.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                 | Runtime              | Mean       | Allocated |
|--------------------------------------- |--------------------- |-----------:|----------:|
| TypeMapperReadOracleTimeStampTZ        | .NET 8.0             |  45.879 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 8.0             |  19.144 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 8.0             |  42.891 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 8.0             |  41.578 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 8.0             |   8.321 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 8.0             |   2.566 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 8.0             |   4.935 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 8.0             |   1.065 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 8.0             |   6.080 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 8.0             |   4.605 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 8.0             |   2.524 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 8.0             |   4.679 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET 9.0             |  42.398 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 9.0             |  38.130 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 9.0             |  18.867 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 9.0             |  38.831 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 9.0             |   8.015 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 9.0             |   3.709 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 9.0             |   4.048 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 9.0             |   2.363 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 9.0             |   5.084 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 9.0             |   4.603 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 9.0             |   4.174 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 9.0             |   4.680 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET Framework 4.6.2 | 135.910 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET Framework 4.6.2 |  54.982 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET Framework 4.6.2 | 143.770 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET Framework 4.6.2 |  64.755 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET Framework 4.6.2 |  49.392 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET Framework 4.6.2 |   8.259 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET Framework 4.6.2 |  25.522 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.6.2 |   7.513 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET Framework 4.6.2 |  30.093 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET Framework 4.6.2 |   8.384 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET Framework 4.6.2 |  15.061 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET Framework 4.6.2 |  11.487 ns |         - |
