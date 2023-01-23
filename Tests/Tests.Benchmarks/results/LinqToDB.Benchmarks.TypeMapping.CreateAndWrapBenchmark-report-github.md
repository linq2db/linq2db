``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean | Allocated |
|--------------------------------------- |--------------------- |-----------:|----------:|
|                TypeMapperParameterless |             .NET 6.0 |  50.176 ns |      96 B |
|              DirectAccessParameterless |             .NET 6.0 |   7.991 ns |      64 B |
|           TypeMapperOneParameterString |             .NET 6.0 |  50.870 ns |      96 B |
|         DirectAccessOneParameterString |             .NET 6.0 |   7.810 ns |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 6.0 |   8.898 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 6.0 |   7.922 ns |      64 B |
|       TypeMapperTwoParametersIntString |             .NET 6.0 |  40.416 ns |      96 B |
|     DirectAccessTwoParametersIntString |             .NET 6.0 |   7.509 ns |      64 B |
|    TypeMapperTwoParametersStringString |             .NET 6.0 |  51.636 ns |      96 B |
|  DirectAccessTwoParametersStringString |             .NET 6.0 |   7.769 ns |      64 B |
|     TypeMapperTwoParametersWrapperEnum |             .NET 6.0 |  60.180 ns |      96 B |
|   DirectAccessTwoParametersWrapperEnum |             .NET 6.0 |   7.774 ns |      64 B |
|   TypeMapperTwoParametersWrapperString |             .NET 6.0 |  46.523 ns |      96 B |
| DirectAccessTwoParametersWrapperString |             .NET 6.0 |   7.936 ns |      64 B |
|              TypeMapperThreeParameters |             .NET 6.0 |  57.084 ns |      96 B |
|            DirectAccessThreeParameters |             .NET 6.0 |   8.371 ns |      64 B |
|                  TypeMapperTSTZFactory |             .NET 6.0 | 162.748 ns |      64 B |
|                DirectAccessTSTZFactory |             .NET 6.0 | 158.032 ns |      64 B |
|                TypeMapperParameterless |             .NET 7.0 |  45.397 ns |      96 B |
|              DirectAccessParameterless |             .NET 7.0 |   9.720 ns |      64 B |
|           TypeMapperOneParameterString |             .NET 7.0 |  50.468 ns |      96 B |
|         DirectAccessOneParameterString |             .NET 7.0 |  10.148 ns |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 7.0 |  11.366 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 7.0 |  10.370 ns |      64 B |
|       TypeMapperTwoParametersIntString |             .NET 7.0 |  48.590 ns |      96 B |
|     DirectAccessTwoParametersIntString |             .NET 7.0 |  10.763 ns |      64 B |
|    TypeMapperTwoParametersStringString |             .NET 7.0 |  45.467 ns |      96 B |
|  DirectAccessTwoParametersStringString |             .NET 7.0 |   8.745 ns |      64 B |
|     TypeMapperTwoParametersWrapperEnum |             .NET 7.0 |  50.796 ns |      96 B |
|   DirectAccessTwoParametersWrapperEnum |             .NET 7.0 |   9.273 ns |      64 B |
|   TypeMapperTwoParametersWrapperString |             .NET 7.0 |  46.830 ns |      96 B |
| DirectAccessTwoParametersWrapperString |             .NET 7.0 |   9.414 ns |      64 B |
|              TypeMapperThreeParameters |             .NET 7.0 |  59.826 ns |      96 B |
|            DirectAccessThreeParameters |             .NET 7.0 |  10.119 ns |      64 B |
|                  TypeMapperTSTZFactory |             .NET 7.0 | 148.434 ns |      64 B |
|                DirectAccessTSTZFactory |             .NET 7.0 | 147.450 ns |      64 B |
|                TypeMapperParameterless |        .NET Core 3.1 |  54.900 ns |      96 B |
|              DirectAccessParameterless |        .NET Core 3.1 |   7.867 ns |      64 B |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  56.655 ns |      96 B |
|         DirectAccessOneParameterString |        .NET Core 3.1 |   8.590 ns |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   8.005 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   8.705 ns |      64 B |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  57.422 ns |      96 B |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |   8.493 ns |      64 B |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  55.887 ns |      96 B |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |   8.772 ns |      64 B |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  61.063 ns |      96 B |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |   8.492 ns |      64 B |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  57.506 ns |      96 B |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |   7.950 ns |      64 B |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  59.910 ns |      96 B |
|            DirectAccessThreeParameters |        .NET Core 3.1 |   7.073 ns |      64 B |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 283.396 ns |      64 B |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 254.574 ns |      64 B |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  74.747 ns |      96 B |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   8.061 ns |      64 B |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  74.280 ns |      96 B |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |   8.208 ns |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  19.958 ns |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |   8.214 ns |      64 B |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  66.718 ns |      96 B |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |   7.094 ns |      64 B |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  76.980 ns |      96 B |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |   8.000 ns |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 |  99.054 ns |      96 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   7.917 ns |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  76.952 ns |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |   8.161 ns |      64 B |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 |  99.323 ns |      96 B |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   8.248 ns |      64 B |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 311.320 ns |      64 B |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 262.984 ns |      64 B |
