``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                  Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 | 22.0031 ns | 21.8749 ns | 24.71 | 0.0014 |      24 B |          NA |
|      DirectAccessAsEnum |             .NET 6.0 |  0.9592 ns |  0.9782 ns |  1.09 |      - |         - |          NA |
|      TypeMapperAsObject |             .NET 6.0 | 29.2973 ns | 29.1242 ns | 33.14 | 0.0029 |      48 B |          NA |
|    DirectAccessAsObject |             .NET 6.0 |  6.0799 ns |  6.0034 ns |  6.88 | 0.0014 |      24 B |          NA |
|     TypeMapperAsDecimal |             .NET 6.0 |  3.2316 ns |  3.2318 ns |  3.66 |      - |         - |          NA |
|   DirectAccessAsDecimal |             .NET 6.0 |  1.0034 ns |  1.0078 ns |  1.13 |      - |         - |          NA |
|     TypeMapperAsBoolean |             .NET 6.0 |  1.9957 ns |  1.8905 ns |  2.25 |      - |         - |          NA |
|   DirectAccessAsBoolean |             .NET 6.0 |  0.9453 ns |  0.9552 ns |  1.07 |      - |         - |          NA |
|      TypeMapperAsString |             .NET 6.0 |  2.5005 ns |  2.4340 ns |  2.82 |      - |         - |          NA |
|    DirectAccessAsString |             .NET 6.0 |  0.7711 ns |  0.8252 ns |  0.91 |      - |         - |          NA |
|         TypeMapperAsInt |             .NET 6.0 |  1.8193 ns |  1.7771 ns |  2.06 |      - |         - |          NA |
|       DirectAccessAsInt |             .NET 6.0 |  0.2992 ns |  0.3154 ns |  0.34 |      - |         - |          NA |
|        TypeMapperAsBool |             .NET 6.0 |  2.3903 ns |  2.2918 ns |  2.70 |      - |         - |          NA |
|      DirectAccessAsBool |             .NET 6.0 |  0.8177 ns |  0.8197 ns |  0.93 |      - |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.3710 ns |  2.4024 ns |  2.69 |      - |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.9211 ns |  0.9255 ns |  1.04 |      - |         - |          NA |
|        TypeMapperAsEnum |             .NET 7.0 | 10.7264 ns | 10.6715 ns | 12.17 |      - |         - |          NA |
|      DirectAccessAsEnum |             .NET 7.0 |  0.4811 ns |  0.4183 ns |  0.56 |      - |         - |          NA |
|      TypeMapperAsObject |             .NET 7.0 | 18.7274 ns | 18.7280 ns | 21.24 | 0.0014 |      24 B |          NA |
|    DirectAccessAsObject |             .NET 7.0 |  7.4458 ns |  7.5390 ns |  8.33 | 0.0014 |      24 B |          NA |
|     TypeMapperAsDecimal |             .NET 7.0 |  3.1129 ns |  3.1870 ns |  3.47 |      - |         - |          NA |
|   DirectAccessAsDecimal |             .NET 7.0 |  0.4879 ns |  0.4876 ns |  0.55 |      - |         - |          NA |
|     TypeMapperAsBoolean |             .NET 7.0 |  2.3137 ns |  2.3114 ns |  2.62 |      - |         - |          NA |
|   DirectAccessAsBoolean |             .NET 7.0 |  0.5779 ns |  0.5754 ns |  0.66 |      - |         - |          NA |
|      TypeMapperAsString |             .NET 7.0 |  1.5973 ns |  1.9505 ns |  0.67 |      - |         - |          NA |
|    DirectAccessAsString |             .NET 7.0 |  0.4368 ns |  0.4687 ns |  0.50 |      - |         - |          NA |
|         TypeMapperAsInt |             .NET 7.0 |  2.0035 ns |  1.8928 ns |  2.25 |      - |         - |          NA |
|       DirectAccessAsInt |             .NET 7.0 |  0.4086 ns |  0.3648 ns |  0.47 |      - |         - |          NA |
|        TypeMapperAsBool |             .NET 7.0 |  5.3469 ns |  5.3714 ns |  6.05 |      - |         - |          NA |
|      DirectAccessAsBool |             .NET 7.0 |  0.3835 ns |  0.5447 ns |  0.20 |      - |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  1.5501 ns |  1.5771 ns |  1.74 |      - |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.6024 ns |  0.2132 ns |  1.01 |      - |         - |          NA |
|        TypeMapperAsEnum |        .NET Core 3.1 | 29.5602 ns | 29.5628 ns | 33.53 | 0.0014 |      24 B |          NA |
|      DirectAccessAsEnum |        .NET Core 3.1 |  0.9336 ns |  0.9153 ns |  1.06 |      - |         - |          NA |
|      TypeMapperAsObject |        .NET Core 3.1 | 36.1596 ns | 36.0603 ns | 40.90 | 0.0029 |      48 B |          NA |
|    DirectAccessAsObject |        .NET Core 3.1 |  5.1585 ns |  6.5722 ns |  4.99 | 0.0014 |      24 B |          NA |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  3.6886 ns |  3.6882 ns |  4.17 |      - |         - |          NA |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  0.9742 ns |  0.9434 ns |  1.10 |      - |         - |          NA |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.3137 ns |  2.1698 ns |  2.59 |      - |         - |          NA |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  0.8904 ns |  0.8912 ns |  1.01 |      - |         - |          NA |
|      TypeMapperAsString |        .NET Core 3.1 |  2.8781 ns |  2.8273 ns |  3.14 |      - |         - |          NA |
|    DirectAccessAsString |        .NET Core 3.1 |  2.5814 ns |  3.3475 ns |  1.52 |      - |         - |          NA |
|         TypeMapperAsInt |        .NET Core 3.1 |  2.3532 ns |  2.2886 ns |  2.69 |      - |         - |          NA |
|       DirectAccessAsInt |        .NET Core 3.1 |  1.4860 ns |  1.4568 ns |  1.68 |      - |         - |          NA |
|        TypeMapperAsBool |        .NET Core 3.1 |  1.4757 ns |  2.5656 ns |  2.71 |      - |         - |          NA |
|      DirectAccessAsBool |        .NET Core 3.1 |  0.8525 ns |  0.8624 ns |  0.97 |      - |         - |          NA |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.3519 ns |  2.3985 ns |  2.66 |      - |         - |          NA |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.4554 ns |  1.4742 ns |  1.63 |      - |         - |          NA |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 53.1958 ns | 53.1044 ns | 60.16 | 0.0038 |      24 B |          NA |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  0.8954 ns |  0.9132 ns |  1.00 |      - |         - |          NA |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 54.3107 ns | 60.3188 ns | 57.74 | 0.0076 |      48 B |          NA |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  5.7766 ns |  5.8368 ns |  6.54 | 0.0038 |      24 B |          NA |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 | 10.7533 ns | 10.8022 ns | 12.23 |      - |         - |          NA |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  0.8997 ns |  0.8815 ns |  0.98 |      - |         - |          NA |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 |  9.7303 ns |  9.7553 ns | 11.01 |      - |         - |          NA |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  0.8546 ns |  0.8572 ns |  0.97 |      - |         - |          NA |
|      TypeMapperAsString | .NET Framework 4.7.2 |  9.9579 ns | 10.0636 ns | 11.26 |      - |         - |          NA |
|    DirectAccessAsString | .NET Framework 4.7.2 |  0.9035 ns |  0.9129 ns |  1.02 |      - |         - |          NA |
|         TypeMapperAsInt | .NET Framework 4.7.2 |  9.7933 ns |  9.8957 ns | 10.99 |      - |         - |          NA |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  0.9086 ns |  0.9052 ns |  1.03 |      - |         - |          NA |
|        TypeMapperAsBool | .NET Framework 4.7.2 |  9.7152 ns |  9.7895 ns | 11.00 |      - |         - |          NA |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  0.8839 ns |  0.8907 ns |  1.00 |      - |         - |          NA |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  9.3589 ns |  9.8850 ns | 10.67 |      - |         - |          NA |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  0.9114 ns |  0.9157 ns |  1.03 |      - |         - |          NA |
