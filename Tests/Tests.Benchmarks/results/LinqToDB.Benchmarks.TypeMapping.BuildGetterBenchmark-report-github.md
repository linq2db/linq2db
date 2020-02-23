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
|                  Method |       Runtime |      Mean |      Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |----------:|-----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 49.618 ns | 17.6646 ns | 4.5874 ns | 44.18 |    6.25 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |  1.130 ns |  0.2703 ns | 0.0702 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      TypeMapperAsObject |    .NET 4.6.2 | 49.676 ns |  3.4121 ns | 0.8861 ns | 44.11 |    3.25 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject |    .NET 4.6.2 |  4.186 ns |  0.4518 ns | 0.1173 ns |  3.72 |    0.28 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal |    .NET 4.6.2 | 10.223 ns |  0.1170 ns | 0.0181 ns |  8.99 |    0.59 |      - |     - |     - |         - |
|   DirectAccessAsDecimal |    .NET 4.6.2 |  1.436 ns |  0.0330 ns | 0.0051 ns |  1.26 |    0.08 |      - |     - |     - |         - |
|     TypeMapperAsBoolean |    .NET 4.6.2 |  8.878 ns |  0.1871 ns | 0.0486 ns |  7.88 |    0.48 |      - |     - |     - |         - |
|   DirectAccessAsBoolean |    .NET 4.6.2 |  1.075 ns |  0.0546 ns | 0.0084 ns |  0.95 |    0.07 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 |  8.683 ns |  0.8314 ns | 0.2159 ns |  7.71 |    0.52 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |  1.092 ns |  0.0265 ns | 0.0015 ns |  0.94 |    0.06 |      - |     - |     - |         - |
|         TypeMapperAsInt |    .NET 4.6.2 |  8.990 ns |  0.0795 ns | 0.0123 ns |  7.91 |    0.53 |      - |     - |     - |         - |
|       DirectAccessAsInt |    .NET 4.6.2 |  1.061 ns |  0.0396 ns | 0.0103 ns |  0.94 |    0.06 |      - |     - |     - |         - |
|        TypeMapperAsBool |    .NET 4.6.2 |  9.171 ns |  1.1693 ns | 0.3037 ns |  8.13 |    0.35 |      - |     - |     - |         - |
|      DirectAccessAsBool |    .NET 4.6.2 |  1.076 ns |  0.0377 ns | 0.0058 ns |  0.95 |    0.06 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |  8.524 ns |  0.6945 ns | 0.1804 ns |  7.56 |    0.31 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |  1.070 ns |  0.0431 ns | 0.0067 ns |  0.94 |    0.06 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 | 31.140 ns |  0.4610 ns | 0.1197 ns | 27.63 |    1.64 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum | .NET Core 2.1 |  1.347 ns |  0.0499 ns | 0.0130 ns |  1.20 |    0.07 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 2.1 | 37.141 ns |  5.9085 ns | 1.5344 ns | 32.91 |    1.43 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject | .NET Core 2.1 |  4.799 ns |  0.8196 ns | 0.2128 ns |  4.26 |    0.26 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 2.1 |  4.302 ns |  0.1189 ns | 0.0309 ns |  3.82 |    0.25 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 2.1 |  1.498 ns |  0.0635 ns | 0.0098 ns |  1.32 |    0.09 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 2.1 |  2.692 ns |  0.0697 ns | 0.0108 ns |  2.37 |    0.15 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 2.1 |  1.047 ns |  0.0427 ns | 0.0111 ns |  0.93 |    0.06 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |  2.669 ns |  0.0730 ns | 0.0113 ns |  2.35 |    0.15 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |  1.108 ns |  0.1517 ns | 0.0394 ns |  0.98 |    0.05 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 2.1 |  2.673 ns |  0.0812 ns | 0.0126 ns |  2.35 |    0.17 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 2.1 |  1.047 ns |  0.0513 ns | 0.0079 ns |  0.92 |    0.07 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 2.1 |  2.686 ns |  0.1088 ns | 0.0283 ns |  2.38 |    0.14 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 2.1 |  1.129 ns |  0.1190 ns | 0.0309 ns |  1.00 |    0.06 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |  2.968 ns |  0.0891 ns | 0.0231 ns |  2.63 |    0.17 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |  1.379 ns |  0.0591 ns | 0.0153 ns |  1.22 |    0.07 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 | 26.340 ns |  0.8853 ns | 0.2299 ns | 23.38 |    1.52 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum | .NET Core 3.1 |  1.357 ns |  0.0399 ns | 0.0062 ns |  1.19 |    0.08 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 3.1 | 31.499 ns |  1.0488 ns | 0.2724 ns | 27.95 |    1.55 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject | .NET Core 3.1 |  5.484 ns |  1.1570 ns | 0.3005 ns |  4.86 |    0.34 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 3.1 |  3.818 ns |  0.0433 ns | 0.0024 ns |  3.30 |    0.23 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 3.1 |  1.372 ns |  0.1031 ns | 0.0268 ns |  1.22 |    0.08 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 3.1 |  2.411 ns |  0.0652 ns | 0.0101 ns |  2.12 |    0.14 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 3.1 |  1.377 ns |  0.0778 ns | 0.0202 ns |  1.22 |    0.07 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |  2.476 ns |  0.2038 ns | 0.0529 ns |  2.20 |    0.12 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |  1.357 ns |  0.1794 ns | 0.0466 ns |  1.21 |    0.11 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 3.1 |  2.445 ns |  0.1074 ns | 0.0279 ns |  2.17 |    0.12 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 3.1 |  1.338 ns |  0.0518 ns | 0.0080 ns |  1.18 |    0.07 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 3.1 |  2.456 ns |  0.0649 ns | 0.0169 ns |  2.18 |    0.14 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 3.1 |  1.362 ns |  0.0689 ns | 0.0179 ns |  1.21 |    0.08 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |  2.466 ns |  0.2832 ns | 0.0736 ns |  2.19 |    0.17 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |  1.358 ns |  0.0342 ns | 0.0053 ns |  1.19 |    0.08 |      - |     - |     - |         - |
