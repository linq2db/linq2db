``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                                Method |       Runtime |      Mean | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------- |-------------- |----------:|------:|-------:|------:|------:|----------:|
|             TestCastConvertTypeMapper |    .NET 4.6.2 | 14.445 ns | 13.05 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess |    .NET 4.6.2 |  1.108 ns |  1.00 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper |    .NET 4.6.2 | 44.103 ns | 39.93 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess |    .NET 4.6.2 |  1.041 ns |  0.94 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper |    .NET 4.6.2 | 78.542 ns | 71.12 | 0.0114 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess |    .NET 4.6.2 |  1.073 ns |  0.97 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper |    .NET 4.6.2 | 14.064 ns | 12.74 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess |    .NET 4.6.2 |  1.078 ns |  0.97 |      - |     - |     - |         - |
|             TestCastConvertTypeMapper | .NET Core 2.1 |  5.810 ns |  5.25 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess | .NET Core 2.1 |  1.155 ns |  1.05 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper | .NET Core 2.1 | 26.559 ns | 24.05 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess | .NET Core 2.1 |  1.076 ns |  0.98 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Core 2.1 | 62.194 ns | 56.16 | 0.0113 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Core 2.1 |  1.079 ns |  0.98 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper | .NET Core 2.1 |  5.817 ns |  5.25 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess | .NET Core 2.1 |  1.082 ns |  0.98 |      - |     - |     - |         - |
|             TestCastConvertTypeMapper | .NET Core 3.1 |  6.255 ns |  5.65 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess | .NET Core 3.1 |  1.087 ns |  0.99 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper | .NET Core 3.1 | 29.127 ns | 26.37 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess | .NET Core 3.1 |  1.090 ns |  0.99 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Core 3.1 | 55.501 ns | 50.12 | 0.0114 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Core 3.1 |  1.140 ns |  1.03 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper | .NET Core 3.1 |  6.155 ns |  5.60 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess | .NET Core 3.1 |  1.120 ns |  1.01 |      - |     - |     - |         - |
