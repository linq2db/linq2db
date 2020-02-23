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
|             TestCastConvertTypeMapper |    .NET 4.6.2 | 14.417 ns | 0.6463 ns | 0.1679 ns | 10.70 |    0.19 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess |    .NET 4.6.2 |  1.348 ns | 0.0466 ns | 0.0072 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper |    .NET 4.6.2 | 44.346 ns | 2.8781 ns | 0.7474 ns | 32.65 |    0.33 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess |    .NET 4.6.2 |  1.349 ns | 0.0501 ns | 0.0130 ns |  1.00 |    0.01 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper |    .NET 4.6.2 | 76.692 ns | 3.7864 ns | 0.9833 ns | 57.08 |    0.96 | 0.0114 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess |    .NET 4.6.2 |  1.306 ns | 0.0384 ns | 0.0100 ns |  0.97 |    0.01 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper |    .NET 4.6.2 | 14.308 ns | 0.3293 ns | 0.0855 ns | 10.63 |    0.07 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess |    .NET 4.6.2 |  1.358 ns | 0.0341 ns | 0.0089 ns |  1.01 |    0.00 |      - |     - |     - |         - |
|             TestCastConvertTypeMapper | .NET Core 2.1 |  5.651 ns | 0.1950 ns | 0.0506 ns |  4.18 |    0.06 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess | .NET Core 2.1 |  1.080 ns | 0.0536 ns | 0.0083 ns |  0.80 |    0.00 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper | .NET Core 2.1 | 26.565 ns | 0.3584 ns | 0.0196 ns | 19.74 |    0.09 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess | .NET Core 2.1 |  1.076 ns | 0.0261 ns | 0.0040 ns |  0.80 |    0.01 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Core 2.1 | 63.731 ns | 1.3675 ns | 0.3551 ns | 47.33 |    0.23 | 0.0113 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Core 2.1 |  1.083 ns | 0.0462 ns | 0.0072 ns |  0.80 |    0.01 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper | .NET Core 2.1 |  5.716 ns | 0.2357 ns | 0.0612 ns |  4.25 |    0.06 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess | .NET Core 2.1 |  1.074 ns | 0.0547 ns | 0.0142 ns |  0.80 |    0.01 |      - |     - |     - |         - |
|             TestCastConvertTypeMapper | .NET Core 3.1 |  5.798 ns | 0.0947 ns | 0.0147 ns |  4.30 |    0.02 |      - |     - |     - |         - |
|           TestCastConvertDirectAccess | .NET Core 3.1 |  1.336 ns | 0.0305 ns | 0.0047 ns |  0.99 |    0.01 |      - |     - |     - |         - |
|       TestDictionaryConvertTypeMapper | .NET Core 3.1 | 27.410 ns | 0.4427 ns | 0.1150 ns | 20.35 |    0.10 |      - |     - |     - |         - |
|     TestDictionaryConvertDirectAccess | .NET Core 3.1 |  1.373 ns | 0.0420 ns | 0.0109 ns |  1.02 |    0.01 |      - |     - |     - |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Core 3.1 | 55.972 ns | 2.6606 ns | 0.6910 ns | 41.49 |    0.47 | 0.0114 |     - |     - |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Core 3.1 |  1.358 ns | 0.0434 ns | 0.0067 ns |  1.01 |    0.00 |      - |     - |     - |         - |
|        TestFlagsCastConvertTypeMapper | .NET Core 3.1 |  5.842 ns | 0.2880 ns | 0.0748 ns |  4.34 |    0.08 |      - |     - |     - |         - |
|      TestFlagsCastConvertDirectAccess | .NET Core 3.1 |  1.368 ns | 0.1246 ns | 0.0324 ns |  1.02 |    0.03 |      - |     - |     - |         - |
