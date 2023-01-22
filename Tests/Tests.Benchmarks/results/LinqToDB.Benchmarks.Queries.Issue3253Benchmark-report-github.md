``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                                                         Method |              Runtime |            Mean |          Median |      Ratio |     Gen0 | Allocated | Alloc Ratio |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------------:|-----------:|---------:|----------:|------------:|
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    135,427.4 ns |    135,010.0 ns |     349.82 |   1.9531 |   36689 B |       87.98 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    153,362.5 ns |    154,439.0 ns |     387.14 |   2.1973 |   36936 B |       88.58 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    147,668.4 ns |    147,150.5 ns |     374.23 |   1.9531 |   36688 B |       87.98 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    154,721.6 ns |    155,568.9 ns |     360.95 |   2.1973 |   36937 B |       88.58 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    342,291.5 ns |    344,161.1 ns |     867.56 |   4.8828 |   84769 B |      203.28 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    347,388.4 ns |    346,986.7 ns |     857.48 |   4.8828 |   85865 B |      205.91 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    341,491.4 ns |    341,686.0 ns |     865.96 |   4.8828 |   84769 B |      203.28 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    331,025.9 ns |    349,765.4 ns |     520.97 |   5.1270 |   85865 B |      205.91 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 63,581,119.3 ns | 63,449,255.6 ns | 161,081.54 |        - |  792603 B |    1,900.73 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 63,882,806.7 ns | 63,759,762.5 ns | 161,738.75 |        - |  793040 B |    1,901.77 |
|                                                                      RawAdoNet |             .NET 6.0 |        168.8 ns |        169.4 ns |       0.43 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 6.0 |     55,274.6 ns |     55,331.1 ns |     139.90 |   1.8311 |   31344 B |       75.17 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    121,078.4 ns |    124,522.3 ns |     242.02 |   1.7090 |   30952 B |       74.23 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 6.0 |    124,868.8 ns |    124,669.4 ns |     315.94 |   1.7090 |   31344 B |       75.17 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    128,528.8 ns |    128,936.5 ns |     324.94 |   1.8311 |   31624 B |       75.84 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    253,580.2 ns |    287,553.6 ns |     589.64 |   4.1504 |   71489 B |      171.44 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    295,527.2 ns |    297,251.5 ns |     753.53 |   3.9063 |   71769 B |      172.11 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 6.0 |    285,183.4 ns |    285,196.3 ns |     727.01 |   3.9063 |   71489 B |      171.44 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    292,658.1 ns |    292,602.7 ns |     742.93 |   3.9063 |   71769 B |      172.11 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 72,065,721.4 ns | 72,230,862.5 ns | 182,422.04 |        - |  830444 B |    1,991.47 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 71,977,466.2 ns | 72,196,542.9 ns | 183,122.51 |        - |  831527 B |    1,994.07 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 6.0 |      5,694.2 ns |      5,695.2 ns |      14.42 |   0.2594 |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 6.0 |    269,862.2 ns |    270,216.0 ns |     680.33 |   3.9063 |   68345 B |      163.90 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 6.0 |    312,900.9 ns |    312,384.8 ns |     796.09 |   3.9063 |   68705 B |      164.76 |
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |     89,725.7 ns |     89,809.0 ns |     227.41 |   1.2207 |   21600 B |       51.80 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |     93,649.2 ns |     93,887.2 ns |     237.16 |   1.2207 |   21256 B |       50.97 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 7.0 |     86,012.3 ns |     90,818.3 ns |     201.96 |   1.2207 |   21200 B |       50.84 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |     95,461.1 ns |     95,377.7 ns |     241.77 |   1.2207 |   21752 B |       52.16 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |    207,075.1 ns |    220,471.8 ns |     541.37 |   2.9297 |   49105 B |      117.76 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |    215,808.1 ns |    233,041.1 ns |     566.94 |   2.9297 |   49113 B |      117.78 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 7.0 |    217,575.1 ns |    217,857.8 ns |     550.94 |   2.9297 |   49137 B |      117.83 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |    228,288.3 ns |    229,265.9 ns |     577.47 |   2.9297 |   49577 B |      118.89 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 65,961,379.1 ns | 67,616,150.0 ns | 169,458.16 |        - |  765234 B |    1,835.09 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 65,000,770.4 ns | 67,667,293.8 ns | 167,478.34 |        - |  766222 B |    1,837.46 |
|                                                                      RawAdoNet |             .NET 7.0 |        152.1 ns |        152.9 ns |       0.38 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 7.0 |     81,125.3 ns |     80,293.8 ns |     205.10 |   1.0986 |   19040 B |       45.66 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |     80,627.6 ns |     80,134.7 ns |     204.32 |   1.0986 |   19256 B |       46.18 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 7.0 |     54,337.3 ns |     40,395.2 ns |     197.30 |   1.0986 |   18880 B |       45.28 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |     81,938.4 ns |     82,333.0 ns |     207.33 |   1.0986 |   19224 B |       46.10 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 7.0 |    174,943.7 ns |    175,271.0 ns |     439.05 |   2.4414 |   42465 B |      101.83 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |    198,269.8 ns |    199,173.5 ns |     502.90 |   2.4414 |   42905 B |      102.89 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 7.0 |    191,907.9 ns |    191,239.4 ns |     485.33 |   2.4414 |   42753 B |      102.53 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |    197,312.0 ns |    196,634.2 ns |     499.69 |   2.4414 |   42745 B |      102.51 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 69,620,658.8 ns | 69,007,806.2 ns | 174,544.29 |        - |  811718 B |    1,946.57 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 76,618,992.4 ns | 77,233,714.3 ns | 193,852.94 |        - |  814314 B |    1,952.79 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 7.0 |      5,310.2 ns |      5,319.0 ns |      13.44 |   0.2594 |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 7.0 |    205,619.3 ns |    206,049.7 ns |     520.13 |   2.1973 |   39576 B |       94.91 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 7.0 |    206,687.4 ns |    209,423.8 ns |     524.93 |   2.1973 |   40625 B |       97.42 |
|                                 Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    197,675.9 ns |    197,275.0 ns |     500.58 |   2.1973 |   38465 B |       92.24 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    198,668.2 ns |    198,548.9 ns |     501.63 |   2.1973 |   37962 B |       91.04 |
|                                   Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    196,326.7 ns |    197,515.5 ns |     496.39 |   2.1973 |   38464 B |       92.24 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    192,733.3 ns |    193,038.5 ns |     488.09 |   2.1973 |   37962 B |       91.04 |
|                                 Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    449,720.1 ns |    451,356.1 ns |   1,141.58 |   5.3711 |   90577 B |      217.21 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    301,676.5 ns |    222,410.1 ns |     993.43 |   4.8828 |   86905 B |      208.41 |
|                                   Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    436,267.6 ns |    437,420.8 ns |   1,105.18 |   5.3711 |   90577 B |      217.21 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    391,544.7 ns |    389,908.7 ns |     989.20 |   4.8828 |   86905 B |      208.41 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 46,396,687.1 ns | 46,841,960.0 ns | 115,335.05 |        - |  784712 B |    1,881.80 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 50,693,703.3 ns | 51,305,500.0 ns | 128,432.32 |        - |  788838 B |    1,891.70 |
|                                                                      RawAdoNet |        .NET Core 3.1 |        166.7 ns |        168.4 ns |       0.42 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    153,560.7 ns |    153,073.6 ns |     388.90 |   1.7090 |   31537 B |       75.63 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    163,406.8 ns |    164,356.8 ns |     412.92 |   1.9531 |   33369 B |       80.02 |
|                                   Small_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    158,510.5 ns |    159,131.2 ns |     401.52 |   1.9531 |   33649 B |       80.69 |
|                             Small_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    165,607.7 ns |    165,583.1 ns |     419.57 |   1.9531 |   33368 B |       80.02 |
|                                 Large_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    343,922.3 ns |    369,073.4 ns |     881.89 |   4.3945 |   73953 B |      177.35 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    377,002.7 ns |    381,099.6 ns |     963.01 |   4.3945 |   73673 B |      176.67 |
|                                   Large_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    370,884.4 ns |    371,570.5 ns |     941.11 |   4.3945 |   73955 B |      177.35 |
|                             Large_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    289,222.1 ns |    353,432.3 ns |     884.80 |   4.3945 |   73673 B |      176.67 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 50,176,877.7 ns | 55,919,044.4 ns |  86,499.38 |        - |  821102 B |    1,969.07 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 56,106,462.2 ns | 56,778,555.6 ns | 142,132.65 |        - |  819414 B |    1,965.02 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |      8,538.0 ns |      8,595.8 ns |      21.63 |   0.2441 |    4336 B |       10.40 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |        .NET Core 3.1 |    343,852.0 ns |    340,817.9 ns |     884.61 |   3.9063 |   68105 B |      163.32 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |        .NET Core 3.1 |    366,437.2 ns |    364,039.7 ns |     919.23 |   3.9063 |   69473 B |      166.60 |
|                                 Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    248,125.8 ns |    249,825.4 ns |     630.01 |   6.3477 |   40197 B |       96.40 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    254,562.6 ns |    255,903.9 ns |     643.92 |   6.3477 |   40317 B |       96.68 |
|                                   Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    243,074.6 ns |    241,558.5 ns |     616.47 |   6.3477 |   40197 B |       96.40 |
|                             Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    261,501.3 ns |    261,515.7 ns |     663.29 |   6.3477 |   40317 B |       96.68 |
|                                 Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    551,698.8 ns |    551,609.5 ns |   1,396.57 |  14.6484 |   92170 B |      221.03 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    564,909.2 ns |    568,119.7 ns |   1,431.82 |  14.6484 |   92306 B |      221.36 |
|                                   Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    546,595.3 ns |    550,091.4 ns |   1,382.48 |  14.6484 |   92170 B |      221.03 |
|                             Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    569,200.6 ns |    569,270.2 ns |   1,440.89 |  14.6484 |   92298 B |      221.34 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 68,317,714.8 ns | 68,384,718.8 ns | 173,190.92 | 125.0000 |  870470 B |    2,087.46 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 66,652,310.9 ns | 66,615,312.5 ns | 167,689.84 | 125.0000 |  874506 B |    2,097.14 |
|                                                                      RawAdoNet | .NET Framework 4.7.2 |        394.8 ns |        396.3 ns |       1.00 |   0.0663 |     417 B |        1.00 |
|                                 Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    174,151.1 ns |    174,944.1 ns |     461.61 |   5.3711 |   34202 B |       82.02 |
|                           Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    204,361.5 ns |    204,394.3 ns |     517.75 |   5.3711 |   34713 B |       83.24 |
|                                   Small_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    194,994.1 ns |    194,603.4 ns |     494.92 |   5.3711 |   33819 B |       81.10 |
|                             Small_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    194,854.4 ns |    207,149.1 ns |     523.46 |   5.3711 |   34327 B |       82.32 |
|                                 Large_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    424,174.5 ns |    443,244.3 ns |   1,064.03 |  11.7188 |   76686 B |      183.90 |
|                           Large_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    450,390.8 ns |    450,582.8 ns |   1,140.33 |  12.2070 |   77204 B |      185.14 |
|                                   Large_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    447,640.5 ns |    448,973.9 ns |   1,133.49 |  11.7188 |   76686 B |      183.90 |
|                             Large_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    455,205.2 ns |    455,853.2 ns |   1,148.93 |  12.2070 |   77204 B |      185.14 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 76,310,946.2 ns | 76,425,242.9 ns | 192,173.78 | 142.8571 |  910498 B |    2,183.45 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 75,563,101.9 ns | 75,249,357.1 ns | 191,399.85 | 142.8571 |  910494 B |    2,183.44 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |      9,500.4 ns |      9,566.7 ns |      24.03 |   0.7019 |    4461 B |       10.70 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload | .NET Framework 4.7.2 |    458,288.3 ns |    468,922.9 ns |     998.19 |  11.7188 |   76493 B |      183.44 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    442,170.8 ns |    479,207.8 ns |   1,171.43 |  12.2070 |   76993 B |      184.64 |
