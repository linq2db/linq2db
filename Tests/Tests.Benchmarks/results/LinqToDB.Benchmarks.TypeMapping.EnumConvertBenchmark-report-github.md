``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                                Method |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------- |-------------- |----------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|             TestCastConvertTypeMapper |    .NET 4.6.2 | 14.080 ns | 0.1750 ns | 0.0271 ns | 13.01 |    0.06 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess |    .NET 4.6.2 |  1.083 ns | 0.0424 ns | 0.0066 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper |    .NET 4.6.2 | 41.677 ns | 1.1744 ns | 0.3050 ns | 38.46 |    0.34 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess |    .NET 4.6.2 |  1.077 ns | 0.1030 ns | 0.0267 ns |  0.99 |    0.03 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper |    .NET 4.6.2 | 75.985 ns | 0.8646 ns | 0.2245 ns | 70.21 |    0.49 | 0.0114 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess |    .NET 4.6.2 |  1.103 ns | 0.0535 ns | 0.0139 ns |  1.02 |    0.01 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper |    .NET 4.6.2 | 14.365 ns | 1.0009 ns | 0.2599 ns | 13.31 |    0.34 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess |    .NET 4.6.2 |  1.081 ns | 0.0457 ns | 0.0025 ns |  1.00 |    0.01 |      - |     - |     - |         - |
|             TestCastConvertTypeMapper | .NET Core 2.1 |  5.988 ns | 0.0907 ns | 0.0236 ns |  5.53 |    0.03 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess | .NET Core 2.1 |  1.379 ns | 0.1528 ns | 0.0397 ns |  1.28 |    0.04 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper | .NET Core 2.1 | 28.587 ns | 2.1562 ns | 0.5599 ns | 26.48 |    0.68 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess | .NET Core 2.1 |  1.355 ns | 0.0392 ns | 0.0061 ns |  1.25 |    0.01 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Core 2.1 | 64.968 ns | 1.1597 ns | 0.3012 ns | 60.06 |    0.20 | 0.0113 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Core 2.1 |  1.352 ns | 0.0364 ns | 0.0056 ns |  1.25 |    0.01 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper | .NET Core 2.1 |  5.993 ns | 0.1052 ns | 0.0273 ns |  5.54 |    0.06 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess | .NET Core 2.1 |  1.416 ns | 0.0619 ns | 0.0096 ns |  1.31 |    0.02 |      - |     - |     - |         - |
|             TestCastConvertTypeMapper | .NET Core 3.1 |  6.603 ns | 0.1813 ns | 0.0471 ns |  6.10 |    0.06 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess | .NET Core 3.1 |  1.061 ns | 0.0351 ns | 0.0054 ns |  0.98 |    0.01 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper | .NET Core 3.1 | 26.787 ns | 0.3333 ns | 0.0866 ns | 24.75 |    0.11 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess | .NET Core 3.1 |  1.069 ns | 0.0395 ns | 0.0103 ns |  0.99 |    0.01 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Core 3.1 | 55.861 ns | 1.1527 ns | 0.2994 ns | 51.52 |    0.53 | 0.0114 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Core 3.1 |  1.040 ns | 0.1624 ns | 0.0422 ns |  0.97 |    0.04 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper | .NET Core 3.1 |  6.694 ns | 0.3717 ns | 0.0965 ns |  6.22 |    0.07 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess | .NET Core 3.1 |  1.068 ns | 0.0801 ns | 0.0208 ns |  0.99 |    0.02 |      - |     - |     - |         - |
