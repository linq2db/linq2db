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
|                  Method |       Runtime |       Mean |      Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |-----------:|-----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 61.8722 ns | 10.8274 ns | 2.8118 ns | 53.92 |    2.70 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |  1.1444 ns |  0.0531 ns | 0.0082 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      TypeMapperAsObject |    .NET 4.6.2 | 60.2117 ns | 23.6028 ns | 6.1296 ns | 53.65 |    5.24 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject |    .NET 4.6.2 |  6.0588 ns |  3.0550 ns | 0.7934 ns |  5.15 |    0.73 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal |    .NET 4.6.2 | 15.5383 ns |  1.8821 ns | 0.4888 ns | 13.57 |    0.51 |      - |     - |     - |         - |
|   DirectAccessAsDecimal |    .NET 4.6.2 |  1.8350 ns |  0.4561 ns | 0.1185 ns |  1.62 |    0.10 |      - |     - |     - |         - |
|     TypeMapperAsBoolean |    .NET 4.6.2 | 12.7507 ns |  4.9602 ns | 1.2882 ns | 11.61 |    0.41 |      - |     - |     - |         - |
|   DirectAccessAsBoolean |    .NET 4.6.2 |  1.5006 ns |  0.2311 ns | 0.0600 ns |  1.30 |    0.05 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 | 14.8691 ns |  4.2565 ns | 1.1054 ns | 13.18 |    1.00 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |  0.9525 ns |  0.9306 ns | 0.2417 ns |  0.87 |    0.22 |      - |     - |     - |         - |
|         TypeMapperAsInt |    .NET 4.6.2 | 15.6774 ns |  2.5981 ns | 0.6747 ns | 13.49 |    0.40 |      - |     - |     - |         - |
|       DirectAccessAsInt |    .NET 4.6.2 |  1.6965 ns |  0.3888 ns | 0.1010 ns |  1.46 |    0.09 |      - |     - |     - |         - |
|        TypeMapperAsBool |    .NET 4.6.2 | 16.1736 ns |  2.7205 ns | 0.7065 ns | 14.26 |    0.72 |      - |     - |     - |         - |
|      DirectAccessAsBool |    .NET 4.6.2 |  1.5786 ns |  0.3655 ns | 0.0949 ns |  1.39 |    0.09 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 | 13.9412 ns |  1.7360 ns | 0.4508 ns | 12.32 |    0.29 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |  1.0230 ns |  0.4007 ns | 0.1041 ns |  0.91 |    0.09 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 | 31.7324 ns |  2.5745 ns | 0.6686 ns | 27.91 |    0.48 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum | .NET Core 2.1 |  1.3614 ns |  0.0496 ns | 0.0077 ns |  1.19 |    0.01 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 2.1 | 36.3243 ns |  2.4354 ns | 0.6325 ns | 31.78 |    0.53 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject | .NET Core 2.1 |  4.9727 ns |  0.5708 ns | 0.1482 ns |  4.33 |    0.14 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 2.1 |  4.7189 ns |  0.7208 ns | 0.1872 ns |  4.17 |    0.14 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 2.1 |  1.5107 ns |  0.0580 ns | 0.0090 ns |  1.32 |    0.01 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 2.1 |  2.9885 ns |  0.0605 ns | 0.0157 ns |  2.61 |    0.02 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 2.1 |  1.3465 ns |  0.0471 ns | 0.0026 ns |  1.18 |    0.01 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |  3.0406 ns |  0.1102 ns | 0.0286 ns |  2.66 |    0.05 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |  1.2349 ns |  0.2195 ns | 0.0570 ns |  1.09 |    0.05 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 2.1 |  2.9798 ns |  0.0509 ns | 0.0079 ns |  2.60 |    0.02 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 2.1 |  1.4287 ns |  0.0419 ns | 0.0109 ns |  1.25 |    0.01 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 2.1 |  2.9585 ns |  0.0778 ns | 0.0043 ns |  2.59 |    0.02 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 2.1 |  1.3500 ns |  0.0250 ns | 0.0039 ns |  1.18 |    0.01 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |  2.9884 ns |  0.0673 ns | 0.0104 ns |  2.61 |    0.01 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |  1.3132 ns |  0.0396 ns | 0.0061 ns |  1.15 |    0.01 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 | 26.5908 ns |  0.5184 ns | 0.1346 ns | 23.25 |    0.15 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum | .NET Core 3.1 |  1.0630 ns |  0.0357 ns | 0.0055 ns |  0.93 |    0.01 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 3.1 | 31.4937 ns |  0.7568 ns | 0.1965 ns | 27.53 |    0.34 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject | .NET Core 3.1 |  4.6631 ns |  0.1449 ns | 0.0224 ns |  4.07 |    0.04 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 3.1 |  4.1475 ns |  0.2230 ns | 0.0579 ns |  3.62 |    0.08 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 3.1 |  1.3559 ns |  0.0578 ns | 0.0090 ns |  1.18 |    0.02 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 3.1 |  2.4630 ns |  0.0853 ns | 0.0132 ns |  2.15 |    0.01 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 3.1 |  1.3706 ns |  0.0468 ns | 0.0121 ns |  1.20 |    0.02 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |  2.4301 ns |  0.0486 ns | 0.0075 ns |  2.12 |    0.02 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |  1.5246 ns |  0.5378 ns | 0.1397 ns |  1.33 |    0.14 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 3.1 |  2.5532 ns |  0.2210 ns | 0.0574 ns |  2.24 |    0.04 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 3.1 |  1.3467 ns |  0.0289 ns | 0.0016 ns |  1.18 |    0.01 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 3.1 |  2.4564 ns |  0.0861 ns | 0.0133 ns |  2.15 |    0.01 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 3.1 |  1.3510 ns |  0.0246 ns | 0.0038 ns |  1.18 |    0.01 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |  2.1363 ns |  0.0279 ns | 0.0043 ns |  1.87 |    0.01 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |  1.0732 ns |  0.0420 ns | 0.0065 ns |  0.94 |    0.01 |      - |     - |     - |         - |
