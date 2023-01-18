``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                Method |              Runtime |        Mean |      Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------------------- |--------------------- |------------:|------------:|------:|-------:|----------:|------------:|
|             TestCastConvertTypeMapper |             .NET 6.0 |   5.0235 ns |   5.0232 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |             .NET 6.0 |   0.9095 ns |   0.9093 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |             .NET 6.0 |  24.8633 ns |  24.8662 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |             .NET 6.0 |   0.9131 ns |   0.9132 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |             .NET 6.0 |  48.1877 ns |  48.1991 ns |     ? | 0.0029 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess |             .NET 6.0 |   2.2228 ns |   2.2227 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |             .NET 6.0 |   5.0222 ns |   5.0221 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |             .NET 6.0 |   0.9350 ns |   0.9134 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper |             .NET 7.0 |   4.2750 ns |   4.2750 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |             .NET 7.0 |   0.9192 ns |   0.9139 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |             .NET 7.0 |  21.8858 ns |  24.6946 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |             .NET 7.0 |   0.9132 ns |   0.9131 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |             .NET 7.0 |  17.0259 ns |  11.2853 ns |     ? |      - |         - |           ? |
| TestDictionaryCastConvertDirectAccess |             .NET 7.0 |   0.7853 ns |   0.8146 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |             .NET 7.0 |   4.3018 ns |   4.3025 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |             .NET 7.0 |   0.8895 ns |   0.9133 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |   5.9651 ns |   5.9652 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |   0.8453 ns |   0.8848 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 |  26.2381 ns |  28.2295 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |   0.8791 ns |   0.8852 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 |  63.7988 ns |  63.9572 ns |     ? | 0.0029 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |   0.0000 ns |   0.0000 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |   6.1466 ns |   6.2036 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |   0.7810 ns |   0.7517 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 |  21.9328 ns |  21.8587 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |   0.5714 ns |   0.5802 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 |  68.2353 ns |  68.6471 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |   0.9114 ns |   0.9277 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 109.1683 ns | 109.4056 ns |     ? | 0.0076 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |   0.9160 ns |   0.9142 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 |  24.2024 ns |  24.2019 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |   1.8243 ns |   2.2080 ns |     ? |      - |         - |           ? |
