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
|              Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  5.1678 ns |  5.2938 ns |     ? |      - |         - |           ? |
|  DirectAccessString |             .NET 6.0 |  1.0185 ns |  1.0409 ns |     ? |      - |         - |           ? |
|       TypeMapperInt |             .NET 6.0 |  5.1634 ns |  5.1226 ns |     ? |      - |         - |           ? |
|     DirectAccessInt |             .NET 6.0 |  0.0000 ns |  0.0000 ns |     ? |      - |         - |           ? |
|      TypeMapperLong |             .NET 6.0 |  6.3603 ns |  6.3900 ns |     ? |      - |         - |           ? |
|    DirectAccessLong |             .NET 6.0 |  0.9404 ns |  0.9027 ns |     ? |      - |         - |           ? |
|   TypeMapperBoolean |             .NET 6.0 |  5.9895 ns |  6.0210 ns |     ? |      - |         - |           ? |
| DirectAccessBoolean |             .NET 6.0 |  1.2780 ns |  1.2776 ns |     ? |      - |         - |           ? |
|   TypeMapperWrapper |             .NET 6.0 | 13.7679 ns | 13.8268 ns |     ? |      - |         - |           ? |
| DirectAccessWrapper |             .NET 6.0 |  0.9109 ns |  0.9175 ns |     ? |      - |         - |           ? |
|      TypeMapperEnum |             .NET 6.0 | 27.3333 ns | 26.9555 ns |     ? | 0.0014 |      24 B |           ? |
|    DirectAccessEnum |             .NET 6.0 |  0.8808 ns |  0.8806 ns |     ? |      - |         - |           ? |
|   TypeMapperVersion |             .NET 6.0 |  7.5251 ns |  7.5245 ns |     ? |      - |         - |           ? |
| DirectAccessVersion |             .NET 6.0 |  0.6919 ns |  0.9142 ns |     ? |      - |         - |           ? |
|    TypeMapperString |             .NET 7.0 |  5.0632 ns |  5.0632 ns |     ? |      - |         - |           ? |
|  DirectAccessString |             .NET 7.0 |  0.4575 ns |  0.4575 ns |     ? |      - |         - |           ? |
|       TypeMapperInt |             .NET 7.0 |  4.9395 ns |  4.9820 ns |     ? |      - |         - |           ? |
|     DirectAccessInt |             .NET 7.0 |  0.4917 ns |  0.4344 ns |     ? |      - |         - |           ? |
|      TypeMapperLong |             .NET 7.0 |  5.0787 ns |  5.0783 ns |     ? |      - |         - |           ? |
|    DirectAccessLong |             .NET 7.0 |  0.4641 ns |  0.4768 ns |     ? |      - |         - |           ? |
|   TypeMapperBoolean |             .NET 7.0 |  2.2044 ns |  0.8613 ns |     ? |      - |         - |           ? |
| DirectAccessBoolean |             .NET 7.0 |  0.5561 ns |  0.5563 ns |     ? |      - |         - |           ? |
|   TypeMapperWrapper |             .NET 7.0 | 10.5963 ns | 10.4466 ns |     ? |      - |         - |           ? |
| DirectAccessWrapper |             .NET 7.0 |  0.5067 ns |  0.5489 ns |     ? |      - |         - |           ? |
|      TypeMapperEnum |             .NET 7.0 | 15.6817 ns | 15.5799 ns |     ? |      - |         - |           ? |
|    DirectAccessEnum |             .NET 7.0 |  0.0000 ns |  0.0000 ns |     ? |      - |         - |           ? |
|   TypeMapperVersion |             .NET 7.0 |  5.1099 ns |  5.1240 ns |     ? |      - |         - |           ? |
| DirectAccessVersion |             .NET 7.0 |  0.4373 ns |  0.4619 ns |     ? |      - |         - |           ? |
|    TypeMapperString |        .NET Core 3.1 |  4.9409 ns |  4.7969 ns |     ? |      - |         - |           ? |
|  DirectAccessString |        .NET Core 3.1 |  0.8213 ns |  0.8440 ns |     ? |      - |         - |           ? |
|       TypeMapperInt |        .NET Core 3.1 |  5.4991 ns |  5.4915 ns |     ? |      - |         - |           ? |
|     DirectAccessInt |        .NET Core 3.1 |  0.8737 ns |  0.9040 ns |     ? |      - |         - |           ? |
|      TypeMapperLong |        .NET Core 3.1 |  5.6529 ns |  5.7154 ns |     ? |      - |         - |           ? |
|    DirectAccessLong |        .NET Core 3.1 |  0.9032 ns |  0.9204 ns |     ? |      - |         - |           ? |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.0365 ns |  6.3772 ns |     ? |      - |         - |           ? |
| DirectAccessBoolean |        .NET Core 3.1 |  0.9729 ns |  0.9351 ns |     ? |      - |         - |           ? |
|   TypeMapperWrapper |        .NET Core 3.1 | 15.6587 ns | 15.7702 ns |     ? |      - |         - |           ? |
| DirectAccessWrapper |        .NET Core 3.1 |  0.9319 ns |  0.9880 ns |     ? |      - |         - |           ? |
|      TypeMapperEnum |        .NET Core 3.1 | 33.3857 ns | 33.4277 ns |     ? | 0.0014 |      24 B |           ? |
|    DirectAccessEnum |        .NET Core 3.1 |  0.9801 ns |  0.9453 ns |     ? |      - |         - |           ? |
|   TypeMapperVersion |        .NET Core 3.1 |  5.5533 ns |  5.4992 ns |     ? |      - |         - |           ? |
| DirectAccessVersion |        .NET Core 3.1 |  0.9726 ns |  1.0425 ns |     ? |      - |         - |           ? |
|    TypeMapperString | .NET Framework 4.7.2 | 23.6448 ns | 23.9032 ns |     ? |      - |         - |           ? |
|  DirectAccessString | .NET Framework 4.7.2 |  0.5802 ns |  0.0000 ns |     ? |      - |         - |           ? |
|       TypeMapperInt | .NET Framework 4.7.2 | 21.3663 ns | 20.5648 ns |     ? |      - |         - |           ? |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.4312 ns |  1.4312 ns |     ? |      - |         - |           ? |
|      TypeMapperLong | .NET Framework 4.7.2 | 22.8741 ns | 22.9566 ns |     ? |      - |         - |           ? |
|    DirectAccessLong | .NET Framework 4.7.2 |  0.8467 ns |  0.9153 ns |     ? |      - |         - |           ? |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 22.4274 ns | 22.2761 ns |     ? |      - |         - |           ? |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.8390 ns |  0.8399 ns |     ? |      - |         - |           ? |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 39.9790 ns | 42.0718 ns |     ? |      - |         - |           ? |
| DirectAccessWrapper | .NET Framework 4.7.2 |  1.1829 ns |  1.2931 ns |     ? |      - |         - |           ? |
|      TypeMapperEnum | .NET Framework 4.7.2 | 66.9765 ns | 66.8329 ns |     ? | 0.0038 |      24 B |           ? |
|    DirectAccessEnum | .NET Framework 4.7.2 |  0.9604 ns |  0.9782 ns |     ? |      - |         - |           ? |
|   TypeMapperVersion | .NET Framework 4.7.2 | 19.3962 ns | 23.6539 ns |     ? |      - |         - |           ? |
| DirectAccessVersion | .NET Framework 4.7.2 |  2.7968 ns |  4.4568 ns |     ? |      - |         - |           ? |
