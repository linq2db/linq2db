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
|                  Method |              Runtime |      Mean |    Median | Ratio | Allocated |
|------------------------ |--------------------- |----------:|----------:|------:|----------:|
|        TypeMapperAsEnum |             .NET 5.0 | 24.416 ns | 24.388 ns | 16.59 |      24 B |
|      DirectAccessAsEnum |             .NET 5.0 |  1.102 ns |  1.090 ns |  0.75 |         - |
|      TypeMapperAsObject |             .NET 5.0 | 29.928 ns | 29.476 ns | 21.66 |      48 B |
|    DirectAccessAsObject |             .NET 5.0 |  4.092 ns |  4.074 ns |  2.80 |      24 B |
|     TypeMapperAsDecimal |             .NET 5.0 |  3.409 ns |  3.415 ns |  2.32 |         - |
|   DirectAccessAsDecimal |             .NET 5.0 |  1.113 ns |  1.093 ns |  0.76 |         - |
|     TypeMapperAsBoolean |             .NET 5.0 |  2.505 ns |  2.430 ns |  1.80 |         - |
|   DirectAccessAsBoolean |             .NET 5.0 |  1.092 ns |  1.086 ns |  0.75 |         - |
|      TypeMapperAsString |             .NET 5.0 |  2.129 ns |  2.122 ns |  1.45 |         - |
|    DirectAccessAsString |             .NET 5.0 |  1.050 ns |  1.038 ns |  0.71 |         - |
|         TypeMapperAsInt |             .NET 5.0 |  2.234 ns |  2.225 ns |  1.54 |         - |
|       DirectAccessAsInt |             .NET 5.0 |  1.118 ns |  1.114 ns |  0.76 |         - |
|        TypeMapperAsBool |             .NET 5.0 |  3.054 ns |  2.970 ns |  2.19 |         - |
|      DirectAccessAsBool |             .NET 5.0 |  1.087 ns |  1.084 ns |  0.74 |         - |
|   TypeMapperAsKnownEnum |             .NET 5.0 |  3.058 ns |  3.019 ns |  2.12 |         - |
| DirectAccessAsKnownEnum |             .NET 5.0 |  1.089 ns |  1.086 ns |  0.74 |         - |
|        TypeMapperAsEnum |        .NET Core 3.1 | 25.648 ns | 25.405 ns | 18.24 |      24 B |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.118 ns |  1.062 ns |  0.81 |         - |
|      TypeMapperAsObject |        .NET Core 3.1 | 29.953 ns | 29.844 ns | 20.47 |      48 B |
|    DirectAccessAsObject |        .NET Core 3.1 |  4.627 ns |  4.479 ns |  3.38 |      24 B |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  3.803 ns |  3.781 ns |  2.60 |         - |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  1.375 ns |  1.349 ns |  0.93 |         - |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.410 ns |  2.409 ns |  1.66 |         - |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  1.374 ns |  1.348 ns |  0.95 |         - |
|      TypeMapperAsString |        .NET Core 3.1 |  2.584 ns |  2.482 ns |  1.85 |         - |
|    DirectAccessAsString |        .NET Core 3.1 |  1.343 ns |  1.336 ns |  0.92 |         - |
|         TypeMapperAsInt |        .NET Core 3.1 |  2.532 ns |  2.496 ns |  1.81 |         - |
|       DirectAccessAsInt |        .NET Core 3.1 |  1.355 ns |  1.340 ns |  0.93 |         - |
|        TypeMapperAsBool |        .NET Core 3.1 |  2.499 ns |  2.454 ns |  1.73 |         - |
|      DirectAccessAsBool |        .NET Core 3.1 |  1.469 ns |  1.446 ns |  1.06 |         - |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.113 ns |  2.089 ns |  1.44 |         - |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.072 ns |  1.063 ns |  0.74 |         - |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 46.352 ns | 45.581 ns | 33.04 |      24 B |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.397 ns |  1.356 ns |  1.00 |         - |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 51.756 ns | 50.534 ns | 37.72 |      48 B |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  4.475 ns |  4.299 ns |  3.35 |      24 B |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 | 10.801 ns | 10.885 ns |  7.81 |         - |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  1.465 ns |  1.432 ns |  1.02 |         - |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 |  9.000 ns |  8.996 ns |  6.15 |         - |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  1.337 ns |  1.313 ns |  0.92 |         - |
|      TypeMapperAsString | .NET Framework 4.7.2 |  8.866 ns |  8.849 ns |  6.12 |         - |
|    DirectAccessAsString | .NET Framework 4.7.2 |  1.230 ns |  1.195 ns |  0.84 |         - |
|         TypeMapperAsInt | .NET Framework 4.7.2 |  9.092 ns |  9.076 ns |  6.25 |         - |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  1.294 ns |  1.296 ns |  0.88 |         - |
|        TypeMapperAsBool | .NET Framework 4.7.2 |  8.826 ns |  8.811 ns |  6.07 |         - |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  1.374 ns |  1.350 ns |  0.95 |         - |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  8.928 ns |  8.906 ns |  6.10 |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.345 ns |  1.322 ns |  0.92 |         - |
