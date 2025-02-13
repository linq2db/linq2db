```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                 | Runtime              | Mean        | Allocated |
|--------------------------------------- |--------------------- |------------:|----------:|
| TypeMapperReadOracleTimeStampTZ        | .NET 6.0             |  46.2017 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 6.0             |  39.1457 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 6.0             |  46.9485 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 6.0             |  43.0829 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 6.0             |   9.8512 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 6.0             |   9.3381 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 6.0             |   7.0421 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 6.0             |   2.8896 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 6.0             |  10.7096 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 6.0             |   8.2863 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 6.0             |  11.5366 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 6.0             |   8.3974 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET 8.0             |  46.0266 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 8.0             |  18.4721 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 8.0             |  39.1883 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 8.0             |  38.1077 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 8.0             |   7.4358 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 8.0             |   1.9265 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 8.0             |   0.3749 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 8.0             |   2.2990 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 8.0             |   4.9225 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 8.0             |   5.0177 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 8.0             |   4.2917 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 8.0             |   5.0162 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET 9.0             |  41.8973 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET 9.0             |  41.5696 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET 9.0             |  41.7872 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET 9.0             |  44.0410 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET 9.0             |   7.3546 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET 9.0             |   2.3462 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET 9.0             |   3.6072 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET 9.0             |   2.3982 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET 9.0             |   5.4805 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET 9.0             |   5.0456 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET 9.0             |   5.6289 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET 9.0             |   4.6041 ns |         - |
| TypeMapperReadOracleTimeStampTZ        | .NET Framework 4.6.2 | 135.8685 ns |         - |
| DirectAccessReadOracleTimeStampTZ      | .NET Framework 4.6.2 |  66.8562 ns |         - |
| TypeMapperReadOracleTimeStampLTZ       | .NET Framework 4.6.2 | 132.6608 ns |         - |
| DirectAccessReadOracleTimeStampLTZ     | .NET Framework 4.6.2 |  65.7286 ns |         - |
| TypeMapperReadOracleDecimalAdv         | .NET Framework 4.6.2 |  51.3901 ns |         - |
| DirectAccessReadOracleDecimalAdv       | .NET Framework 4.6.2 |   8.4396 ns |         - |
| TypeMapperReadOracleDecimalAsDecimal   | .NET Framework 4.6.2 |  31.1977 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.6.2 |   7.0607 ns |         - |
| TypeMapperReadOracleDecimalAsInt       | .NET Framework 4.6.2 |  36.4158 ns |         - |
| DirectAccessReadOracleDecimalAsInt     | .NET Framework 4.6.2 |  27.8336 ns |         - |
| TypeMapperReadOracleDecimalAsLong      | .NET Framework 4.6.2 |  37.3827 ns |         - |
| DirectAccessReadOracleDecimalAsLong    | .NET Framework 4.6.2 |  10.6491 ns |         - |
