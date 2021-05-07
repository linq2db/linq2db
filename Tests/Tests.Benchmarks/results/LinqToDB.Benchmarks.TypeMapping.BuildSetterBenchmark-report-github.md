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
|        TypeMapperAsEnum |             .NET 5.0 | 12.399 ns | 12.488 ns | 11.45 |         - |
|      DirectAccessAsEnum |             .NET 5.0 |  1.067 ns |  1.065 ns |  0.98 |         - |
|   TypeMapperAsKnownEnum |             .NET 5.0 |  2.137 ns |  2.137 ns |  1.97 |         - |
| DirectAccessAsKnownEnum |             .NET 5.0 |  1.051 ns |  1.052 ns |  0.97 |         - |
|      TypeMapperAsString |             .NET 5.0 |  4.233 ns |  4.239 ns |  3.90 |         - |
|    DirectAccessAsString |             .NET 5.0 |  3.198 ns |  3.198 ns |  2.95 |         - |
|        TypeMapperAsEnum |        .NET Core 3.1 | 13.720 ns | 13.727 ns | 12.64 |         - |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.060 ns |  1.059 ns |  0.98 |         - |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.674 ns |  2.657 ns |  2.46 |         - |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.076 ns |  1.067 ns |  0.99 |         - |
|      TypeMapperAsString |        .NET Core 3.1 |  4.624 ns |  4.548 ns |  4.29 |         - |
|    DirectAccessAsString |        .NET Core 3.1 |  2.925 ns |  2.923 ns |  2.70 |         - |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 28.792 ns | 28.423 ns | 27.05 |         - |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.085 ns |  1.077 ns |  1.00 |         - |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  8.557 ns |  8.561 ns |  7.88 |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.129 ns |  1.086 ns |  1.04 |         - |
|      TypeMapperAsString | .NET Framework 4.7.2 | 10.846 ns | 10.765 ns |  9.99 |         - |
|    DirectAccessAsString | .NET Framework 4.7.2 |  3.162 ns |  3.159 ns |  2.91 |         - |
