``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                Method |              Runtime |        Mean |      Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------------------- |--------------------- |------------:|------------:|------:|-------:|----------:|------------:|
|             TestCastConvertTypeMapper |             .NET 6.0 |   5.1113 ns |   5.1378 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |             .NET 6.0 |   0.9598 ns |   0.9715 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |             .NET 6.0 |  26.7833 ns |  26.5802 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |             .NET 6.0 |   1.7130 ns |   1.7129 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |             .NET 6.0 |  49.3447 ns |  49.4046 ns |     ? | 0.0029 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess |             .NET 6.0 |   1.2570 ns |   1.2574 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |             .NET 6.0 |   5.1576 ns |   5.1296 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |             .NET 6.0 |   0.9343 ns |   0.9568 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper |             .NET 7.0 |   4.2790 ns |   4.2787 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |             .NET 7.0 |   2.1894 ns |   2.2084 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |             .NET 7.0 |  21.5521 ns |  24.4765 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |             .NET 7.0 |   0.9331 ns |   0.9148 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |             .NET 7.0 |  24.2166 ns |  24.2107 ns |     ? |      - |         - |           ? |
| TestDictionaryCastConvertDirectAccess |             .NET 7.0 |   0.9675 ns |   0.9802 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |             .NET 7.0 |   3.6194 ns |   4.2484 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |             .NET 7.0 |   0.9828 ns |   0.9902 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |   5.9199 ns |   5.9194 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |   0.9153 ns |   0.9150 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 |  29.6487 ns |  29.2132 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |   0.9472 ns |   0.9176 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 |  58.7552 ns |  59.0150 ns |     ? | 0.0029 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |   0.8435 ns |   0.8031 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |   5.9187 ns |   5.9187 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |   0.9148 ns |   0.9146 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 |  24.0365 ns |  24.2522 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |   0.7596 ns |   0.9155 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 |  68.0326 ns |  67.1447 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |   0.9025 ns |   0.9162 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 109.1376 ns | 109.1772 ns |     ? | 0.0076 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9106 ns |   0.9226 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 |  19.3864 ns |  24.2463 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9090 ns |   0.9299 ns |     ? |      - |         - |           ? |
