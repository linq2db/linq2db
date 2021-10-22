``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                                                         Method |              Runtime |            Mean |          Median |      Ratio |   Allocated |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------------:|-----------:|------------:|
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 5.0 |    191,055.4 ns |    190,822.2 ns |     578.28 |    54,445 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 5.0 |    206,597.8 ns |    205,011.5 ns |     627.17 |    56,951 B |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 5.0 |    413,095.5 ns |    392,920.5 ns |   1,255.18 |    56,792 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 5.0 |    203,412.7 ns |    203,093.1 ns |     615.48 |    56,951 B |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 5.0 |    427,061.8 ns |    426,507.4 ns |   1,292.57 |   131,686 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 5.0 |    454,879.2 ns |    454,126.8 ns |   1,376.35 |   133,936 B |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 5.0 |    452,365.1 ns |    452,602.5 ns |   1,371.17 |   131,686 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 5.0 |    449,845.6 ns |    449,384.5 ns |   1,359.22 |   133,936 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 5.0 | 41,312,084.1 ns | 41,330,112.6 ns | 125,043.28 |   886,452 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 5.0 | 41,432,278.1 ns | 41,362,227.7 ns | 125,406.52 |   889,068 B |
|                                                                      RawAdoNet |             .NET 5.0 |        162.2 ns |        162.3 ns |       0.49 |       360 B |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 5.0 |    412,652.4 ns |    394,968.5 ns |   1,262.89 |    57,232 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 5.0 |    431,463.5 ns |    412,522.7 ns |   1,349.45 |    57,496 B |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 5.0 |    421,647.2 ns |    399,795.9 ns |   1,292.97 |    57,424 B |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 5.0 |    410,677.0 ns |    385,898.9 ns |   1,281.39 |    57,496 B |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 5.0 |    869,793.7 ns |    847,865.7 ns |   2,623.28 |   136,464 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 5.0 |    868,981.0 ns |    826,215.6 ns |   2,681.40 |   136,536 B |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 5.0 |    859,139.0 ns |    827,093.3 ns |   2,626.08 |   136,464 B |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 5.0 |    888,846.5 ns |    863,371.9 ns |   2,617.59 |   136,536 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 5.0 | 46,186,908.4 ns | 46,177,643.7 ns | 139,754.60 |   935,860 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 5.0 | 47,698,386.8 ns | 47,463,804.8 ns | 144,340.33 |   935,504 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 5.0 |     38,275.8 ns |     33,791.8 ns |     117.08 |     4,784 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 5.0 |    997,751.9 ns |    966,648.9 ns |   3,004.96 |   149,048 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 5.0 |  1,013,493.5 ns |  1,001,610.9 ns |   3,132.29 |   152,992 B |
|                                 Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    215,370.5 ns |    215,970.8 ns |     651.69 |    56,046 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    218,295.5 ns |    216,571.8 ns |     662.67 |    55,238 B |
|                                   Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    211,667.1 ns |    210,576.7 ns |     642.75 |    55,342 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    223,174.1 ns |    221,740.6 ns |     676.40 |    55,238 B |
|                                 Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    486,769.1 ns |    487,116.1 ns |   1,475.49 |   132,185 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    496,261.0 ns |    493,989.2 ns |   1,501.74 |   131,826 B |
|                                   Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    489,438.9 ns |    490,257.5 ns |   1,480.87 |   132,185 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    497,249.2 ns |    497,363.8 ns |   1,502.49 |   131,826 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 43,515,537.0 ns | 43,512,071.7 ns | 131,665.43 |   872,976 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 45,107,782.6 ns | 45,424,109.4 ns | 136,702.30 |   870,968 B |
|                                                                      RawAdoNet |        .NET Core 3.1 |        160.6 ns |        160.9 ns |       0.49 |       360 B |
|                                 Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    470,799.4 ns |    433,002.5 ns |   1,464.96 |    56,944 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    489,776.0 ns |    471,914.2 ns |   1,485.87 |    60,040 B |
|                                   Small_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    463,214.3 ns |    449,386.4 ns |   1,414.47 |    56,752 B |
|                             Small_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    489,252.7 ns |    464,014.9 ns |   1,504.01 |    59,848 B |
|                                 Large_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    991,755.0 ns |    984,641.9 ns |   3,012.32 |   135,904 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    994,162.6 ns |    964,454.6 ns |   3,088.49 |   139,000 B |
|                                   Large_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    991,182.5 ns |    954,946.1 ns |   3,030.61 |   135,904 B |
|                             Large_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    984,718.1 ns |    944,706.2 ns |   3,075.41 |   139,000 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 50,213,100.7 ns | 49,755,499.9 ns | 151,986.26 |   919,000 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 48,997,444.5 ns | 48,987,900.5 ns | 148,522.02 |   918,950 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |     44,402.3 ns |     37,156.3 ns |     146.21 |     4,752 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |        .NET Core 3.1 |  1,107,271.1 ns |  1,091,137.1 ns |   3,351.64 |   148,856 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |        .NET Core 3.1 |  1,133,752.1 ns |  1,113,518.6 ns |   3,526.36 |   159,016 B |
|                                 Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    269,977.6 ns |    265,926.0 ns |     820.07 |    61,542 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    433,242.4 ns |    396,577.6 ns |   1,341.46 |    73,728 B |
|                                   Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    264,488.4 ns |    264,504.3 ns |     800.28 |    59,994 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    432,399.6 ns |    415,448.4 ns |   1,376.78 |    73,728 B |
|                                 Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    785,285.5 ns |    754,243.6 ns |   2,434.28 |   155,648 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    825,832.8 ns |    803,980.4 ns |   2,479.92 |   155,648 B |
|                                   Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    849,086.3 ns |    840,551.5 ns |   2,609.70 |   155,648 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    804,694.2 ns |    761,850.4 ns |   2,464.09 |   155,648 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 57,761,014.2 ns | 57,647,555.9 ns | 174,761.97 |   991,232 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 57,292,922.9 ns | 57,393,605.7 ns | 173,333.00 |   999,424 B |
|                                                                      RawAdoNet | .NET Framework 4.7.2 |        329.1 ns |        327.6 ns |       1.00 |       417 B |
|                                 Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    253,394.1 ns |    253,008.1 ns |     766.71 |    60,874 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    258,218.0 ns |    257,791.8 ns |     785.38 |    60,614 B |
|                                   Small_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    256,403.4 ns |    253,257.4 ns |     778.62 |    61,873 B |
|                             Small_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    258,228.0 ns |    258,288.1 ns |     781.57 |    61,210 B |
|                                 Large_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    629,794.8 ns |    622,135.4 ns |   1,916.95 |   155,782 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    635,938.6 ns |    630,515.3 ns |   1,928.22 |   151,714 B |
|                                   Large_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    592,914.6 ns |    593,038.5 ns |   1,794.00 |   155,782 B |
|                             Large_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    595,881.9 ns |    594,931.0 ns |   1,803.65 |   151,714 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 64,980,498.0 ns | 64,219,575.9 ns | 197,262.53 | 1,051,683 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 62,805,115.6 ns | 62,794,178.4 ns | 190,029.55 | 1,039,370 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |      8,405.4 ns |      8,367.6 ns |      25.56 |     4,959 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload | .NET Framework 4.7.2 |    683,034.6 ns |    678,372.0 ns |   2,075.36 |   158,549 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    657,315.1 ns |    656,610.4 ns |   1,989.15 |   163,678 B |
