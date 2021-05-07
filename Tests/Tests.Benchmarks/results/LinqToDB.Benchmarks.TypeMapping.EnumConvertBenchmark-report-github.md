``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                Method |              Runtime |      Mean |    Median | Ratio | Allocated |
|-------------------------------------- |--------------------- |----------:|----------:|------:|----------:|
|             TestCastConvertTypeMapper |             .NET 5.0 |  6.536 ns |  6.539 ns |  4.93 |         - |
|           TestCastConvertDirectAccess |             .NET 5.0 |  1.066 ns |  1.065 ns |  0.80 |         - |
|       TestDictionaryConvertTypeMapper |             .NET 5.0 | 26.395 ns | 26.340 ns | 20.10 |         - |
|     TestDictionaryConvertDirectAccess |             .NET 5.0 |  1.056 ns |  1.058 ns |  0.80 |         - |
|   TestDictionaryCastConvertTypeMapper |             .NET 5.0 | 50.006 ns | 49.465 ns | 38.34 |      48 B |
| TestDictionaryCastConvertDirectAccess |             .NET 5.0 |  1.052 ns |  1.052 ns |  0.79 |         - |
|        TestFlagsCastConvertTypeMapper |             .NET 5.0 |  6.625 ns |  6.495 ns |  5.17 |         - |
|      TestFlagsCastConvertDirectAccess |             .NET 5.0 |  1.059 ns |  1.059 ns |  0.80 |         - |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |  5.737 ns |  5.691 ns |  4.35 |         - |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |  1.335 ns |  1.334 ns |  1.00 |         - |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 | 29.260 ns | 28.978 ns | 22.55 |         - |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |  1.342 ns |  1.343 ns |  1.01 |         - |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 | 52.898 ns | 52.618 ns | 39.97 |      48 B |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |  1.340 ns |  1.341 ns |  1.01 |         - |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |  5.715 ns |  5.704 ns |  4.31 |         - |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |  1.328 ns |  1.329 ns |  1.00 |         - |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 | 20.008 ns | 19.980 ns | 15.07 |         - |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |  1.326 ns |  1.329 ns |  1.00 |         - |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 | 62.405 ns | 62.367 ns | 47.01 |         - |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |  1.327 ns |  1.326 ns |  1.00 |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 96.459 ns | 96.466 ns | 72.66 |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |  1.323 ns |  1.326 ns |  1.00 |         - |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 | 20.393 ns | 20.316 ns | 15.38 |         - |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |  1.367 ns |  1.351 ns |  1.03 |         - |
