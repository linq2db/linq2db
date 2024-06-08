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
| TypeMapperParameterless                | .NET 6.0             |  49.555 ns |      96 B |
| DirectAccessParameterless              | .NET 6.0             |   6.112 ns |      64 B |
| TypeMapperOneParameterString           | .NET 6.0             |  49.066 ns |      96 B |
| DirectAccessOneParameterString         | .NET 6.0             |   7.878 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 6.0             |   9.387 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 6.0             |   8.464 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 6.0             |  33.634 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 6.0             |   7.681 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 6.0             |  40.223 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 6.0             |   7.684 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 6.0             |  55.890 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 6.0             |   8.117 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 6.0             |  49.382 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 6.0             |   4.684 ns |      64 B |
| TypeMapperThreeParameters              | .NET 6.0             |  58.008 ns |      96 B |
| DirectAccessThreeParameters            | .NET 6.0             |   7.702 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 6.0             | 149.963 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 6.0             | 165.231 ns |      64 B |
| TypeMapperParameterless                | .NET 7.0             |  45.289 ns |      96 B |
| DirectAccessParameterless              | .NET 7.0             |   9.279 ns |      64 B |
| TypeMapperOneParameterString           | .NET 7.0             |  47.195 ns |      96 B |
| DirectAccessOneParameterString         | .NET 7.0             |   9.718 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET 7.0             |   8.119 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET 7.0             |   8.297 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET 7.0             |  47.482 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET 7.0             |   7.540 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET 7.0             |  46.037 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET 7.0             |   9.694 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET 7.0             |  55.704 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET 7.0             |  10.627 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET 7.0             |  30.333 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET 7.0             |  10.151 ns |      64 B |
| TypeMapperThreeParameters              | .NET 7.0             |  57.708 ns |      96 B |
| DirectAccessThreeParameters            | .NET 7.0             |   9.863 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET 7.0             | 150.100 ns |      64 B |
| DirectAccessTSTZFactory                | .NET 7.0             | 146.088 ns |      64 B |
| TypeMapperParameterless                | .NET Core 3.1        |  52.828 ns |      96 B |
| DirectAccessParameterless              | .NET Core 3.1        |   8.256 ns |      64 B |
| TypeMapperOneParameterString           | .NET Core 3.1        |  54.260 ns |      96 B |
| DirectAccessOneParameterString         | .NET Core 3.1        |   6.983 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET Core 3.1        |   7.768 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 3.1        |   7.984 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET Core 3.1        |  54.369 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET Core 3.1        |   6.285 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET Core 3.1        |  52.342 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET Core 3.1        |   8.089 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET Core 3.1        |  65.930 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET Core 3.1        |   7.430 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET Core 3.1        |  55.223 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Core 3.1        |   8.433 ns |      64 B |
| TypeMapperThreeParameters              | .NET Core 3.1        |  64.076 ns |      96 B |
| DirectAccessThreeParameters            | .NET Core 3.1        |   8.507 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET Core 3.1        | 287.582 ns |      64 B |
| DirectAccessTSTZFactory                | .NET Core 3.1        | 274.139 ns |      64 B |
| TypeMapperParameterless                | .NET Framework 4.7.2 |  72.085 ns |      96 B |
| DirectAccessParameterless              | .NET Framework 4.7.2 |   3.926 ns |      64 B |
| TypeMapperOneParameterString           | .NET Framework 4.7.2 |  73.720 ns |      96 B |
| DirectAccessOneParameterString         | .NET Framework 4.7.2 |   7.079 ns |      64 B |
| TypeMapperOneParameterTimeSpanUnwrap   | .NET Framework 4.7.2 |  17.918 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |   3.040 ns |      64 B |
| TypeMapperTwoParametersIntString       | .NET Framework 4.7.2 |  66.627 ns |      96 B |
| DirectAccessTwoParametersIntString     | .NET Framework 4.7.2 |   7.001 ns |      64 B |
| TypeMapperTwoParametersStringString    | .NET Framework 4.7.2 |  72.975 ns |      96 B |
| DirectAccessTwoParametersStringString  | .NET Framework 4.7.2 |   7.160 ns |      64 B |
| TypeMapperTwoParametersWrapperEnum     | .NET Framework 4.7.2 |  93.808 ns |      96 B |
| DirectAccessTwoParametersWrapperEnum   | .NET Framework 4.7.2 |   7.122 ns |      64 B |
| TypeMapperTwoParametersWrapperString   | .NET Framework 4.7.2 |  53.336 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |   6.967 ns |      64 B |
| TypeMapperThreeParameters              | .NET Framework 4.7.2 |  96.082 ns |      96 B |
| DirectAccessThreeParameters            | .NET Framework 4.7.2 |   6.036 ns |      64 B |
| TypeMapperTSTZFactory                  | .NET Framework 4.7.2 | 334.448 ns |      64 B |
| DirectAccessTSTZFactory                | .NET Framework 4.7.2 | 328.530 ns |      64 B |
