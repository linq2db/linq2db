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
| Method                                 | Runtime              | Mean        | Allocated |
|--------------------------------------- |--------------------- |------------:|----------:|
| TypeMapperReadOracleTimeStampTZ        | .NET 8.0             |  42.9291 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 8.0             |  36.9057 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 8.0             |  36.8703 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 8.0             |  41.3802 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 8.0             |   7.0432 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 8.0             |   1.8270 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 8.0             |   3.6178 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 8.0             |   2.4005 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 8.0             |   1.2647 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 8.0             |   2.3948 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 8.0             |   5.1176 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 8.0             |   3.9763 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET 9.0             |  18.2672 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 9.0             |  25.5293 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 9.0             |  36.3168 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 9.0             |  31.5782 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 9.0             |   7.5477 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 9.0             |   0.5996 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 9.0             |   3.7856 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 9.0             |   0.9867 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 9.0             |   3.9309 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 9.0             |   4.6580 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 9.0             |   5.5702 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 9.0             |   1.9090 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET Framework 4.6.2 | 130.0656 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET Framework 4.6.2 |  43.1204 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET Framework 4.6.2 | 131.6394 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET Framework 4.6.2 |  72.0903 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET Framework 4.6.2 |  45.0041 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET Framework 4.6.2 |   6.6924 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET Framework 4.6.2 |  32.9003 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.6.2 |   7.5464 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET Framework 4.6.2 |  36.0396 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET Framework 4.6.2 |   8.8257 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET Framework 4.6.2 |  36.1759 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET Framework 4.6.2 |  11.5534 ns |         - |
