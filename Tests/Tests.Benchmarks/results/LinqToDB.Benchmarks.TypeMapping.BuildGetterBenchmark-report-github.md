``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                  Method |              Runtime |       Mean | Allocated |
|------------------------ |--------------------- |-----------:|----------:|
|        TypeMapperAsEnum |             .NET 6.0 | 24.6610 ns |      24 B |
|      DirectAccessAsEnum |             .NET 6.0 |  0.8984 ns |         - |
|      TypeMapperAsObject |             .NET 6.0 | 29.2023 ns |      48 B |
|    DirectAccessAsObject |             .NET 6.0 |  6.2905 ns |      24 B |
|     TypeMapperAsDecimal |             .NET 6.0 |  3.3117 ns |         - |
|   DirectAccessAsDecimal |             .NET 6.0 |  0.9488 ns |         - |
|     TypeMapperAsBoolean |             .NET 6.0 |  1.8190 ns |         - |
|   DirectAccessAsBoolean |             .NET 6.0 |  0.8033 ns |         - |
|      TypeMapperAsString |             .NET 6.0 |  1.4560 ns |         - |
|    DirectAccessAsString |             .NET 6.0 |  1.0671 ns |         - |
|         TypeMapperAsInt |             .NET 6.0 |  2.4700 ns |         - |
|       DirectAccessAsInt |             .NET 6.0 |  0.8364 ns |         - |
|        TypeMapperAsBool |             .NET 6.0 |  1.9025 ns |         - |
|      DirectAccessAsBool |             .NET 6.0 |  0.8764 ns |         - |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.1223 ns |         - |
| DirectAccessAsKnownEnum |             .NET 6.0 |  1.3265 ns |         - |
|        TypeMapperAsEnum |             .NET 7.0 | 11.1268 ns |         - |
|      DirectAccessAsEnum |             .NET 7.0 |  0.4559 ns |         - |
|      TypeMapperAsObject |             .NET 7.0 | 14.7813 ns |      24 B |
|    DirectAccessAsObject |             .NET 7.0 |  8.1631 ns |      24 B |
|     TypeMapperAsDecimal |             .NET 7.0 |  3.1256 ns |         - |
|   DirectAccessAsDecimal |             .NET 7.0 |  0.4994 ns |         - |
|     TypeMapperAsBoolean |             .NET 7.0 | 18.9771 ns |         - |
|   DirectAccessAsBoolean |             .NET 7.0 |  1.8005 ns |         - |
|      TypeMapperAsString |             .NET 7.0 |  1.8164 ns |         - |
|    DirectAccessAsString |             .NET 7.0 |  0.4574 ns |         - |
|         TypeMapperAsInt |             .NET 7.0 |  2.4124 ns |         - |
|       DirectAccessAsInt |             .NET 7.0 |  0.3872 ns |         - |
|        TypeMapperAsBool |             .NET 7.0 |  1.6065 ns |         - |
|      DirectAccessAsBool |             .NET 7.0 |  0.5738 ns |         - |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  1.5668 ns |         - |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.2581 ns |         - |
|        TypeMapperAsEnum |        .NET Core 3.1 | 30.7396 ns |      24 B |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.4153 ns |         - |
|      TypeMapperAsObject |        .NET Core 3.1 | 37.6064 ns |      48 B |
|    DirectAccessAsObject |        .NET Core 3.1 |  6.1968 ns |      24 B |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  3.7343 ns |         - |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  0.8509 ns |         - |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.3038 ns |         - |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  0.8511 ns |         - |
|      TypeMapperAsString |        .NET Core 3.1 |  2.3842 ns |         - |
|    DirectAccessAsString |        .NET Core 3.1 |  1.3828 ns |         - |
|         TypeMapperAsInt |        .NET Core 3.1 |  1.4911 ns |         - |
|       DirectAccessAsInt |        .NET Core 3.1 |  0.9473 ns |         - |
|        TypeMapperAsBool |        .NET Core 3.1 |  1.0093 ns |         - |
|      DirectAccessAsBool |        .NET Core 3.1 |  1.2899 ns |         - |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  1.9768 ns |         - |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  0.9080 ns |         - |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 50.4339 ns |      24 B |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  0.8029 ns |         - |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 60.2975 ns |      48 B |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  6.4113 ns |      24 B |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 | 10.7608 ns |         - |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  1.4120 ns |         - |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 | 10.1696 ns |         - |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  1.3485 ns |         - |
|      TypeMapperAsString | .NET Framework 4.7.2 | 10.0949 ns |         - |
|    DirectAccessAsString | .NET Framework 4.7.2 |  0.7526 ns |         - |
|         TypeMapperAsInt | .NET Framework 4.7.2 | 10.3912 ns |         - |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  1.3166 ns |         - |
|        TypeMapperAsBool | .NET Framework 4.7.2 | 10.0563 ns |         - |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  1.2848 ns |         - |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  9.9364 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  0.8955 ns |         - |
