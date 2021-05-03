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
|             TestCastConvertTypeMapper |             .NET 5.0 |  6.542 ns |  6.555 ns |  4.86 |         - |
|           TestCastConvertDirectAccess |             .NET 5.0 |  1.075 ns |  1.075 ns |  0.80 |         - |
|       TestDictionaryConvertTypeMapper |             .NET 5.0 | 26.388 ns | 26.501 ns | 22.22 |         - |
|     TestDictionaryConvertDirectAccess |             .NET 5.0 |  1.066 ns |  1.056 ns |  0.79 |         - |
|   TestDictionaryCastConvertTypeMapper |             .NET 5.0 | 50.458 ns | 49.788 ns | 41.97 |      48 B |
| TestDictionaryCastConvertDirectAccess |             .NET 5.0 |  1.045 ns |  1.045 ns |  0.78 |         - |
|        TestFlagsCastConvertTypeMapper |             .NET 5.0 |  6.634 ns |  6.572 ns |  5.16 |         - |
|      TestFlagsCastConvertDirectAccess |             .NET 5.0 |  1.083 ns |  1.080 ns |  0.80 |         - |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |  6.149 ns |  6.145 ns |  4.57 |         - |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |  1.311 ns |  1.311 ns |  0.97 |         - |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 | 28.373 ns | 28.207 ns | 22.05 |         - |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |  1.309 ns |  1.312 ns |  0.97 |         - |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 | 56.946 ns | 57.175 ns | 49.35 |      48 B |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |  1.183 ns |  1.181 ns |  0.92 |         - |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |  6.222 ns |  6.098 ns |  5.26 |         - |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |  1.317 ns |  1.314 ns |  0.98 |         - |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 | 21.523 ns | 21.484 ns | 18.66 |         - |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |  1.198 ns |  1.157 ns |  1.00 |         - |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 | 64.083 ns | 63.203 ns | 51.14 |         - |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |  1.063 ns |  1.054 ns |  0.79 |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 99.881 ns | 99.669 ns | 79.22 |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |  1.054 ns |  1.055 ns |  0.78 |         - |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 | 20.455 ns | 20.217 ns | 16.66 |         - |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |  1.092 ns |  1.089 ns |  0.81 |         - |
