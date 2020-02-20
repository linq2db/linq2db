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
|                  Method |       Runtime |          Mean |       Error |     StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |--------------:|------------:|-----------:|---------:|--------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 1,320.0282 ns | 106.4275 ns | 63.3333 ns |   663.74 |   32.18 | 0.0420 |     - |     - |     177 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |     1.9791 ns |   0.1219 ns |  0.0806 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|      TypeMapperAsObject |    .NET 4.6.2 | 2,347.2196 ns | 126.9143 ns | 66.3786 ns | 1,175.00 |   47.00 | 0.0458 |     - |     - |     201 B |
|    DirectAccessAsObject |    .NET 4.6.2 |     7.4424 ns |   0.5703 ns |  0.3394 ns |     3.75 |    0.24 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal |    .NET 4.6.2 |    17.0683 ns |   2.7355 ns |  1.8094 ns |     8.64 |    1.05 |      - |     - |     - |         - |
|   DirectAccessAsDecimal |    .NET 4.6.2 |     2.2188 ns |   0.2198 ns |  0.1454 ns |     1.12 |    0.08 |      - |     - |     - |         - |
|     TypeMapperAsBoolean |    .NET 4.6.2 |    11.8906 ns |   0.7298 ns |  0.4827 ns |     6.02 |    0.42 |      - |     - |     - |         - |
|   DirectAccessAsBoolean |    .NET 4.6.2 |     1.5890 ns |   0.2016 ns |  0.1334 ns |     0.80 |    0.08 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 |    10.2390 ns |   0.7103 ns |  0.4227 ns |     5.16 |    0.38 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |     1.6660 ns |   0.6043 ns |  0.3997 ns |     0.84 |    0.20 |      - |     - |     - |         - |
|         TypeMapperAsInt |    .NET 4.6.2 |    11.5575 ns |   0.9139 ns |  0.6045 ns |     5.85 |    0.48 |      - |     - |     - |         - |
|       DirectAccessAsInt |    .NET 4.6.2 |     1.6137 ns |   0.0780 ns |  0.0516 ns |     0.82 |    0.05 |      - |     - |     - |         - |
|        TypeMapperAsBool |    .NET 4.6.2 |    11.6869 ns |   1.5658 ns |  1.0357 ns |     5.91 |    0.55 |      - |     - |     - |         - |
|      DirectAccessAsBool |    .NET 4.6.2 |     1.5937 ns |   0.1685 ns |  0.1115 ns |     0.81 |    0.08 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |    10.7262 ns |   0.7533 ns |  0.4483 ns |     5.40 |    0.27 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |     1.5076 ns |   0.0605 ns |  0.0400 ns |     0.76 |    0.04 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 |   624.8125 ns | 111.8169 ns | 73.9599 ns |   316.55 |   43.42 | 0.0296 |     - |     - |     128 B |
|      DirectAccessAsEnum | .NET Core 2.1 |     1.4523 ns |   0.0616 ns |  0.0322 ns |     0.73 |    0.04 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 2.1 |   633.9959 ns |  17.0506 ns |  8.9178 ns |   317.48 |   13.40 | 0.0353 |     - |     - |     152 B |
|    DirectAccessAsObject | .NET Core 2.1 |     8.2007 ns |   1.2326 ns |  0.8153 ns |     4.15 |    0.47 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 2.1 |     4.7909 ns |   0.2565 ns |  0.1697 ns |     2.42 |    0.11 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 2.1 |     2.9252 ns |   0.1808 ns |  0.1196 ns |     1.48 |    0.08 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 2.1 |     4.0678 ns |   0.2239 ns |  0.1481 ns |     2.06 |    0.12 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 2.1 |     1.5252 ns |   0.1045 ns |  0.0691 ns |     0.77 |    0.06 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |     3.7159 ns |   0.4696 ns |  0.3106 ns |     1.88 |    0.13 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |     0.7331 ns |   0.5303 ns |  0.3507 ns |     0.37 |    0.17 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 2.1 |     3.5340 ns |   0.1872 ns |  0.1238 ns |     1.79 |    0.11 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 2.1 |     1.5616 ns |   0.0741 ns |  0.0264 ns |     0.78 |    0.04 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 2.1 |     3.3973 ns |   0.1092 ns |  0.0571 ns |     1.70 |    0.07 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 2.1 |     1.6271 ns |   0.2073 ns |  0.1371 ns |     0.82 |    0.07 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |     3.4076 ns |   0.1181 ns |  0.0781 ns |     1.72 |    0.07 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |     1.5093 ns |   0.1173 ns |  0.0305 ns |     0.76 |    0.03 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 |   215.9545 ns |   7.7961 ns |  5.1566 ns |   109.31 |    5.93 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 3.1 |     1.0397 ns |   0.0523 ns |  0.0232 ns |     0.52 |    0.03 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 3.1 |   227.9340 ns |  11.8106 ns |  7.8120 ns |   115.36 |    6.65 | 0.0172 |     - |     - |      72 B |
|    DirectAccessAsObject | .NET Core 3.1 |     5.4232 ns |   0.5822 ns |  0.3851 ns |     2.74 |    0.22 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 3.1 |     4.3172 ns |   0.1131 ns |  0.0748 ns |     2.19 |    0.12 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 3.1 |     1.8137 ns |   0.4702 ns |  0.3110 ns |     0.92 |    0.17 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 3.1 |     3.1301 ns |   1.1483 ns |  0.7595 ns |     1.58 |    0.37 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 3.1 |     1.7704 ns |   0.1727 ns |  0.1143 ns |     0.90 |    0.06 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |     3.2015 ns |   0.5629 ns |  0.3723 ns |     1.62 |    0.21 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |     1.3079 ns |   0.3788 ns |  0.2505 ns |     0.66 |    0.14 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 3.1 |     3.2580 ns |   0.2196 ns |  0.1453 ns |     1.65 |    0.05 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 3.1 |     1.5059 ns |   0.2130 ns |  0.1268 ns |     0.76 |    0.07 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 3.1 |     2.6662 ns |   0.2246 ns |  0.0583 ns |     1.34 |    0.05 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 3.1 |     1.5204 ns |   0.0870 ns |  0.0575 ns |     0.77 |    0.04 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |     2.4473 ns |   0.2298 ns |  0.1368 ns |     1.23 |    0.07 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |     1.1538 ns |   0.1082 ns |  0.0715 ns |     0.58 |    0.05 |      - |     - |     - |         - |
