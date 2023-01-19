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
|              Method |              Runtime |       Mean |     Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|-------:|-------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  4.9569 ns |  4.9573 ns |  3.657 |      - |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  0.9002 ns |  0.9271 ns |  0.661 |      - |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  4.9010 ns |  5.0242 ns |  3.616 |      - |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.9135 ns |  0.9133 ns |  0.674 |      - |         - |          NA |
|      TypeMapperLong |             .NET 6.0 |  5.9520 ns |  5.9507 ns |  4.391 |      - |         - |          NA |
|    DirectAccessLong |             .NET 6.0 |  0.9598 ns |  0.9599 ns |  0.709 |      - |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  5.0746 ns |  5.0659 ns |  3.754 |      - |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.4189 ns |  0.4153 ns |  0.309 |      - |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 | 11.5500 ns | 11.3834 ns |  8.665 |      - |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  0.9131 ns |  0.9131 ns |  0.673 |      - |         - |          NA |
|      TypeMapperEnum |             .NET 6.0 | 26.7265 ns | 26.7897 ns | 19.720 | 0.0014 |      24 B |          NA |
|    DirectAccessEnum |             .NET 6.0 |  0.9133 ns |  0.9134 ns |  0.674 |      - |         - |          NA |
|   TypeMapperVersion |             .NET 6.0 |  5.9365 ns |  5.9369 ns |  4.380 |      - |         - |          NA |
| DirectAccessVersion |             .NET 6.0 |  0.9122 ns |  0.9123 ns |  0.672 |      - |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  5.0342 ns |  5.2713 ns |  2.709 |      - |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  0.0000 ns |  0.0000 ns |  0.000 |      - |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  5.1621 ns |  5.0752 ns |  3.809 |      - |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.4500 ns |  0.4495 ns |  0.332 |      - |         - |          NA |
|      TypeMapperLong |             .NET 7.0 |  4.5265 ns |  4.6253 ns |  3.460 |      - |         - |          NA |
|    DirectAccessLong |             .NET 7.0 |  0.4631 ns |  0.4710 ns |  0.344 |      - |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  4.8918 ns |  4.8908 ns |  3.609 |      - |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  0.5698 ns |  0.5517 ns |  0.415 |      - |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 | 11.8724 ns | 11.8722 ns |  8.749 |      - |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  0.3809 ns |  0.3305 ns |  0.280 |      - |         - |          NA |
|      TypeMapperEnum |             .NET 7.0 | 14.7242 ns | 14.8480 ns | 10.294 |      - |         - |          NA |
|    DirectAccessEnum |             .NET 7.0 |  0.1216 ns |  0.0000 ns |  0.000 |      - |         - |          NA |
|   TypeMapperVersion |             .NET 7.0 |  4.9749 ns |  5.0227 ns |  3.643 |      - |         - |          NA |
| DirectAccessVersion |             .NET 7.0 |  0.4237 ns |  0.4713 ns |  0.315 |      - |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  5.4323 ns |  5.1669 ns |  3.921 |      - |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  1.0427 ns |  1.0693 ns |  0.774 |      - |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  5.9657 ns |  5.9667 ns |  4.401 |      - |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  0.9416 ns |  0.9415 ns |  0.695 |      - |         - |          NA |
|      TypeMapperLong |        .NET Core 3.1 |  4.9654 ns |  4.9656 ns |  3.659 |      - |         - |          NA |
|    DirectAccessLong |        .NET Core 3.1 |  0.8566 ns |  0.8565 ns |  0.632 |      - |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  5.4360 ns |  5.4500 ns |  4.021 |      - |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  0.8316 ns |  0.8269 ns |  0.614 |      - |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 | 15.5465 ns | 15.6245 ns | 11.440 |      - |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  0.7544 ns |  0.7600 ns |  0.518 |      - |         - |          NA |
|      TypeMapperEnum |        .NET Core 3.1 | 30.1106 ns | 29.7752 ns | 22.297 | 0.0014 |      24 B |          NA |
|    DirectAccessEnum |        .NET Core 3.1 |  0.9439 ns |  1.1401 ns |  0.430 |      - |         - |          NA |
|   TypeMapperVersion |        .NET Core 3.1 |  5.6667 ns |  5.6667 ns |  4.181 |      - |         - |          NA |
| DirectAccessVersion |        .NET Core 3.1 |  0.9136 ns |  0.9134 ns |  0.674 |      - |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 23.9352 ns | 23.7217 ns | 17.578 |      - |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  1.3556 ns |  1.3504 ns |  1.000 |      - |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 22.3200 ns | 22.3190 ns | 16.467 |      - |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  0.0000 ns |  0.0000 ns |  0.000 |      - |         - |          NA |
|      TypeMapperLong | .NET Framework 4.7.2 | 15.7665 ns | 10.4112 ns | 13.592 |      - |         - |          NA |
|    DirectAccessLong | .NET Framework 4.7.2 |  0.9018 ns |  0.9069 ns |  0.665 |      - |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 20.4850 ns | 20.1283 ns | 15.206 |      - |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.8228 ns |  0.8329 ns |  0.611 |      - |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 37.8394 ns | 37.8374 ns | 27.917 |      - |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  1.4292 ns |  1.4753 ns |  1.047 |      - |         - |          NA |
|      TypeMapperEnum | .NET Framework 4.7.2 | 66.0169 ns | 66.1173 ns | 48.701 | 0.0038 |      24 B |          NA |
|    DirectAccessEnum | .NET Framework 4.7.2 |  0.9128 ns |  0.9128 ns |  0.673 |      - |         - |          NA |
|   TypeMapperVersion | .NET Framework 4.7.2 | 23.9243 ns | 24.0214 ns | 17.632 |      - |         - |          NA |
| DirectAccessVersion | .NET Framework 4.7.2 |  1.3270 ns |  1.4838 ns |  0.785 |      - |         - |          NA |
