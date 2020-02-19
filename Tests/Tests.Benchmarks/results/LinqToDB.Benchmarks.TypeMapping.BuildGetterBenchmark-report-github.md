``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZXOHUL : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TAKBNN : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-WOIQBX : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=10  
MinIterationCount=5  WarmupCount=2  

```
|                  Method |       Runtime |         Mean |      Error |     StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |-------------:|-----------:|-----------:|---------:|--------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 1,134.013 ns | 27.0269 ns | 16.0833 ns | 1,019.92 |   37.58 | 0.0420 |     - |     - |     177 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |     1.109 ns |  0.0637 ns |  0.0421 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|      TypeMapperAsObject |    .NET 4.6.2 | 1,239.722 ns | 46.3369 ns | 27.5744 ns | 1,114.35 |   21.05 | 0.0477 |     - |     - |     201 B |
|    DirectAccessAsObject |    .NET 4.6.2 |     4.149 ns |  0.3084 ns |  0.1835 ns |     3.73 |    0.16 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal |    .NET 4.6.2 |    10.487 ns |  0.6207 ns |  0.4106 ns |     9.45 |    0.15 |      - |     - |     - |         - |
|   DirectAccessAsDecimal |    .NET 4.6.2 |     1.528 ns |  0.0770 ns |  0.0509 ns |     1.38 |    0.08 |      - |     - |     - |         - |
|     TypeMapperAsBoolean |    .NET 4.6.2 |     9.726 ns |  0.4556 ns |  0.2711 ns |     8.74 |    0.15 |      - |     - |     - |         - |
|   DirectAccessAsBoolean |    .NET 4.6.2 |     1.111 ns |  0.1385 ns |  0.0916 ns |     1.00 |    0.09 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 |     9.108 ns |  0.2606 ns |  0.0677 ns |     8.01 |    0.25 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |     1.139 ns |  0.1552 ns |  0.1027 ns |     1.03 |    0.08 |      - |     - |     - |         - |
|         TypeMapperAsInt |    .NET 4.6.2 |     9.537 ns |  0.1547 ns |  0.0402 ns |     8.39 |    0.29 |      - |     - |     - |         - |
|       DirectAccessAsInt |    .NET 4.6.2 |     1.137 ns |  0.1488 ns |  0.0984 ns |     1.03 |    0.10 |      - |     - |     - |         - |
|        TypeMapperAsBool |    .NET 4.6.2 |     9.436 ns |  0.3591 ns |  0.2375 ns |     8.52 |    0.41 |      - |     - |     - |         - |
|      DirectAccessAsBool |    .NET 4.6.2 |     1.144 ns |  0.0770 ns |  0.0200 ns |     1.01 |    0.04 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |     9.245 ns |  0.4670 ns |  0.3089 ns |     8.34 |    0.36 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |     1.144 ns |  0.1230 ns |  0.0814 ns |     1.03 |    0.10 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 |   492.696 ns |  9.0395 ns |  2.3475 ns |   433.29 |   13.46 | 0.0296 |     - |     - |     128 B |
|      DirectAccessAsEnum | .NET Core 2.1 |     1.345 ns |  0.0196 ns |  0.0051 ns |     1.18 |    0.04 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 2.1 |   532.107 ns | 10.9054 ns |  5.7037 ns |   476.72 |   22.20 | 0.0353 |     - |     - |     152 B |
|    DirectAccessAsObject | .NET Core 2.1 |     5.265 ns |  0.5466 ns |  0.3616 ns |     4.75 |    0.31 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 2.1 |     4.061 ns |  0.2589 ns |  0.1712 ns |     3.66 |    0.17 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 2.1 |     1.513 ns |  0.0604 ns |  0.0215 ns |     1.34 |    0.05 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 2.1 |     2.467 ns |  0.0731 ns |  0.0261 ns |     2.18 |    0.08 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 2.1 |     1.392 ns |  0.0699 ns |  0.0462 ns |     1.26 |    0.07 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |     2.751 ns |  0.3305 ns |  0.2186 ns |     2.48 |    0.17 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |     1.365 ns |  0.0797 ns |  0.0207 ns |     1.20 |    0.05 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 2.1 |     2.618 ns |  0.1185 ns |  0.0784 ns |     2.36 |    0.12 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 2.1 |     1.534 ns |  0.1923 ns |  0.1272 ns |     1.39 |    0.15 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 2.1 |     2.652 ns |  0.2451 ns |  0.1621 ns |     2.39 |    0.08 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 2.1 |     1.329 ns |  0.0609 ns |  0.0270 ns |     1.18 |    0.04 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |     2.505 ns |  0.0701 ns |  0.0311 ns |     2.23 |    0.08 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |     1.413 ns |  0.0851 ns |  0.0563 ns |     1.28 |    0.07 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 |   190.149 ns |  5.9872 ns |  3.1314 ns |   170.27 |    5.80 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 3.1 |     1.083 ns |  0.0559 ns |  0.0145 ns |     0.95 |    0.04 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 3.1 |   193.225 ns |  3.6366 ns |  2.4054 ns |   174.39 |    6.36 | 0.0169 |     - |     - |      72 B |
|    DirectAccessAsObject | .NET Core 3.1 |     4.823 ns |  0.2156 ns |  0.1283 ns |     4.34 |    0.23 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 3.1 |     4.491 ns |  0.4107 ns |  0.2716 ns |     4.06 |    0.36 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 3.1 |     1.316 ns |  0.0624 ns |  0.0277 ns |     1.17 |    0.04 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 3.1 |     2.741 ns |  0.0829 ns |  0.0368 ns |     2.44 |    0.07 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 3.1 |     1.071 ns |  0.0689 ns |  0.0179 ns |     0.94 |    0.04 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |     3.082 ns |  0.3447 ns |  0.2280 ns |     2.79 |    0.28 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |     1.006 ns |  0.0856 ns |  0.0222 ns |     0.89 |    0.04 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 3.1 |     2.234 ns |  0.0920 ns |  0.0608 ns |     2.02 |    0.11 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 3.1 |     1.160 ns |  0.0616 ns |  0.0408 ns |     1.05 |    0.06 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 3.1 |     2.408 ns |  0.2581 ns |  0.0670 ns |     2.12 |    0.12 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 3.1 |     1.162 ns |  0.1452 ns |  0.0960 ns |     1.05 |    0.09 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |     2.758 ns |  0.0887 ns |  0.0230 ns |     2.43 |    0.08 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |     1.069 ns |  0.0346 ns |  0.0090 ns |     0.94 |    0.04 |      - |     - |     - |         - |
