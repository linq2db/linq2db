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
|                  Method |       Runtime |         Mean |       Error |     StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |-------------:|------------:|-----------:|---------:|--------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 1,384.613 ns |  97.5650 ns | 51.0284 ns | 1,089.41 |   87.67 | 0.0420 |     - |     - |     177 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |     1.273 ns |   0.1247 ns |  0.0825 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|      TypeMapperAsObject |    .NET 4.6.2 | 1,370.331 ns | 106.6670 ns | 70.5536 ns | 1,080.19 |   90.31 | 0.0477 |     - |     - |     201 B |
|    DirectAccessAsObject |    .NET 4.6.2 |     8.626 ns |   1.4476 ns |  0.9575 ns |     6.81 |    1.00 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal |    .NET 4.6.2 |    17.464 ns |   1.3479 ns |  0.8916 ns |    13.76 |    1.00 |      - |     - |     - |         - |
|   DirectAccessAsDecimal |    .NET 4.6.2 |     2.461 ns |   0.2921 ns |  0.1932 ns |     1.95 |    0.27 |      - |     - |     - |         - |
|     TypeMapperAsBoolean |    .NET 4.6.2 |    14.176 ns |   0.7884 ns |  0.5215 ns |    11.17 |    0.79 |      - |     - |     - |         - |
|   DirectAccessAsBoolean |    .NET 4.6.2 |     1.765 ns |   0.1111 ns |  0.0735 ns |     1.39 |    0.13 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 |    13.648 ns |   1.4580 ns |  0.9644 ns |    10.77 |    1.21 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |     1.936 ns |   0.2249 ns |  0.1488 ns |     1.53 |    0.18 |      - |     - |     - |         - |
|         TypeMapperAsInt |    .NET 4.6.2 |    15.947 ns |   1.8039 ns |  1.1932 ns |    12.58 |    1.42 |      - |     - |     - |         - |
|       DirectAccessAsInt |    .NET 4.6.2 |     1.703 ns |   0.2294 ns |  0.1517 ns |     1.34 |    0.14 |      - |     - |     - |         - |
|        TypeMapperAsBool |    .NET 4.6.2 |    11.210 ns |   0.5037 ns |  0.2997 ns |     8.89 |    0.72 |      - |     - |     - |         - |
|      DirectAccessAsBool |    .NET 4.6.2 |     1.564 ns |   0.1593 ns |  0.1054 ns |     1.23 |    0.12 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |    11.468 ns |   0.7755 ns |  0.5129 ns |     9.04 |    0.69 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |     1.553 ns |   0.1217 ns |  0.0805 ns |     1.22 |    0.09 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 |   669.150 ns |  61.8915 ns | 36.8306 ns |   530.16 |   43.60 | 0.0296 |     - |     - |     128 B |
|      DirectAccessAsEnum | .NET Core 2.1 |     1.554 ns |   0.1177 ns |  0.0779 ns |     1.23 |    0.10 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 2.1 |   697.225 ns |  27.1428 ns | 16.1523 ns |   552.77 |   41.85 | 0.0353 |     - |     - |     152 B |
|    DirectAccessAsObject | .NET Core 2.1 |     6.340 ns |   0.7997 ns |  0.4759 ns |     5.02 |    0.47 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 2.1 |     5.225 ns |   0.1230 ns |  0.0546 ns |     4.11 |    0.33 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 2.1 |     1.821 ns |   0.1071 ns |  0.0708 ns |     1.44 |    0.12 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 2.1 |     3.034 ns |   0.1795 ns |  0.1068 ns |     2.40 |    0.16 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 2.1 |     1.621 ns |   0.1349 ns |  0.0892 ns |     1.28 |    0.10 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |     2.467 ns |   0.5203 ns |  0.3441 ns |     1.94 |    0.28 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |     2.546 ns |   0.1468 ns |  0.0971 ns |     2.01 |    0.18 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 2.1 |     3.561 ns |   0.1758 ns |  0.1163 ns |     2.81 |    0.23 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 2.1 |     1.698 ns |   0.1228 ns |  0.0812 ns |     1.34 |    0.14 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 2.1 |     3.555 ns |   0.2603 ns |  0.1722 ns |     2.80 |    0.21 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 2.1 |     1.779 ns |   0.1635 ns |  0.1081 ns |     1.40 |    0.15 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |     3.254 ns |   0.2964 ns |  0.1961 ns |     2.57 |    0.27 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |     1.718 ns |   0.2110 ns |  0.1396 ns |     1.35 |    0.15 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 |   272.143 ns |  29.2554 ns | 19.3506 ns |   213.87 |   10.12 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 3.1 |     1.535 ns |   0.1286 ns |  0.0851 ns |     1.21 |    0.12 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 3.1 |   239.297 ns |  10.6954 ns |  6.3647 ns |   189.58 |   12.71 | 0.0172 |     - |     - |      72 B |
|    DirectAccessAsObject | .NET Core 3.1 |     6.494 ns |   0.8894 ns |  0.5883 ns |     5.12 |    0.57 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 3.1 |     5.339 ns |   0.6951 ns |  0.4598 ns |     4.21 |    0.51 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 3.1 |     1.669 ns |   0.0491 ns |  0.0175 ns |     1.31 |    0.11 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 3.1 |     2.670 ns |   0.1811 ns |  0.1198 ns |     2.10 |    0.14 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 3.1 |     1.194 ns |   0.0906 ns |  0.0599 ns |     0.94 |    0.08 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |     2.687 ns |   0.4201 ns |  0.2779 ns |     2.12 |    0.23 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |     1.140 ns |   0.1689 ns |  0.1117 ns |     0.90 |    0.11 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 3.1 |     2.706 ns |   0.2073 ns |  0.1371 ns |     2.13 |    0.19 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 3.1 |     1.205 ns |   0.1267 ns |  0.0838 ns |     0.95 |    0.09 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 3.1 |     3.267 ns |   0.8124 ns |  0.5374 ns |     2.58 |    0.47 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 3.1 |     1.373 ns |   0.1609 ns |  0.1065 ns |     1.08 |    0.08 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |    21.189 ns |   0.4282 ns |  0.2833 ns |    16.71 |    1.29 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |     1.698 ns |   0.1545 ns |  0.1022 ns |     1.34 |    0.13 |      - |     - |     - |         - |
