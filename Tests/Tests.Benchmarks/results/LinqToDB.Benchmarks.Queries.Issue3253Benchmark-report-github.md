``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.401
  [Host]     : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  Job-FSAWMY : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  Job-OSEEPF : .NET Core 3.1.19 (CoreCLR 4.700.21.41101, CoreFX 4.700.21.41603), X64 RyuJIT
  Job-VDKHPM : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                                               Method |              Runtime |            Mean |          Median |      Ratio |   Allocated |
|--------------------------------------------------------------------- |--------------------- |----------------:|----------------:|-----------:|------------:|
|                       Small_UpdateStatement_With_Variable_Parameters |             .NET 5.0 |    732,355.7 ns |    713,137.6 ns |   1,522.54 |    39,224 B |
|                 Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 5.0 |    814,671.3 ns |    815,098.0 ns |   1,687.28 |    40,208 B |
|                         Small_UpdateStatement_With_Static_Parameters |             .NET 5.0 |    686,532.1 ns |    692,365.2 ns |   1,419.64 |    35,392 B |
|                   Small_UpdateStatement_With_Static_Parameters_Async |             .NET 5.0 |    718,613.5 ns |    712,406.2 ns |   1,490.74 |    34,408 B |
|                        Large_UpdateStatement_With_Variable_Paramters |             .NET 5.0 |  1,350,726.6 ns |  1,345,233.5 ns |   2,805.97 |    89,296 B |
|                  Large_UpdateStatement_With_Variable_Paramters_Async |             .NET 5.0 |  1,375,022.4 ns |  1,403,162.2 ns |   2,842.84 |    88,312 B |
|                          Large_UpdateStatement_With_Static_Paramters |             .NET 5.0 |  1,221,298.1 ns |  1,227,035.5 ns |   2,529.65 |    77,856 B |
|                    Large_UpdateStatement_With_Static_Paramters_Async |             .NET 5.0 |  1,284,247.7 ns |  1,298,568.7 ns |   2,660.96 |    76,872 B |
|       Large_UpdateStatement_With_Variable_Paramters_With_ClearCaches |             .NET 5.0 | 86,240,888.2 ns | 86,251,175.4 ns | 178,891.10 |   914,360 B |
| Large_UpdateStatement_With_Variable_Paramters_With_ClearCaches_Async |             .NET 5.0 | 85,744,554.8 ns | 84,748,539.6 ns | 177,840.23 |   916,992 B |
|                                                            RawAdoNet |             .NET 5.0 |        307.7 ns |        307.6 ns |       0.64 |       360 B |
|                       Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    957,156.3 ns |    952,166.7 ns |   1,987.26 |    40,600 B |
|                 Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    237,715.9 ns |    241,239.1 ns |     493.36 |    37,288 B |
|                         Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    231,893.7 ns |    257,676.2 ns |     478.02 |    32,484 B |
|                   Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    239,540.5 ns |    242,261.5 ns |     498.37 |    31,531 B |
|                        Large_UpdateStatement_With_Variable_Paramters |        .NET Core 3.1 |    634,624.6 ns |    639,492.1 ns |   1,318.28 |    91,962 B |
|                  Large_UpdateStatement_With_Variable_Paramters_Async |        .NET Core 3.1 |    608,342.0 ns |    606,721.5 ns |   1,260.52 |    87,759 B |
|                          Large_UpdateStatement_With_Static_Paramters |        .NET Core 3.1 |    428,611.5 ns |    422,297.7 ns |     889.33 |    80,530 B |
|                    Large_UpdateStatement_With_Static_Paramters_Async |        .NET Core 3.1 |    396,928.7 ns |    396,582.0 ns |     823.24 |    76,328 B |
|       Large_UpdateStatement_With_Variable_Paramters_With_ClearCaches |        .NET Core 3.1 | 56,431,510.6 ns | 56,675,128.7 ns | 116,871.33 |   903,075 B |
| Large_UpdateStatement_With_Variable_Paramters_With_ClearCaches_Async |        .NET Core 3.1 | 64,442,767.8 ns | 63,719,867.6 ns | 133,558.10 |   904,761 B |
|                                                            RawAdoNet |        .NET Core 3.1 |        307.0 ns |        293.3 ns |       0.63 |       360 B |
|                       Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    834,522.8 ns |    810,416.9 ns |   1,731.65 |    49,152 B |
|                 Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    780,469.2 ns |    793,740.4 ns |   1,617.79 |    49,152 B |
|                         Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    436,552.4 ns |    413,107.8 ns |     903.89 |    40,960 B |
|                   Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    483,147.0 ns |    447,192.1 ns |     999.63 |    40,960 B |
|                        Large_UpdateStatement_With_Variable_Paramters | .NET Framework 4.7.2 |  1,075,307.3 ns |  1,000,294.3 ns |   2,210.10 |   106,496 B |
|                  Large_UpdateStatement_With_Variable_Paramters_Async | .NET Framework 4.7.2 |  1,115,011.3 ns |  1,037,158.1 ns |   2,309.89 |    98,304 B |
|                          Large_UpdateStatement_With_Static_Paramters | .NET Framework 4.7.2 |    777,609.1 ns |    713,283.9 ns |   1,612.20 |    90,112 B |
|                    Large_UpdateStatement_With_Static_Paramters_Async | .NET Framework 4.7.2 |    725,648.1 ns |    685,782.4 ns |   1,500.07 |    90,112 B |
|       Large_UpdateStatement_With_Variable_Paramters_With_ClearCaches | .NET Framework 4.7.2 | 87,752,508.2 ns | 86,179,203.4 ns | 182,235.06 | 1,015,808 B |
| Large_UpdateStatement_With_Variable_Paramters_With_ClearCaches_Async | .NET Framework 4.7.2 | 85,601,411.9 ns | 84,826,655.6 ns | 177,416.51 | 1,015,808 B |
|                                                            RawAdoNet | .NET Framework 4.7.2 |        484.1 ns |        485.9 ns |       1.00 |       417 B |
