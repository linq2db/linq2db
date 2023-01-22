``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|             TestCastConvertTypeMapper |             .NET 6.0 |   5.2772 ns |   5.3342 ns |   5.86 |      - |         - |          NA |
|           TestCastConvertDirectAccess |             .NET 6.0 |   0.8493 ns |   0.8395 ns |   0.93 |      - |         - |          NA |
|       TestDictionaryConvertTypeMapper |             .NET 6.0 |  24.1901 ns |  24.2433 ns |  26.72 |      - |         - |          NA |
|     TestDictionaryConvertDirectAccess |             .NET 6.0 |   1.2854 ns |   1.2849 ns |   1.38 |      - |         - |          NA |
|   TestDictionaryCastConvertTypeMapper |             .NET 6.0 |  51.6026 ns |  51.8115 ns |  56.72 | 0.0029 |      48 B |          NA |
| TestDictionaryCastConvertDirectAccess |             .NET 6.0 |   0.9091 ns |   0.9196 ns |   1.01 |      - |         - |          NA |
|        TestFlagsCastConvertTypeMapper |             .NET 6.0 |   5.2096 ns |   5.1802 ns |   5.80 |      - |         - |          NA |
|      TestFlagsCastConvertDirectAccess |             .NET 6.0 |   1.2949 ns |   1.2749 ns |   1.44 |      - |         - |          NA |
|             TestCastConvertTypeMapper |             .NET 7.0 |   4.3219 ns |   4.3589 ns |   4.70 |      - |         - |          NA |
|           TestCastConvertDirectAccess |             .NET 7.0 |   0.8558 ns |   0.9291 ns |   0.58 |      - |         - |          NA |
|       TestDictionaryConvertTypeMapper |             .NET 7.0 |  23.8788 ns |  23.9395 ns |  26.51 |      - |         - |          NA |
|     TestDictionaryConvertDirectAccess |             .NET 7.0 |   0.8848 ns |   0.9197 ns |   0.95 |      - |         - |          NA |
|   TestDictionaryCastConvertTypeMapper |             .NET 7.0 |  24.7270 ns |  24.8360 ns |  27.45 |      - |         - |          NA |
| TestDictionaryCastConvertDirectAccess |             .NET 7.0 |   0.9025 ns |   0.9216 ns |   0.98 |      - |         - |          NA |
|        TestFlagsCastConvertTypeMapper |             .NET 7.0 |   3.7246 ns |   3.7351 ns |   3.61 |      - |         - |          NA |
|      TestFlagsCastConvertDirectAccess |             .NET 7.0 |   0.8010 ns |   0.9145 ns |   0.77 |      - |         - |          NA |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |   6.1453 ns |   6.1614 ns |   6.82 |      - |         - |          NA |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |   1.1088 ns |   1.0693 ns |   1.23 |      - |         - |          NA |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 |  29.3642 ns |  29.4658 ns |  32.58 |      - |         - |          NA |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |   1.0302 ns |   1.1033 ns |   1.06 |      - |         - |          NA |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 |  66.3842 ns |  67.0015 ns |  73.70 | 0.0029 |      48 B |          NA |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |   0.9350 ns |   0.9248 ns |   1.04 |      - |         - |          NA |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |   6.5340 ns |   6.6532 ns |   7.23 |      - |         - |          NA |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |   1.7463 ns |   2.3223 ns |   2.50 |      - |         - |          NA |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 |  22.2242 ns |  22.0135 ns |  24.36 |      - |         - |          NA |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9038 ns |   0.9056 ns |   1.00 |      - |         - |          NA |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 |  66.6328 ns |  66.8310 ns |  72.95 |      - |         - |          NA |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |   0.9439 ns |   0.9753 ns |   1.05 |      - |         - |          NA |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 110.8924 ns | 110.9319 ns | 122.49 | 0.0076 |      48 B |          NA |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9087 ns |   0.9093 ns |   1.00 |      - |         - |          NA |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 |  25.0047 ns |  26.1448 ns |  27.41 |      - |         - |          NA |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9903 ns |   0.9849 ns |   1.09 |      - |         - |          NA |
