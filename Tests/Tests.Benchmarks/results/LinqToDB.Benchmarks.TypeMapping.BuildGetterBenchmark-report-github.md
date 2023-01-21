``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                  Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 | 30.1809 ns | 30.8091 ns |     ? | 0.0014 |      24 B |           ? |
|      DirectAccessAsEnum |             .NET 6.0 |  0.7368 ns |  0.4928 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |             .NET 6.0 | 37.7463 ns | 36.9660 ns |     ? | 0.0029 |      48 B |           ? |
|    DirectAccessAsObject |             .NET 6.0 |  7.7571 ns |  7.9357 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |             .NET 6.0 |  3.0096 ns |  3.2395 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |             .NET 6.0 |  2.5432 ns |  2.7418 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |             .NET 6.0 |  3.1496 ns |  3.5536 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |             .NET 6.0 |  2.5613 ns |  3.0161 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |             .NET 6.0 |  4.5572 ns |  5.2477 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |             .NET 6.0 |  0.6219 ns |  0.0000 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |             .NET 6.0 |  2.8909 ns |  3.2131 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |             .NET 6.0 |  0.8796 ns |  1.0512 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |             .NET 6.0 |  2.7761 ns |  3.0648 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |             .NET 6.0 |  0.7734 ns |  0.9246 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  3.2164 ns |  3.6659 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 6.0 |  1.0923 ns |  1.1353 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum |             .NET 7.0 | 13.2115 ns | 13.7578 ns |     ? |      - |         - |           ? |
|      DirectAccessAsEnum |             .NET 7.0 |  0.3341 ns |  0.3311 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |             .NET 7.0 | 23.6336 ns | 25.1119 ns |     ? | 0.0014 |      24 B |           ? |
|    DirectAccessAsObject |             .NET 7.0 | 11.3661 ns | 11.8769 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |             .NET 7.0 |  2.9090 ns |  3.4798 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |             .NET 7.0 |  0.6094 ns |  0.7815 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |             .NET 7.0 |  3.4579 ns |  3.7285 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |             .NET 7.0 |  1.1114 ns |  1.3057 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |             .NET 7.0 |  1.7600 ns |  1.5412 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |             .NET 7.0 |  0.5749 ns |  0.5851 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |             .NET 7.0 |  2.9922 ns |  3.1783 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |             .NET 7.0 |  0.4998 ns |  0.5709 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |             .NET 7.0 |  2.5427 ns |  2.7755 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |             .NET 7.0 |  0.7510 ns |  0.8233 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  2.3461 ns |  2.6233 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.5252 ns |  0.5445 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum |        .NET Core 3.1 | 35.5791 ns | 37.6307 ns |     ? | 0.0014 |      24 B |           ? |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.6122 ns |  1.7984 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject |        .NET Core 3.1 | 42.6299 ns | 43.9497 ns |     ? | 0.0029 |      48 B |           ? |
|    DirectAccessAsObject |        .NET Core 3.1 |  9.4285 ns | 10.1790 ns |     ? | 0.0014 |      24 B |           ? |
|     TypeMapperAsDecimal |        .NET Core 3.1 |  3.9229 ns |  4.1449 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal |        .NET Core 3.1 |  1.9338 ns |  2.1650 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean |        .NET Core 3.1 |  2.8511 ns |  3.1124 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean |        .NET Core 3.1 |  1.6229 ns |  1.8775 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString |        .NET Core 3.1 |  2.9358 ns |  3.2583 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString |        .NET Core 3.1 |  1.5726 ns |  1.8826 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt |        .NET Core 3.1 |  3.6132 ns |  4.0262 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt |        .NET Core 3.1 |  0.7629 ns |  0.6925 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool |        .NET Core 3.1 |  2.4745 ns |  2.8535 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool |        .NET Core 3.1 |  1.2989 ns |  1.4258 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.4085 ns |  2.5947 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.3072 ns |  1.5001 ns |     ? |      - |         - |           ? |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 58.9375 ns | 62.2218 ns |     ? | 0.0038 |      24 B |           ? |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  0.7895 ns |  1.0135 ns |     ? |      - |         - |           ? |
|      TypeMapperAsObject | .NET Framework 4.7.2 | 64.6615 ns | 68.3523 ns |     ? | 0.0076 |      48 B |           ? |
|    DirectAccessAsObject | .NET Framework 4.7.2 |  7.1220 ns |  7.8332 ns |     ? | 0.0038 |      24 B |           ? |
|     TypeMapperAsDecimal | .NET Framework 4.7.2 | 12.1523 ns | 13.0801 ns |     ? |      - |         - |           ? |
|   DirectAccessAsDecimal | .NET Framework 4.7.2 |  1.5637 ns |  1.6654 ns |     ? |      - |         - |           ? |
|     TypeMapperAsBoolean | .NET Framework 4.7.2 | 10.6959 ns | 11.4364 ns |     ? |      - |         - |           ? |
|   DirectAccessAsBoolean | .NET Framework 4.7.2 |  2.0928 ns |  2.3084 ns |     ? |      - |         - |           ? |
|      TypeMapperAsString | .NET Framework 4.7.2 |  9.8097 ns | 10.4498 ns |     ? |      - |         - |           ? |
|    DirectAccessAsString | .NET Framework 4.7.2 |  0.8746 ns |  0.9585 ns |     ? |      - |         - |           ? |
|         TypeMapperAsInt | .NET Framework 4.7.2 | 11.3317 ns | 12.3884 ns |     ? |      - |         - |           ? |
|       DirectAccessAsInt | .NET Framework 4.7.2 |  1.6455 ns |  1.8555 ns |     ? |      - |         - |           ? |
|        TypeMapperAsBool | .NET Framework 4.7.2 | 12.9905 ns | 13.7448 ns |     ? |      - |         - |           ? |
|      DirectAccessAsBool | .NET Framework 4.7.2 |  1.5016 ns |  1.7067 ns |     ? |      - |         - |           ? |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  9.2350 ns |  9.6777 ns |     ? |      - |         - |           ? |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.4775 ns |  1.4920 ns |     ? |      - |         - |           ? |
