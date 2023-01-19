``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|             TestCastConvertTypeMapper |             .NET 6.0 |   5.2208 ns |   5.2150 ns |   5.48 |      - |         - |          NA |
|           TestCastConvertDirectAccess |             .NET 6.0 |   0.4118 ns |   0.4107 ns |   0.43 |      - |         - |          NA |
|       TestDictionaryConvertTypeMapper |             .NET 6.0 |  25.7888 ns |  25.7302 ns |  26.96 |      - |         - |          NA |
|     TestDictionaryConvertDirectAccess |             .NET 6.0 |   0.8885 ns |   0.8886 ns |   0.93 |      - |         - |          NA |
|   TestDictionaryCastConvertTypeMapper |             .NET 6.0 |  47.6330 ns |  47.3749 ns |  49.97 | 0.0029 |      48 B |          NA |
| TestDictionaryCastConvertDirectAccess |             .NET 6.0 |   0.9327 ns |   0.9133 ns |   0.98 |      - |         - |          NA |
|        TestFlagsCastConvertTypeMapper |             .NET 6.0 |   5.0868 ns |   5.0591 ns |   5.35 |      - |         - |          NA |
|      TestFlagsCastConvertDirectAccess |             .NET 6.0 |   0.6875 ns |   0.9120 ns |   0.30 |      - |         - |          NA |
|             TestCastConvertTypeMapper |             .NET 7.0 |   4.2954 ns |   4.2811 ns |   4.52 |      - |         - |          NA |
|           TestCastConvertDirectAccess |             .NET 7.0 |   0.6702 ns |   0.9085 ns |   0.71 |      - |         - |          NA |
|       TestDictionaryConvertTypeMapper |             .NET 7.0 |  21.1184 ns |  21.1487 ns |  22.31 |      - |         - |          NA |
|     TestDictionaryConvertDirectAccess |             .NET 7.0 |   0.7823 ns |   0.8326 ns |   0.72 |      - |         - |          NA |
|   TestDictionaryCastConvertTypeMapper |             .NET 7.0 |  24.2871 ns |  24.1294 ns |  25.55 |      - |         - |          NA |
| TestDictionaryCastConvertDirectAccess |             .NET 7.0 |   0.9354 ns |   0.9133 ns |   0.98 |      - |         - |          NA |
|        TestFlagsCastConvertTypeMapper |             .NET 7.0 |   5.6211 ns |   5.5890 ns |   5.90 |      - |         - |          NA |
|      TestFlagsCastConvertDirectAccess |             .NET 7.0 |   1.3267 ns |   1.3267 ns |   1.38 |      - |         - |          NA |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |   6.3923 ns |   6.3919 ns |   6.71 |      - |         - |          NA |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |   0.9414 ns |   0.9414 ns |   0.99 |      - |         - |          NA |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 |  28.2915 ns |  28.2905 ns |  29.46 |      - |         - |          NA |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |   0.9333 ns |   0.9147 ns |   0.98 |      - |         - |          NA |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 |  63.0105 ns |  62.7486 ns |  66.30 | 0.0029 |      48 B |          NA |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |   0.4286 ns |   0.4286 ns |   0.45 |      - |         - |          NA |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |   6.1949 ns |   6.1974 ns |   6.52 |      - |         - |          NA |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |   0.8628 ns |   0.8547 ns |   0.86 |      - |         - |          NA |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 |  22.6258 ns |  22.0857 ns |  23.56 |      - |         - |          NA |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9517 ns |   0.9623 ns |   1.00 |      - |         - |          NA |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 |  67.3449 ns |  66.7213 ns |  70.84 |      - |         - |          NA |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |   1.4545 ns |   2.2098 ns |   1.46 |      - |         - |          NA |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 109.8340 ns | 109.7119 ns | 115.83 | 0.0076 |      48 B |          NA |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9127 ns |   0.9126 ns |   0.95 |      - |         - |          NA |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 |  25.5067 ns |  25.5058 ns |  26.76 |      - |         - |          NA |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9132 ns |   0.9132 ns |   0.95 |      - |         - |          NA |
