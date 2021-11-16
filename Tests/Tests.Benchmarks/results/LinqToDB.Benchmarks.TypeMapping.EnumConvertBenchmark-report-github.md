``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                Method |              Runtime |      Mean | Ratio | Allocated |
|-------------------------------------- |--------------------- |----------:|------:|----------:|
|             TestCastConvertTypeMapper |             .NET 5.0 |  6.520 ns |  4.83 |         - |
|           TestCastConvertDirectAccess |             .NET 5.0 |  1.059 ns |  0.79 |         - |
|       TestDictionaryConvertTypeMapper |             .NET 5.0 | 24.432 ns | 18.11 |         - |
|     TestDictionaryConvertDirectAccess |             .NET 5.0 |  1.043 ns |  0.77 |         - |
|   TestDictionaryCastConvertTypeMapper |             .NET 5.0 | 49.701 ns | 36.99 |      48 B |
| TestDictionaryCastConvertDirectAccess |             .NET 5.0 |  1.067 ns |  0.79 |         - |
|        TestFlagsCastConvertTypeMapper |             .NET 5.0 |  6.422 ns |  4.76 |         - |
|      TestFlagsCastConvertDirectAccess |             .NET 5.0 |  1.062 ns |  0.79 |         - |
|             TestCastConvertTypeMapper |        .NET Core 3.1 |  5.707 ns |  4.23 |         - |
|           TestCastConvertDirectAccess |        .NET Core 3.1 |  1.078 ns |  0.80 |         - |
|       TestDictionaryConvertTypeMapper |        .NET Core 3.1 | 27.524 ns | 20.40 |         - |
|     TestDictionaryConvertDirectAccess |        .NET Core 3.1 |  1.077 ns |  0.80 |         - |
|   TestDictionaryCastConvertTypeMapper |        .NET Core 3.1 | 52.259 ns | 38.77 |      48 B |
| TestDictionaryCastConvertDirectAccess |        .NET Core 3.1 |  1.043 ns |  0.77 |         - |
|        TestFlagsCastConvertTypeMapper |        .NET Core 3.1 |  6.367 ns |  4.72 |         - |
|      TestFlagsCastConvertDirectAccess |        .NET Core 3.1 |  1.039 ns |  0.77 |         - |
|             TestCastConvertTypeMapper | .NET Framework 4.7.2 | 20.126 ns | 14.91 |         - |
|           TestCastConvertDirectAccess | .NET Framework 4.7.2 |  1.350 ns |  1.00 |         - |
|       TestDictionaryConvertTypeMapper | .NET Framework 4.7.2 | 59.824 ns | 44.35 |         - |
|     TestDictionaryConvertDirectAccess | .NET Framework 4.7.2 |  1.329 ns |  0.99 |         - |
|   TestDictionaryCastConvertTypeMapper | .NET Framework 4.7.2 | 97.127 ns | 72.17 |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |  1.357 ns |  1.00 |         - |
|        TestFlagsCastConvertTypeMapper | .NET Framework 4.7.2 | 19.967 ns | 14.80 |         - |
|      TestFlagsCastConvertDirectAccess | .NET Framework 4.7.2 |  1.327 ns |  0.98 |         - |
