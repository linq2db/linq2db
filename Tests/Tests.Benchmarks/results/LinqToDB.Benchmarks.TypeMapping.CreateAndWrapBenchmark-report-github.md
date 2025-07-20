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
| TypeMapperParameterless                | .NET 8.0             |  28.3116 ns |      96 B |
| DirectAccessParameterless              | .NET 8.0             |   8.9166 ns |      64 B |
| TypeMapperOneParameterString           | .NET 8.0             |  23.5376 ns |      96 B |
| DirectAccessOneParameterString         | .NET 8.0             |   7.8771 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 8.0             |   9.1511 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 8.0             |   3.2716 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 8.0             |  36.9641 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 8.0             |   4.6302 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 8.0             |  36.1446 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 8.0             |   3.4831 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 8.0             |  18.4818 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 8.0             |   6.6748 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 8.0             |  37.0308 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 8.0             |   2.1327 ns |      64 B |
| TypeMapperThreeParameters              | .NET 8.0             |  45.3720 ns |      96 B |
| DirectAccessThreeParameters            | .NET 8.0             |   5.8597 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 8.0             | 135.6809 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 8.0             | 137.6881 ns |      64 B |
| TypeMapperParameterless                | .NET 9.0             |  37.0897 ns |      96 B |
| DirectAccessParameterless              | .NET 9.0             |   6.7328 ns |      64 B |
| TypeMapperOneParameterString           | .NET 9.0             |  16.3809 ns |      96 B |
| DirectAccessOneParameterString         | .NET 9.0             |   9.3694 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 9.0             |   6.4723 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 9.0             |   8.7135 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 9.0             |  35.1057 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 9.0             |   8.3368 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 9.0             |  35.5469 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 9.0             |   1.5670 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 9.0             |  35.8145 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 9.0             |   8.8174 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 9.0             |  37.6676 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 9.0             |   8.4706 ns |      64 B |
| TypeMapperThreeParameters              | .NET 9.0             |  44.8938 ns |      96 B |
| DirectAccessThreeParameters            | .NET 9.0             |   7.9573 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 9.0             |  94.8905 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 9.0             | 110.2177 ns |      64 B |
| TypeMapperParameterless                | .NET Framework 4.6.2 |  62.3943 ns |      96 B |
| DirectAccessParameterless              | .NET Framework 4.6.2 |   7.9561 ns |      64 B |
| TypeMapperOneParameterString           | .NET Framework 4.6.2 |  71.9470 ns |      96 B |
| DirectAccessOneParameterString         | .NET Framework 4.6.2 |   6.9754 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET Framework 4.6.2 |  17.3474 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.6.2 |   6.2165 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET Framework 4.6.2 |  72.1714 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET Framework 4.6.2 |   7.0550 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET Framework 4.6.2 |  71.8992 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET Framework 4.6.2 |   4.6337 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET Framework 4.6.2 |  94.1921 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET Framework 4.6.2 |   7.8021 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET Framework 4.6.2 |  72.1238 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.6.2 |   6.2221 ns |      64 B |
| TypeMapperThreeParameters              | .NET Framework 4.6.2 |  92.9518 ns |      96 B |
| DirectAccessThreeParameters            | .NET Framework 4.6.2 |   0.3029 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET Framework 4.6.2 | 142.6844 ns |      64 B |
| DirectAccessTSTZFactory                | .NET Framework 4.6.2 | 316.3240 ns |      64 B |
