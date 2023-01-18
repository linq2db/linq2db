``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                  Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 | 25.5412 ns | 25.5263 ns |     ? | 0.0014 |      24 B |           ? |
|      DirectAccessAsEnum |             .NET 6.0 |  0.8760 ns |  0.9130 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |             .NET 6.0 | 30.3354 ns | 30.7495 ns |     ? | 0.0029 |      48 B |           ? |
|    DirectAccessAsObject |             .NET 6.0 |  6.0984 ns |  6.1177 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |             .NET 6.0 |  3.3001 ns |  3.3524 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |             .NET 6.0 |  0.9464 ns |  0.9538 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |             .NET 6.0 |  1.7375 ns |  1.7380 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |             .NET 6.0 |  0.8745 ns |  0.8711 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |             .NET 6.0 |  2.2256 ns |  2.2253 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |             .NET 6.0 |  0.8554 ns |  0.8556 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |             .NET 6.0 |  1.8838 ns |  1.8839 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |             .NET 6.0 |  0.3969 ns |  0.4250 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |             .NET 6.0 |  2.2359 ns |  2.2155 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |             .NET 6.0 |  0.8688 ns |  0.8646 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  1.0407 ns |  1.0407 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.8869 ns |  0.9009 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum |             .NET 7.0 | 11.8211 ns | 11.8826 ns |     ? |      - |         - |           ? |
|      DirectAccessAsEnum |             .NET 7.0 |  0.4637 ns |  0.4637 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |             .NET 7.0 | 18.8257 ns | 18.6930 ns |     ? | 0.0014 |      24 B |           ? |
|    DirectAccessAsObject |             .NET 7.0 |  5.5730 ns |  7.4901 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |             .NET 7.0 |  2.8622 ns |  2.8975 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |             .NET 7.0 |  0.5327 ns |  0.4507 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |             .NET 7.0 |  2.1985 ns |  2.2302 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |             .NET 7.0 |  0.5629 ns |  0.5677 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |             .NET 7.0 |  1.8270 ns |  1.8270 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |             .NET 7.0 |  0.5124 ns |  0.4585 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |             .NET 7.0 |  2.3409 ns |  2.3409 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |             .NET 7.0 |  0.4072 ns |  0.4072 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |             .NET 7.0 |  1.7847 ns |  1.7787 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |             .NET 7.0 |  0.4761 ns |  0.5428 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  1.5747 ns |  1.5695 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.3959 ns |  0.3253 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum |        .NET Core 3.1 | 30.5418 ns | 30.1919 ns |     ? | 0.0014 |      24 B |           ? |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.2593 ns |  1.2588 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |        .NET Core 3.1 | 32.2889 ns | 31.8151 ns |     ? | 0.0029 |      48 B |           ? |
|    DirectAccessAsObject |        .NET Core 3.1 |  5.6086 ns |  5.5625 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  3.3604 ns |  3.3194 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  0.9415 ns |  0.9414 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.1739 ns |  2.1739 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  1.9299 ns |  2.2638 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |        .NET Core 3.1 |  2.0976 ns |  2.0919 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |        .NET Core 3.1 |  0.9707 ns |  0.9708 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |        .NET Core 3.1 |  2.3117 ns |  2.3116 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |        .NET Core 3.1 |  0.6534 ns |  0.8845 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |        .NET Core 3.1 |  2.2741 ns |  2.2948 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |        .NET Core 3.1 |  0.8679 ns |  0.8668 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.3554 ns |  2.3840 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.4233 ns |  1.3699 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 51.7610 ns | 51.8702 ns |     ? | 0.0038 |      24 B |           ? |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  0.0000 ns |  0.0000 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 60.4937 ns | 60.3936 ns |     ? | 0.0076 |      48 B |           ? |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  5.0013 ns |  4.7757 ns |     ? | 0.0038 |      24 B |           ? |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 | 10.5289 ns | 10.5285 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  0.7883 ns |  0.9415 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 |  9.4865 ns |  9.4864 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  0.8517 ns |  0.8502 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString | .NET Framework 4.7.2 |  9.8851 ns |  9.6972 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString | .NET Framework 4.7.2 |  0.5805 ns |  0.5426 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt | .NET Framework 4.7.2 |  9.6169 ns |  9.6170 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  0.9415 ns |  0.9415 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool | .NET Framework 4.7.2 |  9.4652 ns |  9.4654 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  0.8409 ns |  0.8409 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  7.4382 ns |  9.5881 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  0.8756 ns |  0.8497 ns |     ? |      - |         - |           ? |
