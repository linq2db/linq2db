``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                Method |              Runtime |        Mean |      Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------------------- |--------------------- |------------:|------------:|------:|-------:|----------:|------------:|
|             TestCastConvertTypeMapper |             .NET 6.0 |   4.4964 ns |   4.8446 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |             .NET 6.0 |   0.8459 ns |   0.8076 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |             .NET 6.0 |  24.6128 ns |  26.1243 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |             .NET 6.0 |   1.2055 ns |   1.3291 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |             .NET 6.0 |  64.1652 ns |  65.1245 ns |     ? | 0.0029 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess |             .NET 6.0 |   0.6883 ns |   0.7159 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |             .NET 6.0 |   6.3231 ns |   7.0057 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |             .NET 6.0 |   1.0104 ns |   1.1781 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper |             .NET 7.0 |   4.1165 ns |   4.6027 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |             .NET 7.0 |   0.9558 ns |   1.0540 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |             .NET 7.0 |  25.6181 ns |  26.7147 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |             .NET 7.0 |   1.4493 ns |   1.6195 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |             .NET 7.0 |  27.1742 ns |  28.1823 ns |     ? |      - |         - |           ? |
| TestDictionaryCastConvertDirectAccess |             .NET 7.0 |   1.3174 ns |   1.4407 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |             .NET 7.0 |   5.0266 ns |   5.3001 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |             .NET 7.0 |   1.5134 ns |   1.8152 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |   6.0145 ns |   6.1568 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |   0.8725 ns |   0.9698 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 |  30.4646 ns |  31.9032 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |   0.8812 ns |   0.9280 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 |  73.3421 ns |  75.4355 ns |     ? | 0.0029 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |   0.8268 ns |   0.9386 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |   7.7778 ns |   8.1133 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |   1.1691 ns |   1.3106 ns |     ? |      - |         - |           ? |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 |  25.7186 ns |  28.0926 ns |     ? |      - |         - |           ? |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |   1.2583 ns |   1.3873 ns |     ? |      - |         - |           ? |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 |  73.3081 ns |  79.7724 ns |     ? |      - |         - |           ? |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |   0.9103 ns |   1.0056 ns |     ? |      - |         - |           ? |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 119.7465 ns | 130.0869 ns |     ? | 0.0076 |      48 B |           ? |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |   1.7326 ns |   1.8038 ns |     ? |      - |         - |           ? |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 |  26.3621 ns |  26.6219 ns |     ? |      - |         - |           ? |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |   1.3768 ns |   1.6031 ns |     ? |      - |         - |           ? |
