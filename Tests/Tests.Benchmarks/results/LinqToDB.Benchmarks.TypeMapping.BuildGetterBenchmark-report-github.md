``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                  Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 | 26.2288 ns | 26.1116 ns | 28.86 | 0.0014 |      24 B |          NA |
|      DirectAccessAsEnum |             .NET 6.0 |  0.8990 ns |  0.9059 ns |  1.00 |      - |         - |          NA |
|      TypeMapperAsObject |             .NET 6.0 | 31.0338 ns | 31.7096 ns | 31.08 | 0.0029 |      48 B |          NA |
|    DirectAccessAsObject |             .NET 6.0 |  5.3830 ns |  5.3294 ns |  6.10 | 0.0014 |      24 B |          NA |
|     TypeMapperAsDecimal |             .NET 6.0 |  3.3261 ns |  3.3557 ns |  3.67 |      - |         - |          NA |
|   DirectAccessAsDecimal |             .NET 6.0 |  0.9270 ns |  0.9507 ns |  1.02 |      - |         - |          NA |
|     TypeMapperAsBoolean |             .NET 6.0 |  1.8374 ns |  1.8881 ns |  1.95 |      - |         - |          NA |
|   DirectAccessAsBoolean |             .NET 6.0 |  0.9173 ns |  0.9289 ns |  1.01 |      - |         - |          NA |
|      TypeMapperAsString |             .NET 6.0 |  4.7425 ns |  4.8094 ns |  5.23 |      - |         - |          NA |
|    DirectAccessAsString |             .NET 6.0 |  0.8465 ns |  0.8660 ns |  0.94 |      - |         - |          NA |
|         TypeMapperAsInt |             .NET 6.0 |  3.6561 ns |  3.6751 ns |  4.05 |      - |         - |          NA |
|       DirectAccessAsInt |             .NET 6.0 |  1.0073 ns |  0.9950 ns |  1.11 |      - |         - |          NA |
|        TypeMapperAsBool |             .NET 6.0 |  2.7480 ns |  2.7700 ns |  3.03 |      - |         - |          NA |
|      DirectAccessAsBool |             .NET 6.0 |  0.8961 ns |  0.8537 ns |  1.00 |      - |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.3757 ns |  2.4128 ns |  2.62 |      - |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.9096 ns |  0.9136 ns |  1.01 |      - |         - |          NA |
|        TypeMapperAsEnum |             .NET 7.0 | 12.1959 ns | 12.1863 ns | 13.45 |      - |         - |          NA |
|      DirectAccessAsEnum |             .NET 7.0 |  0.4881 ns |  0.4772 ns |  0.54 |      - |         - |          NA |
|      TypeMapperAsObject |             .NET 7.0 | 19.8113 ns | 19.8346 ns | 21.85 | 0.0014 |      24 B |          NA |
|    DirectAccessAsObject |             .NET 7.0 |  8.1466 ns |  8.1187 ns |  8.98 | 0.0014 |      24 B |          NA |
|     TypeMapperAsDecimal |             .NET 7.0 |  2.8302 ns |  2.8882 ns |  3.00 |      - |         - |          NA |
|   DirectAccessAsDecimal |             .NET 7.0 |  0.5000 ns |  0.5011 ns |  0.55 |      - |         - |          NA |
|     TypeMapperAsBoolean |             .NET 7.0 |  2.1172 ns |  2.1384 ns |  2.32 |      - |         - |          NA |
|   DirectAccessAsBoolean |             .NET 7.0 |  0.5703 ns |  0.5751 ns |  0.63 |      - |         - |          NA |
|      TypeMapperAsString |             .NET 7.0 |  1.8265 ns |  1.8399 ns |  2.00 |      - |         - |          NA |
|    DirectAccessAsString |             .NET 7.0 |  0.4061 ns |  0.3036 ns |  0.40 |      - |         - |          NA |
|         TypeMapperAsInt |             .NET 7.0 |  2.3864 ns |  2.3895 ns |  2.63 |      - |         - |          NA |
|       DirectAccessAsInt |             .NET 7.0 |  0.4101 ns |  0.4110 ns |  0.45 |      - |         - |          NA |
|        TypeMapperAsBool |             .NET 7.0 |  1.8166 ns |  1.8416 ns |  2.00 |      - |         - |          NA |
|      DirectAccessAsBool |             .NET 7.0 |  0.5754 ns |  0.5706 ns |  0.63 |      - |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  1.4982 ns |  1.5213 ns |  1.62 |      - |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.4594 ns |  0.4584 ns |  0.51 |      - |         - |          NA |
|        TypeMapperAsEnum |        .NET Core 3.1 | 30.6467 ns | 30.7145 ns | 33.77 | 0.0014 |      24 B |          NA |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.3020 ns |  1.2894 ns |  1.44 |      - |         - |          NA |
|      TypeMapperAsObject |        .NET Core 3.1 | 33.7733 ns | 34.3361 ns | 37.36 | 0.0029 |      48 B |          NA |
|    DirectAccessAsObject |        .NET Core 3.1 |  6.2640 ns |  6.2521 ns |  6.91 | 0.0014 |      24 B |          NA |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  3.8222 ns |  3.8174 ns |  4.22 |      - |         - |          NA |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  1.5122 ns |  1.5384 ns |  1.67 |      - |         - |          NA |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.2348 ns |  2.2666 ns |  2.46 |      - |         - |          NA |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  0.8590 ns |  0.8592 ns |  0.95 |      - |         - |          NA |
|      TypeMapperAsString |        .NET Core 3.1 |  2.4117 ns |  2.4771 ns |  2.66 |      - |         - |          NA |
|    DirectAccessAsString |        .NET Core 3.1 |  0.7207 ns |  0.9570 ns |  1.11 |      - |         - |          NA |
|         TypeMapperAsInt |        .NET Core 3.1 |  2.8304 ns |  2.8195 ns |  3.13 |      - |         - |          NA |
|       DirectAccessAsInt |        .NET Core 3.1 |  0.8859 ns |  0.9421 ns |  0.98 |      - |         - |          NA |
|        TypeMapperAsBool |        .NET Core 3.1 |  1.6813 ns |  1.7145 ns |  2.06 |      - |         - |          NA |
|      DirectAccessAsBool |        .NET Core 3.1 |  1.3093 ns |  1.3722 ns |  1.42 |      - |         - |          NA |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.3328 ns |  2.3595 ns |  2.57 |      - |         - |          NA |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.3535 ns |  1.4133 ns |  1.56 |      - |         - |          NA |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 52.0898 ns | 54.7706 ns | 60.37 | 0.0038 |      24 B |          NA |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  0.9089 ns |  0.9064 ns |  1.00 |      - |         - |          NA |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 59.2140 ns | 60.2715 ns | 62.08 | 0.0076 |      48 B |          NA |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  9.6550 ns |  9.6348 ns | 10.65 | 0.0038 |      24 B |          NA |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 | 11.3422 ns | 11.4242 ns | 12.42 |      - |         - |          NA |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  1.5086 ns |  1.5204 ns |  1.67 |      - |         - |          NA |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 | 10.2664 ns | 10.2237 ns | 11.32 |      - |         - |          NA |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  1.3041 ns |  1.3304 ns |  1.40 |      - |         - |          NA |
|      TypeMapperAsString | .NET Framework 4.7.2 |  9.9268 ns | 10.1030 ns | 10.94 |      - |         - |          NA |
|    DirectAccessAsString | .NET Framework 4.7.2 |  0.9278 ns |  0.9404 ns |  1.00 |      - |         - |          NA |
|         TypeMapperAsInt | .NET Framework 4.7.2 | 10.4832 ns | 10.4836 ns | 11.56 |      - |         - |          NA |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  1.3068 ns |  1.3415 ns |  1.30 |      - |         - |          NA |
|        TypeMapperAsBool | .NET Framework 4.7.2 | 10.1524 ns | 10.2012 ns | 11.20 |      - |         - |          NA |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  1.1987 ns |  1.1926 ns |  1.33 |      - |         - |          NA |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  9.8434 ns |  9.8767 ns | 10.92 |      - |         - |          NA |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  0.9397 ns |  0.9195 ns |  1.04 |      - |         - |          NA |
