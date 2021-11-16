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
|                  Method |              Runtime |      Mean | Ratio | Allocated |
|------------------------ |--------------------- |----------:|------:|----------:|
|        TypeMapperAsEnum |             .NET 5.0 | 24.032 ns | 23.14 |      24 B |
|      DirectAccessAsEnum |             .NET 5.0 |  1.083 ns |  1.04 |         - |
|      TypeMapperAsObject |             .NET 5.0 | 29.257 ns | 28.18 |      48 B |
|    DirectAccessAsObject |             .NET 5.0 |  3.981 ns |  3.83 |      24 B |
|     TypeMapperAsDecimal |             .NET 5.0 |  3.288 ns |  3.17 |         - |
|   DirectAccessAsDecimal |             .NET 5.0 |  1.153 ns |  1.11 |         - |
|     TypeMapperAsBoolean |             .NET 5.0 |  2.393 ns |  2.31 |         - |
|   DirectAccessAsBoolean |             .NET 5.0 |  1.047 ns |  1.01 |         - |
|      TypeMapperAsString |             .NET 5.0 |  2.314 ns |  2.23 |         - |
|    DirectAccessAsString |             .NET 5.0 |  1.056 ns |  1.02 |         - |
|         TypeMapperAsInt |             .NET 5.0 |  2.358 ns |  2.27 |         - |
|       DirectAccessAsInt |             .NET 5.0 |  1.078 ns |  1.04 |         - |
|        TypeMapperAsBool |             .NET 5.0 |  2.919 ns |  2.81 |         - |
|      DirectAccessAsBool |             .NET 5.0 |  1.068 ns |  1.03 |         - |
|   TypeMapperAsKnownEnum |             .NET 5.0 |  2.677 ns |  2.58 |         - |
| DirectAccessAsKnownEnum |             .NET 5.0 |  1.057 ns |  1.02 |         - |
|        TypeMapperAsEnum |        .NET Core 3.1 | 25.250 ns | 24.32 |      24 B |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.078 ns |  1.04 |         - |
|      TypeMapperAsObject |        .NET Core 3.1 | 30.136 ns | 29.03 |      48 B |
|    DirectAccessAsObject |        .NET Core 3.1 |  4.273 ns |  4.12 |      24 B |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  3.841 ns |  3.74 |         - |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  1.381 ns |  1.33 |         - |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.379 ns |  2.29 |         - |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  1.308 ns |  1.26 |         - |
|      TypeMapperAsString |        .NET Core 3.1 |  2.415 ns |  2.33 |         - |
|    DirectAccessAsString |        .NET Core 3.1 |  1.365 ns |  1.31 |         - |
|         TypeMapperAsInt |        .NET Core 3.1 |  2.389 ns |  2.30 |         - |
|       DirectAccessAsInt |        .NET Core 3.1 |  1.288 ns |  1.24 |         - |
|        TypeMapperAsBool |        .NET Core 3.1 |  2.373 ns |  2.29 |         - |
|      DirectAccessAsBool |        .NET Core 3.1 |  1.311 ns |  1.26 |         - |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.156 ns |  2.08 |         - |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.063 ns |  1.02 |         - |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 43.683 ns | 42.08 |      24 B |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.038 ns |  1.00 |         - |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 51.322 ns | 49.45 |      48 B |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  3.917 ns |  3.77 |      24 B |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 | 10.035 ns |  9.67 |         - |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  1.487 ns |  1.43 |         - |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 |  8.344 ns |  8.04 |         - |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  1.086 ns |  1.05 |         - |
|      TypeMapperAsString | .NET Framework 4.7.2 |  8.470 ns |  8.16 |         - |
|    DirectAccessAsString | .NET Framework 4.7.2 |  1.084 ns |  1.04 |         - |
|         TypeMapperAsInt | .NET Framework 4.7.2 |  9.384 ns |  9.09 |         - |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  1.118 ns |  1.08 |         - |
|        TypeMapperAsBool | .NET Framework 4.7.2 |  8.283 ns |  7.98 |         - |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  1.058 ns |  1.02 |         - |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  8.293 ns |  7.99 |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.063 ns |  1.02 |         - |
