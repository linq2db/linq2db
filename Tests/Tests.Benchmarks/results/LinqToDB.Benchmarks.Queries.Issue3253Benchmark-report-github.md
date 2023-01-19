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
|                                                                         Method |              Runtime |            Mean |          Median |      Ratio |     Gen0 | Allocated | Alloc Ratio |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------------:|-----------:|---------:|----------:|------------:|
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    139,338.6 ns |    140,387.3 ns |     603.20 |   2.1973 |   38400 B |       92.09 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    131,741.0 ns |    148,601.0 ns |     400.92 |   2.1973 |   39801 B |       95.45 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    137,943.9 ns |    139,335.4 ns |     497.01 |   2.1973 |   38400 B |       92.09 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    149,159.0 ns |    148,138.1 ns |     645.96 |   2.1973 |   39032 B |       93.60 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    321,374.7 ns |    321,295.3 ns |   1,345.60 |   5.3711 |   90401 B |      216.79 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    349,483.9 ns |    351,631.7 ns |   1,511.78 |   5.3711 |   91689 B |      219.88 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    290,277.4 ns |    295,872.1 ns |     925.68 |   5.3711 |   90401 B |      216.79 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    347,355.7 ns |    349,028.8 ns |   1,503.69 |   5.3711 |   91785 B |      220.11 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 63,435,527.3 ns | 63,385,750.0 ns | 278,204.48 |        - |  805367 B |    1,931.34 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 62,503,490.0 ns | 61,998,075.0 ns | 270,231.59 |        - |  806737 B |    1,934.62 |
|                                                                      RawAdoNet |             .NET 6.0 |        163.1 ns |        162.7 ns |       0.70 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    108,805.2 ns |    111,747.5 ns |     354.30 |   1.9531 |   33952 B |       81.42 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    127,223.7 ns |    128,115.6 ns |     549.97 |   1.9531 |   34136 B |       81.86 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 6.0 |    128,667.2 ns |    129,295.2 ns |     557.49 |   1.9531 |   35040 B |       84.03 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    127,389.0 ns |    127,918.5 ns |     551.41 |   1.9531 |   34136 B |       81.86 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    250,762.3 ns |    283,276.6 ns |     833.64 |   4.6387 |   77905 B |      186.82 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    290,969.9 ns |    292,586.2 ns |   1,259.19 |   4.3945 |   77833 B |      186.65 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 6.0 |    258,136.7 ns |    258,751.8 ns |   1,118.88 |   4.6387 |   78385 B |      187.97 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    288,348.5 ns |    288,196.3 ns |   1,207.00 |   4.3945 |   77833 B |      186.65 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 70,138,893.3 ns | 70,002,125.0 ns | 303,677.99 |        - |  842606 B |    2,020.64 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 68,730,485.4 ns | 68,720,925.0 ns | 281,649.22 |        - |  841703 B |    2,018.47 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 6.0 |      5,016.2 ns |      5,396.6 ns |      16.06 |   0.2594 |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 6.0 |    297,473.3 ns |    297,528.2 ns |   1,219.24 |   4.3945 |   74841 B |      179.47 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 6.0 |    299,059.8 ns |    298,137.5 ns |   1,146.95 |   4.3945 |   74513 B |      178.69 |
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |     87,167.2 ns |     87,144.4 ns |     357.22 |   1.3428 |   23504 B |       56.36 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |     92,954.8 ns |     92,942.7 ns |     402.20 |   1.3428 |   24008 B |       57.57 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 7.0 |     87,410.8 ns |     86,969.8 ns |     371.99 |   1.3428 |   23488 B |       56.33 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |     91,029.6 ns |     91,089.5 ns |     387.89 |   1.3428 |   23432 B |       56.19 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |    218,846.8 ns |    218,217.3 ns |     947.79 |   3.1738 |   54385 B |      130.42 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |    224,593.3 ns |    224,720.8 ns |     971.69 |   3.1738 |   55049 B |      132.01 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 7.0 |    219,656.4 ns |    219,592.8 ns |     894.78 |   3.1738 |   55201 B |      132.38 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |    133,657.3 ns |     99,834.1 ns |     442.50 |   3.1738 |   54969 B |      131.82 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 66,764,516.0 ns | 66,757,945.8 ns | 273,566.13 |        - |  783341 B |    1,878.52 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 66,501,259.8 ns | 66,422,893.8 ns | 283,269.88 |        - |  780435 B |    1,871.55 |
|                                                                      RawAdoNet |             .NET 7.0 |        141.0 ns |        142.3 ns |       0.52 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 7.0 |     78,271.3 ns |     78,285.8 ns |     327.75 |   1.2207 |   21216 B |       50.88 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |     80,792.2 ns |     79,955.0 ns |     338.03 |   1.2207 |   21336 B |       51.17 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 7.0 |     77,196.4 ns |     77,344.0 ns |     334.34 |   1.2207 |   20896 B |       50.11 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |     81,698.3 ns |     82,165.3 ns |     353.65 |   1.2207 |   21400 B |       51.32 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 7.0 |    177,251.6 ns |    173,043.8 ns |     605.48 |   2.6855 |   48097 B |      115.34 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |    201,846.2 ns |    202,229.1 ns |     845.06 |   2.6855 |   48953 B |      117.39 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 7.0 |    191,151.6 ns |    190,888.9 ns |     783.44 |   2.6855 |   48097 B |      115.34 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |    196,203.4 ns |    196,248.8 ns |     821.52 |   2.6855 |   48601 B |      116.55 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 74,555,439.6 ns | 74,490,242.9 ns | 312,102.84 |        - |  823042 B |    1,973.72 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 75,098,908.3 ns | 75,103,785.7 ns | 307,788.28 |        - |  822650 B |    1,972.78 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 7.0 |      5,304.4 ns |      5,303.6 ns |      21.74 |   0.2594 |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 7.0 |    199,406.1 ns |    201,293.2 ns |     826.77 |   2.6855 |   45721 B |      109.64 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 7.0 |    206,645.9 ns |    205,374.4 ns |     894.89 |   2.6855 |   45841 B |      109.93 |
|                                 Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    184,363.7 ns |    184,381.2 ns |     771.91 |   2.1973 |   39314 B |       94.28 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    196,375.1 ns |    196,574.6 ns |     837.43 |   2.4414 |   41145 B |       98.67 |
|                                   Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    182,991.3 ns |    183,107.0 ns |     772.89 |   2.1973 |   39312 B |       94.27 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    194,589.8 ns |    193,742.1 ns |     843.90 |   2.4414 |   41145 B |       98.67 |
|                                 Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    415,703.1 ns |    415,687.1 ns |   1,772.02 |   5.3711 |   91299 B |      218.94 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    434,849.2 ns |    431,706.8 ns |   1,883.38 |   5.3711 |   93385 B |      223.94 |
|                                   Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    413,925.1 ns |    414,045.5 ns |   1,733.16 |   5.3711 |   91298 B |      218.94 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    386,655.5 ns |    435,455.1 ns |   1,244.71 |   5.3711 |   93385 B |      223.94 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 49,234,137.3 ns | 49,236,090.0 ns | 213,065.63 |        - |  798674 B |    1,915.29 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 48,962,939.3 ns | 48,966,650.0 ns | 211,936.16 |        - |  800574 B |    1,919.84 |
|                                                                      RawAdoNet |        .NET Core 3.1 |        159.4 ns |        159.3 ns |       0.65 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    151,682.5 ns |    151,593.7 ns |     635.03 |   1.9531 |   34385 B |       82.46 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    163,627.6 ns |    164,568.9 ns |     707.78 |   1.9531 |   34136 B |       81.86 |
|                                   Small_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    148,658.8 ns |    151,172.8 ns |     503.70 |   1.9531 |   34384 B |       82.46 |
|                             Small_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    160,492.2 ns |    159,901.3 ns |     682.82 |   1.9531 |   34810 B |       83.48 |
|                                 Large_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    363,175.7 ns |    363,205.6 ns |   1,520.19 |   4.3945 |   77729 B |      186.40 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    373,415.1 ns |    373,256.0 ns |   1,562.97 |   4.3945 |   78157 B |      187.43 |
|                                   Large_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    360,271.5 ns |    357,741.1 ns |   1,562.07 |   4.3945 |   77731 B |      186.41 |
|                             Large_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    360,510.6 ns |    360,748.3 ns |   1,536.76 |   4.3945 |   78155 B |      187.42 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 54,965,777.3 ns | 54,577,710.0 ns | 237,444.76 |        - |  830744 B |    1,992.19 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 54,508,748.7 ns | 54,521,180.0 ns | 235,903.75 |        - |  831168 B |    1,993.21 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |      7,998.1 ns |      7,937.4 ns |      34.64 |   0.2441 |    4336 B |       10.40 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |        .NET Core 3.1 |    374,605.6 ns |    374,650.7 ns |   1,535.17 |   4.3945 |   76153 B |      182.62 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |        .NET Core 3.1 |    397,341.2 ns |    399,587.7 ns |   1,719.98 |   4.3945 |   75777 B |      181.72 |
|                                 Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    257,009.0 ns |    259,323.2 ns |   1,111.67 |   6.3477 |   42385 B |      101.64 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    270,439.5 ns |    266,924.3 ns |   1,152.95 |   6.3477 |   42889 B |      102.85 |
|                                   Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    252,186.6 ns |    252,228.8 ns |   1,055.90 |   6.3477 |   42381 B |      101.63 |
|                             Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    267,731.7 ns |    267,743.7 ns |   1,121.11 |   6.8359 |   43273 B |      103.77 |
|                                 Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    573,665.1 ns |    573,614.4 ns |   2,350.77 |  14.6484 |   97834 B |      234.61 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    594,009.7 ns |    603,358.8 ns |   2,569.45 |  15.6250 |   99114 B |      237.68 |
|                                   Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    533,583.1 ns |    533,060.1 ns |   1,729.69 |  14.6484 |   98226 B |      235.55 |
|                             Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    551,334.3 ns |    548,585.1 ns |   1,785.98 |  15.6250 |   99114 B |      237.68 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 66,289,920.8 ns | 65,772,350.0 ns | 287,450.30 | 125.0000 |  887814 B |    2,129.05 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 65,614,692.0 ns | 65,333,587.5 ns | 280,216.72 | 125.0000 |  888845 B |    2,131.52 |
|                                                                      RawAdoNet | .NET Framework 4.7.2 |        349.3 ns |        419.3 ns |       1.00 |   0.0663 |     417 B |        1.00 |
|                                 Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    214,694.1 ns |    215,804.4 ns |     931.08 |   5.6152 |   36387 B |       87.26 |
|                           Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    226,627.6 ns |    227,182.2 ns |     966.30 |   5.8594 |   36889 B |       88.46 |
|                                   Small_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    159,309.4 ns |    187,283.9 ns |     506.75 |   5.7373 |   36384 B |       87.25 |
|                             Small_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    226,771.9 ns |    227,393.1 ns |     949.68 |   5.8594 |   36889 B |       88.46 |
|                                 Large_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    482,896.2 ns |    482,899.7 ns |   1,978.95 |  12.6953 |   82333 B |      197.44 |
|                           Large_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    504,094.3 ns |    500,296.1 ns |   2,146.66 |  12.6953 |   82857 B |      198.70 |
|                                   Large_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    486,086.9 ns |    484,771.9 ns |   2,036.04 |  12.6953 |   82345 B |      197.47 |
|                             Large_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    495,651.9 ns |    495,657.0 ns |   2,075.21 |  12.6953 |   82857 B |      198.70 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 74,567,834.3 ns | 73,697,714.3 ns | 322,464.47 | 142.8571 |  922197 B |    2,211.50 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 73,767,792.3 ns | 73,769,385.7 ns | 308,818.95 | 142.8571 |  922194 B |    2,211.50 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |      9,025.7 ns |      9,027.6 ns |      37.79 |   0.7019 |    4461 B |       10.70 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload | .NET Framework 4.7.2 |    508,941.3 ns |    508,191.6 ns |   2,086.25 |  12.6953 |   82649 B |      198.20 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    524,430.2 ns |    524,453.1 ns |   2,235.07 |  12.6953 |   83170 B |      199.45 |
