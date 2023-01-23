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
|                                Method |              Runtime |        Mean | Allocated |
|-------------------------------------- |--------------------- |------------:|----------:|
|             TestCastConvertTypeMapper |             .NET 6.0 |   5.3804 ns |         - |
|           TestCastConvertDirectAccess |             .NET 6.0 |   0.6465 ns |         - |
|       TestDictionaryConvertTypeMapper |             .NET 6.0 |  23.0703 ns |         - |
|     TestDictionaryConvertDirectAccess |             .NET 6.0 |   1.0095 ns |         - |
|   TestDictionaryCastConvertTypeMapper |             .NET 6.0 |  52.3257 ns |      48 B |
| TestDictionaryCastConvertDirectAccess |             .NET 6.0 |   0.8610 ns |         - |
|        TestFlagsCastConvertTypeMapper |             .NET 6.0 |   4.5873 ns |         - |
|      TestFlagsCastConvertDirectAccess |             .NET 6.0 |   1.0061 ns |         - |
|             TestCastConvertTypeMapper |             .NET 7.0 |   4.2027 ns |         - |
|           TestCastConvertDirectAccess |             .NET 7.0 |   1.0273 ns |         - |
|       TestDictionaryConvertTypeMapper |             .NET 7.0 |  21.7908 ns |         - |
|     TestDictionaryConvertDirectAccess |             .NET 7.0 |   0.9383 ns |         - |
|   TestDictionaryCastConvertTypeMapper |             .NET 7.0 |  21.6558 ns |         - |
| TestDictionaryCastConvertDirectAccess |             .NET 7.0 |   1.0449 ns |         - |
|        TestFlagsCastConvertTypeMapper |             .NET 7.0 |   4.6651 ns |         - |
|      TestFlagsCastConvertDirectAccess |             .NET 7.0 |   1.1652 ns |         - |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |   6.2864 ns |         - |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |   1.0801 ns |         - |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 |  26.0167 ns |         - |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |   1.0272 ns |         - |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 |  66.7685 ns |      48 B |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |   1.0432 ns |         - |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |   6.5572 ns |         - |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |   0.8362 ns |         - |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 |  24.8017 ns |         - |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |   0.8729 ns |         - |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 |  66.0098 ns |         - |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |   0.9170 ns |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 113.3068 ns |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |   0.8221 ns |         - |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 |  24.9643 ns |         - |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9461 ns |         - |
