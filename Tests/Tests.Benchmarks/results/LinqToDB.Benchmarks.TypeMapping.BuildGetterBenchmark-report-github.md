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
|        TypeMapperAsEnum |             .NET 5.0 | 26.370 ns | 26.135 ns | 21.08 |      24 B |
|      DirectAccessAsEnum |             .NET 5.0 |  1.134 ns |  1.126 ns |  1.01 |         - |
|      TypeMapperAsObject |             .NET 5.0 | 32.250 ns | 32.344 ns | 25.79 |      48 B |
|    DirectAccessAsObject |             .NET 5.0 |  4.373 ns |  4.314 ns |  3.58 |      24 B |
|     TypeMapperAsDecimal |             .NET 5.0 |  3.905 ns |  3.889 ns |  3.14 |         - |
|   DirectAccessAsDecimal |             .NET 5.0 |  1.184 ns |  1.207 ns |  0.92 |         - |
|     TypeMapperAsBoolean |             .NET 5.0 |  2.338 ns |  2.310 ns |  1.90 |         - |
|   DirectAccessAsBoolean |             .NET 5.0 |  1.138 ns |  1.136 ns |  0.91 |         - |
|      TypeMapperAsString |             .NET 5.0 |  2.264 ns |  2.275 ns |  1.81 |         - |
|    DirectAccessAsString |             .NET 5.0 |  1.264 ns |  1.198 ns |  1.08 |         - |
|         TypeMapperAsInt |             .NET 5.0 |  2.383 ns |  2.414 ns |  1.98 |         - |
|       DirectAccessAsInt |             .NET 5.0 |  1.078 ns |  1.085 ns |  0.85 |         - |
|        TypeMapperAsBool |             .NET 5.0 |  2.815 ns |  2.820 ns |  2.25 |         - |
|      DirectAccessAsBool |             .NET 5.0 |  1.163 ns |  1.152 ns |  0.93 |         - |
|   TypeMapperAsKnownEnum |             .NET 5.0 |  3.076 ns |  3.117 ns |  2.44 |         - |
| DirectAccessAsKnownEnum |             .NET 5.0 |  1.145 ns |  1.137 ns |  0.93 |         - |
|        TypeMapperAsEnum |        .NET Core 3.1 | 26.679 ns | 26.607 ns | 21.34 |      24 B |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.185 ns |  1.180 ns |  0.95 |         - |
|      TypeMapperAsObject |        .NET Core 3.1 | 31.669 ns | 31.204 ns | 25.39 |      48 B |
|    DirectAccessAsObject |        .NET Core 3.1 |  3.926 ns |  3.808 ns |  3.18 |      24 B |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  3.799 ns |  3.795 ns |  3.05 |         - |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  1.339 ns |  1.327 ns |  1.07 |         - |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.203 ns |  2.182 ns |  1.77 |         - |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  1.134 ns |  1.090 ns |  0.94 |         - |
|      TypeMapperAsString |        .NET Core 3.1 |  2.184 ns |  2.181 ns |  1.76 |         - |
|    DirectAccessAsString |        .NET Core 3.1 |  1.057 ns |  1.055 ns |  0.85 |         - |
|         TypeMapperAsInt |        .NET Core 3.1 |  2.205 ns |  2.229 ns |  1.76 |         - |
|       DirectAccessAsInt |        .NET Core 3.1 |  1.106 ns |  1.071 ns |  0.90 |         - |
|        TypeMapperAsBool |        .NET Core 3.1 |  2.261 ns |  2.291 ns |  1.81 |         - |
|      DirectAccessAsBool |        .NET Core 3.1 |  1.133 ns |  1.093 ns |  0.92 |         - |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.487 ns |  2.423 ns |  2.04 |         - |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.312 ns |  1.290 ns |  1.06 |         - |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 46.148 ns | 45.685 ns | 37.65 |      24 B |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.252 ns |  1.252 ns |  1.00 |         - |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 54.241 ns | 54.640 ns | 43.58 |      48 B |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  4.396 ns |  4.333 ns |  3.53 |      24 B |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 | 10.763 ns | 10.832 ns |  8.67 |         - |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  1.433 ns |  1.426 ns |  1.15 |         - |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 |  9.480 ns |  9.311 ns |  7.74 |         - |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  1.302 ns |  1.304 ns |  1.04 |         - |
|      TypeMapperAsString | .NET Framework 4.7.2 |  9.198 ns |  9.170 ns |  7.35 |         - |
|    DirectAccessAsString | .NET Framework 4.7.2 |  1.378 ns |  1.350 ns |  1.10 |         - |
|         TypeMapperAsInt | .NET Framework 4.7.2 |  9.881 ns |  9.894 ns |  7.95 |         - |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  1.390 ns |  1.369 ns |  1.14 |         - |
|        TypeMapperAsBool | .NET Framework 4.7.2 |  8.843 ns |  8.844 ns |  7.07 |         - |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  1.403 ns |  1.354 ns |  1.13 |         - |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  8.526 ns |  8.512 ns |  6.83 |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.064 ns |  1.062 ns |  0.85 |         - |
