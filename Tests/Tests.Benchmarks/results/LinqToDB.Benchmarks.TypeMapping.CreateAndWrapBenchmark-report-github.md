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
| TypeMapperParameterless                | .NET 8.0             |  36.2314 ns |      96 B |
| DirectAccessParameterless              | .NET 8.0             |   7.3611 ns |      64 B |
| TypeMapperOneParameterString           | .NET 8.0             |  28.1847 ns |      96 B |
| DirectAccessOneParameterString         | .NET 8.0             |   9.8750 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 8.0             |   7.3926 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 8.0             |   1.4013 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 8.0             |  35.4434 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 8.0             |   9.2395 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 8.0             |  36.9901 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 8.0             |   7.2842 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 8.0             |  46.1990 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 8.0             |   6.7697 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 8.0             |  29.9667 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 8.0             |   9.1003 ns |      64 B |
| TypeMapperThreeParameters              | .NET 8.0             |  46.5021 ns |      96 B |
| DirectAccessThreeParameters            | .NET 8.0             |   9.8612 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 8.0             | 141.6122 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 8.0             | 137.6533 ns |      64 B |
| TypeMapperParameterless                | .NET 9.0             |  31.9622 ns |      96 B |
| DirectAccessParameterless              | .NET 9.0             |   8.5199 ns |      64 B |
| TypeMapperOneParameterString           | .NET 9.0             |  33.4908 ns |      96 B |
| DirectAccessOneParameterString         | .NET 9.0             |   9.0839 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 9.0             |  10.0750 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 9.0             |   9.2874 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 9.0             |  38.5432 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 9.0             |   8.9544 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 9.0             |  13.8716 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 9.0             |   7.4582 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 9.0             |  44.9621 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 9.0             |   8.9746 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 9.0             |  17.0142 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 9.0             |  10.0389 ns |      64 B |
| TypeMapperThreeParameters              | .NET 9.0             |  33.4990 ns |      96 B |
| DirectAccessThreeParameters            | .NET 9.0             |   7.9728 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 9.0             | 119.1591 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 9.0             | 134.0324 ns |      64 B |
| TypeMapperParameterless                | .NET Framework 4.6.2 |  69.7166 ns |      96 B |
| DirectAccessParameterless              | .NET Framework 4.6.2 |   6.8222 ns |      64 B |
| TypeMapperOneParameterString           | .NET Framework 4.6.2 |  32.5479 ns |      96 B |
| DirectAccessOneParameterString         | .NET Framework 4.6.2 |   0.2248 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET Framework 4.6.2 |  17.9303 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.6.2 |   6.5210 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET Framework 4.6.2 |  57.5971 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET Framework 4.6.2 |   0.3559 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET Framework 4.6.2 |  66.1242 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET Framework 4.6.2 |   7.0484 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET Framework 4.6.2 |  96.8897 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET Framework 4.6.2 |   4.5108 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET Framework 4.6.2 |  56.1187 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.6.2 |   7.2451 ns |      64 B |
| TypeMapperThreeParameters              | .NET Framework 4.6.2 |  40.3466 ns |      96 B |
| DirectAccessThreeParameters            | .NET Framework 4.6.2 |   4.7438 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET Framework 4.6.2 | 143.5569 ns |      64 B |
| DirectAccessTSTZFactory                | .NET Framework 4.6.2 | 310.0719 ns |      64 B |
