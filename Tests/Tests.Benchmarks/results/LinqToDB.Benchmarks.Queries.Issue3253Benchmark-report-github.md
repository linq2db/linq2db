``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.401
  [Host]     : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  Job-ONMFBC : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  Job-XWUAGQ : .NET Core 3.1.19 (CoreCLR 4.700.21.41101, CoreFX 4.700.21.41603), X64 RyuJIT
  Job-GIMZDR : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                                                Method |              Runtime |            Mean |          Median |      Ratio |   Allocated |
|---------------------------------------------------------------------- |--------------------- |----------------:|----------------:|-----------:|------------:|
|                        Small_UpdateStatement_With_Variable_Parameters |             .NET 5.0 |    263,068.7 ns |    256,774.0 ns |     716.47 |    61,064 B |
|                  Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 5.0 |    273,993.0 ns |    261,270.3 ns |     697.71 |    61,937 B |
|                          Small_UpdateStatement_With_Static_Parameters |             .NET 5.0 |    220,883.7 ns |    214,776.1 ns |     603.33 |    61,064 B |
|                    Small_UpdateStatement_With_Static_Parameters_Async |             .NET 5.0 |    214,208.2 ns |    213,024.3 ns |     567.25 |    61,937 B |
|                        Large_UpdateStatement_With_Variable_Parameters |             .NET 5.0 |    493,240.2 ns |    485,648.2 ns |   1,333.14 |   145,600 B |
|                  Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 5.0 |    481,205.3 ns |    478,242.5 ns |   1,275.02 |   146,728 B |
|                          Large_UpdateStatement_With_Static_Parameters |             .NET 5.0 |    674,952.8 ns |    717,920.1 ns |   1,379.80 |   145,600 B |
|                    Large_UpdateStatement_With_Static_Parameters_Async |             .NET 5.0 |    509,110.7 ns |    507,717.7 ns |   1,399.36 |   146,728 B |
|       Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 5.0 | 45,788,422.8 ns | 45,542,868.1 ns | 123,143.40 |   912,866 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 5.0 | 47,181,462.0 ns | 47,093,039.7 ns | 123,530.89 |   916,740 B |
|                                                             RawAdoNet |             .NET 5.0 |        175.7 ns |        176.0 ns |       0.47 |       360 B |
|                        Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    235,249.7 ns |    232,452.6 ns |     635.42 |    61,913 B |
|                  Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    365,384.8 ns |    380,735.2 ns |   1,034.83 |    62,001 B |
|                          Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    320,305.5 ns |    311,228.1 ns |     832.32 |    61,913 B |
|                    Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    255,344.9 ns |    251,007.8 ns |     699.71 |    62,001 B |
|                        Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    567,951.0 ns |    567,431.4 ns |   1,510.87 |   152,085 B |
|                  Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    567,977.4 ns |    560,053.6 ns |   1,548.41 |   148,923 B |
|                          Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    558,876.3 ns |    561,713.3 ns |   1,476.19 |   152,087 B |
|                    Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    567,380.6 ns |    560,030.4 ns |   1,570.11 |   148,923 B |
|       Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 60,768,335.2 ns | 54,165,060.0 ns | 159,989.21 |   905,180 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 50,141,901.6 ns | 49,609,361.5 ns | 135,255.87 |   905,844 B |
|                                                             RawAdoNet |        .NET Core 3.1 |        181.4 ns |        181.8 ns |       0.49 |       360 B |
|                        Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    521,186.0 ns |    467,233.1 ns |   1,245.24 |    73,728 B |
|                  Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    541,444.0 ns |    509,948.2 ns |   1,460.07 |    81,920 B |
|                          Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    435,140.8 ns |    436,220.8 ns |   1,164.38 |    73,728 B |
|                    Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    473,933.6 ns |    453,189.8 ns |   1,224.72 |    81,920 B |
|                        Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    930,563.2 ns |    914,864.1 ns |   2,453.57 |   163,840 B |
|                  Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    843,234.4 ns |    819,486.5 ns |   2,182.50 |   172,032 B |
|                          Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    810,357.7 ns |    818,023.7 ns |   2,170.10 |   163,840 B |
|                    Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    860,443.1 ns |    839,381.2 ns |   2,308.25 |   172,032 B |
|       Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 66,836,651.5 ns | 65,877,675.6 ns | 187,587.84 | 1,015,808 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 62,929,718.4 ns | 63,016,787.0 ns | 166,814.58 | 1,015,808 B |
|                                                             RawAdoNet | .NET Framework 4.7.2 |        373.7 ns |        371.0 ns |       1.00 |       417 B |
