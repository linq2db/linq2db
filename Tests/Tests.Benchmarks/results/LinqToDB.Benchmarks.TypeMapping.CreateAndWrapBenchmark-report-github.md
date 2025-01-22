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
| Method                                 | Runtime              | Mean       | Allocated |
|--------------------------------------- |--------------------- |-----------:|----------:|
| TypeMapperParameterless                | .NET 6.0             |  45.063 ns |      96 B |
| DirectAccessParameterless              | .NET 6.0             |   6.642 ns |      64 B |
| TypeMapperOneParameterString           | .NET 6.0             |  49.184 ns |      96 B |
| DirectAccessOneParameterString         | .NET 6.0             |   7.886 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 6.0             |   8.819 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 6.0             |   7.948 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 6.0             |  41.659 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 6.0             |   5.315 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 6.0             |  50.677 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 6.0             |   8.158 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 6.0             |  56.874 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 6.0             |   8.140 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 6.0             |  49.711 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 6.0             |   7.740 ns |      64 B |
| TypeMapperThreeParameters              | .NET 6.0             |  57.646 ns |      96 B |
| DirectAccessThreeParameters            | .NET 6.0             |   7.348 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 6.0             | 159.916 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 6.0             | 157.644 ns |      64 B |
| TypeMapperParameterless                | .NET 8.0             |  33.834 ns |      96 B |
| DirectAccessParameterless              | .NET 8.0             |   9.077 ns |      64 B |
| TypeMapperOneParameterString           | .NET 8.0             |  37.967 ns |      96 B |
| DirectAccessOneParameterString         | .NET 8.0             |   8.886 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 8.0             |  14.313 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 8.0             |   9.545 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 8.0             |  37.381 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 8.0             |   9.450 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 8.0             |  37.661 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 8.0             |   9.425 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 8.0             |  44.752 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 8.0             |  10.423 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 8.0             |  17.826 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 8.0             |   8.628 ns |      64 B |
| TypeMapperThreeParameters              | .NET 8.0             |  45.936 ns |      96 B |
| DirectAccessThreeParameters            | .NET 8.0             |  17.139 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 8.0             | 139.080 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 8.0             | 137.701 ns |      64 B |
| TypeMapperParameterless                | .NET 9.0             |  36.510 ns |      96 B |
| DirectAccessParameterless              | .NET 9.0             |   7.996 ns |      64 B |
| TypeMapperOneParameterString           | .NET 9.0             |  36.214 ns |      96 B |
| DirectAccessOneParameterString         | .NET 9.0             |  10.276 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 9.0             |  21.253 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 9.0             |  10.725 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 9.0             |  36.094 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 9.0             |   9.029 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 9.0             |  36.254 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 9.0             |   7.670 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 9.0             |  45.180 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 9.0             |   9.559 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 9.0             |  37.527 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 9.0             |   9.512 ns |      64 B |
| TypeMapperThreeParameters              | .NET 9.0             |  45.014 ns |      96 B |
| DirectAccessThreeParameters            | .NET 9.0             |   8.704 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 9.0             | 139.207 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 9.0             | 133.522 ns |      64 B |
| TypeMapperParameterless                | .NET Framework 4.6.2 |  74.074 ns |      96 B |
| DirectAccessParameterless              | .NET Framework 4.6.2 |   6.673 ns |      64 B |
| TypeMapperOneParameterString           | .NET Framework 4.6.2 |  72.892 ns |      96 B |
| DirectAccessOneParameterString         | .NET Framework 4.6.2 |   7.628 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET Framework 4.6.2 |  18.404 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.6.2 |   7.326 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET Framework 4.6.2 |  73.241 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET Framework 4.6.2 |   7.426 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET Framework 4.6.2 |  66.284 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET Framework 4.6.2 |   7.404 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET Framework 4.6.2 |  99.327 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET Framework 4.6.2 |   7.777 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET Framework 4.6.2 |  72.472 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.6.2 |   7.827 ns |      64 B |
| TypeMapperThreeParameters              | .NET Framework 4.6.2 |  96.817 ns |      96 B |
| DirectAccessThreeParameters            | .NET Framework 4.6.2 |   7.140 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET Framework 4.6.2 | 308.498 ns |      64 B |
| DirectAccessTSTZFactory                | .NET Framework 4.6.2 | 302.070 ns |      64 B |
