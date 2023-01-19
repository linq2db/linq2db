``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                  Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 | 24.9620 ns | 25.0276 ns |     ? | 0.0014 |      24 B |           ? |
|      DirectAccessAsEnum |             .NET 6.0 |  0.9143 ns |  0.9155 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |             .NET 6.0 | 28.3209 ns | 30.5468 ns |     ? | 0.0029 |      48 B |           ? |
|    DirectAccessAsObject |             .NET 6.0 |  6.0890 ns |  6.1301 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |             .NET 6.0 |  2.7987 ns |  2.7314 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |             .NET 6.0 |  0.9540 ns |  0.9569 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |             .NET 6.0 |  1.8213 ns |  1.8440 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |             .NET 6.0 |  0.8124 ns |  0.8440 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |             .NET 6.0 |  2.3476 ns |  2.3527 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |             .NET 6.0 |  1.0103 ns |  1.0286 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |             .NET 6.0 |  2.3599 ns |  2.3399 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |             .NET 6.0 |  0.8736 ns |  0.8784 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |             .NET 6.0 |  2.1914 ns |  2.1426 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |             .NET 6.0 |  0.7811 ns |  0.7949 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.7920 ns |  2.8166 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.8972 ns |  0.9064 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum |             .NET 7.0 | 12.1420 ns | 12.1645 ns |     ? |      - |         - |           ? |
|      DirectAccessAsEnum |             .NET 7.0 |  0.3316 ns |  0.4654 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |             .NET 7.0 | 19.2059 ns | 19.1787 ns |     ? | 0.0014 |      24 B |           ? |
|    DirectAccessAsObject |             .NET 7.0 |  7.7217 ns |  7.7432 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |             .NET 7.0 |  3.2688 ns |  3.2219 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |             .NET 7.0 |  0.7961 ns |  0.8062 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |             .NET 7.0 |  1.6983 ns |  1.6460 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |             .NET 7.0 |  0.6274 ns |  0.6355 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |             .NET 7.0 |  1.5860 ns |  1.6022 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |             .NET 7.0 |  0.4317 ns |  0.4711 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |             .NET 7.0 |  1.9529 ns |  1.9518 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |             .NET 7.0 |  0.3870 ns |  0.4071 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |             .NET 7.0 |  1.6652 ns |  1.7041 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |             .NET 7.0 |  0.5767 ns |  0.5745 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  1.5683 ns |  1.5825 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.4485 ns |  0.4621 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum |        .NET Core 3.1 | 28.9525 ns | 28.6446 ns |     ? | 0.0014 |      24 B |           ? |
|      DirectAccessAsEnum |        .NET Core 3.1 |  0.8967 ns |  0.8688 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |        .NET Core 3.1 | 35.6595 ns | 35.6308 ns |     ? | 0.0029 |      48 B |           ? |
|    DirectAccessAsObject |        .NET Core 3.1 |  9.0513 ns |  9.6793 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  4.3816 ns |  5.0218 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  1.4949 ns |  1.5260 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.4863 ns |  2.7242 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  0.8594 ns |  0.8608 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |        .NET Core 3.1 |  2.4473 ns |  2.3405 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |        .NET Core 3.1 |  0.8335 ns |  0.8677 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |        .NET Core 3.1 |  2.3831 ns |  2.3980 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |        .NET Core 3.1 |  1.4245 ns |  1.4444 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |        .NET Core 3.1 |  2.2330 ns |  2.2753 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |        .NET Core 3.1 |  0.8395 ns |  0.8503 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  3.3675 ns |  3.6063 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  0.8662 ns |  0.8376 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 52.8534 ns | 52.5485 ns |     ? | 0.0038 |      24 B |           ? |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.2922 ns |  1.3721 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 59.7602 ns | 58.9530 ns |     ? | 0.0076 |      48 B |           ? |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  6.4140 ns |  6.4206 ns |     ? | 0.0038 |      24 B |           ? |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 |  9.5490 ns |  9.4460 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  0.9500 ns |  0.9697 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 |  9.5227 ns |  9.4755 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  0.9011 ns |  0.8941 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString | .NET Framework 4.7.2 |  8.4307 ns |  8.5077 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString | .NET Framework 4.7.2 |  0.9826 ns |  0.9408 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt | .NET Framework 4.7.2 |  9.6807 ns |  9.5902 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  1.8336 ns |  1.8509 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool | .NET Framework 4.7.2 |  9.6821 ns |  9.7240 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  0.7909 ns |  0.8169 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 | 10.1658 ns | 10.0455 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.3698 ns |  1.3698 ns |     ? |      - |         - |           ? |
