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
|                                                                         Method |              Runtime |            Mean |          Median |      Ratio |     Gen0 | Allocated | Alloc Ratio |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------------:|-----------:|---------:|----------:|------------:|
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    179,253.4 ns |    178,745.1 ns |     421.86 |   2.1973 |   38625 B |       92.63 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    186,020.9 ns |    186,013.7 ns |     437.71 |   2.1973 |   39033 B |       93.60 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    166,901.1 ns |    166,651.2 ns |     402.80 |   2.1973 |   38625 B |       92.63 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    188,334.0 ns |    186,916.2 ns |     443.13 |   2.1973 |   39033 B |       93.60 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    402,010.6 ns |    407,802.2 ns |     940.57 |   5.3711 |   91731 B |      219.98 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    429,205.4 ns |    425,736.1 ns |   1,007.87 |   5.3711 |   96347 B |      231.05 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    416,066.6 ns |    414,976.0 ns |     979.01 |   5.3711 |   91731 B |      219.98 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    431,302.7 ns |    431,234.1 ns |   1,014.67 |   5.3711 |   96347 B |      231.05 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 64,164,561.6 ns | 64,062,090.6 ns | 150,924.61 |  31.2500 |  810696 B |    1,944.12 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 63,267,790.8 ns | 63,218,550.0 ns | 148,893.06 |        - |  814705 B |    1,953.73 |
|                                                                      RawAdoNet |             .NET 6.0 |        159.4 ns |        160.1 ns |       0.37 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    156,421.5 ns |    157,408.6 ns |     368.27 |   1.9531 |   33921 B |       81.35 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    165,800.1 ns |    164,669.8 ns |     389.98 |   1.9531 |   34393 B |       82.48 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 6.0 |    157,887.4 ns |    159,074.1 ns |     371.79 |   1.9531 |   34113 B |       81.81 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    131,203.8 ns |    161,007.9 ns |     359.44 |   1.9531 |   34393 B |       82.48 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    379,002.4 ns |    376,424.8 ns |     892.03 |   4.3945 |   78579 B |      188.44 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    361,577.1 ns |    368,070.1 ns |     773.83 |   4.3945 |   79211 B |      189.95 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 6.0 |    367,785.8 ns |    367,932.3 ns |     865.41 |   4.3945 |   78579 B |      188.44 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    239,883.8 ns |    169,112.0 ns |     662.05 |   4.6387 |   79211 B |      189.95 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 69,449,806.2 ns | 69,076,031.2 ns | 163,482.63 |        - |  842907 B |    2,021.36 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 69,241,946.2 ns | 69,256,412.5 ns | 162,928.40 |        - |  844127 B |    2,024.29 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 6.0 |      5,529.7 ns |      5,498.5 ns |      13.01 |   0.2594 |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 6.0 |    280,999.5 ns |    293,030.6 ns |     742.94 |   4.3945 |   76140 B |      182.59 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 6.0 |    398,932.1 ns |    400,490.2 ns |     938.03 |   4.3945 |   76405 B |      183.23 |
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |    110,649.0 ns |    110,683.5 ns |     260.55 |   1.5869 |   26977 B |       64.69 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |    121,118.7 ns |    121,933.0 ns |     292.08 |   1.4648 |   27561 B |       66.09 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 7.0 |    126,315.7 ns |    126,293.6 ns |     297.22 |   1.4648 |   27425 B |       65.77 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |    130,959.1 ns |    130,983.5 ns |     308.15 |   1.4648 |   27513 B |       65.98 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |    295,792.8 ns |    295,855.6 ns |     696.01 |   3.4180 |   64242 B |      154.06 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |    299,276.9 ns |    299,393.8 ns |     704.21 |   3.4180 |   63946 B |      153.35 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 7.0 |    304,438.6 ns |    304,232.1 ns |     716.35 |   3.4180 |   63826 B |      153.06 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |    304,094.0 ns |    307,718.9 ns |     700.24 |   3.4180 |   64522 B |      154.73 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 66,331,541.7 ns | 66,350,093.8 ns | 156,121.05 |        - |  797823 B |    1,913.24 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 66,754,088.4 ns | 66,756,356.2 ns | 157,095.55 |        - |  799047 B |    1,916.18 |
|                                                                      RawAdoNet |             .NET 7.0 |        143.1 ns |        143.0 ns |       0.34 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 7.0 |    111,642.8 ns |    112,231.8 ns |     259.51 |   1.4648 |   25233 B |       60.51 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |    113,964.7 ns |    113,126.8 ns |     267.88 |   1.4648 |   25001 B |       59.95 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 7.0 |    108,531.8 ns |    107,993.2 ns |     255.65 |   1.4648 |   24721 B |       59.28 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |     81,878.1 ns |     71,291.3 ns |     140.66 |   1.4648 |   25001 B |       59.95 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 7.0 |    206,859.7 ns |    261,098.7 ns |     542.58 |   3.4180 |   57378 B |      137.60 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |    278,100.2 ns |    275,846.3 ns |     653.29 |   3.4180 |   57882 B |      138.81 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 7.0 |    274,689.6 ns |    274,462.5 ns |     615.10 |   3.4180 |   57602 B |      138.13 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |    277,958.8 ns |    277,768.9 ns |     654.05 |   3.4180 |   57978 B |      139.04 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 75,441,620.0 ns | 75,329,357.1 ns | 177,647.21 |        - |  837465 B |    2,008.31 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 69,398,818.7 ns | 69,805,107.1 ns | 162,049.57 |        - |  838705 B |    2,011.28 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 7.0 |      4,632.4 ns |      4,606.3 ns |      10.82 |   0.2594 |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 7.0 |    284,185.2 ns |    282,117.6 ns |     669.07 |   2.9297 |   55675 B |      133.51 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 7.0 |    303,079.6 ns |    304,504.1 ns |     713.65 |   2.9297 |   56115 B |      134.57 |
|                                 Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    232,787.3 ns |    232,819.7 ns |     547.76 |   2.1973 |   38899 B |       93.28 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    251,058.2 ns |    250,880.9 ns |     591.37 |   2.4414 |   42028 B |      100.79 |
|                                   Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    226,174.8 ns |    225,356.2 ns |     532.23 |   2.1973 |   38897 B |       93.28 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    247,261.0 ns |    247,532.1 ns |     581.96 |   2.4414 |   42380 B |      101.63 |
|                                 Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    527,144.4 ns |    527,522.1 ns |   1,240.13 |   4.8828 |   91751 B |      220.03 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    486,637.1 ns |    490,032.0 ns |   1,143.24 |   5.3711 |   95227 B |      228.36 |
|                                   Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    487,977.7 ns |    484,473.4 ns |   1,131.92 |   4.8828 |   91747 B |      220.02 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    541,487.5 ns |    543,362.9 ns |   1,274.17 |   4.8828 |   95232 B |      228.37 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 50,266,720.0 ns | 49,498,850.0 ns | 118,609.82 |        - |  802156 B |    1,923.64 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 37,484,791.8 ns | 40,522,806.2 ns | 117,985.90 |  31.2500 |  806686 B |    1,934.50 |
|                                                                      RawAdoNet |        .NET Core 3.1 |        158.3 ns |        159.9 ns |       0.37 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    175,374.8 ns |    199,047.0 ns |     436.00 |   1.9531 |   34321 B |       82.30 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    213,713.3 ns |    212,476.7 ns |     503.41 |   1.9531 |   34745 B |       83.32 |
|                                   Small_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    194,308.4 ns |    195,021.0 ns |     455.29 |   1.9531 |   34321 B |       82.30 |
|                             Small_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    208,300.1 ns |    209,417.6 ns |     489.65 |   1.9531 |   35098 B |       84.17 |
|                                 Large_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    414,433.4 ns |    414,708.5 ns |     974.20 |   4.3945 |   78787 B |      188.94 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    427,224.1 ns |    432,460.6 ns |     989.37 |   4.3945 |   79563 B |      190.80 |
|                                   Large_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    447,462.6 ns |    451,781.3 ns |   1,040.13 |   4.3945 |   78787 B |      188.94 |
|                             Large_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    469,301.9 ns |    469,351.3 ns |   1,104.57 |   3.9063 |   79563 B |      190.80 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 54,735,913.5 ns | 54,739,027.8 ns | 128,785.43 |        - |  839012 B |    2,012.02 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 55,477,850.4 ns | 55,023,988.9 ns | 130,710.67 |        - |  839788 B |    2,013.88 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |      7,912.4 ns |      7,946.7 ns |      18.54 |   0.2556 |    4336 B |       10.40 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |        .NET Core 3.1 |    488,175.8 ns |    488,012.6 ns |   1,148.69 |   3.9063 |   76480 B |      183.41 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |        .NET Core 3.1 |    488,348.4 ns |    488,429.7 ns |   1,149.40 |   3.9063 |   77096 B |      184.88 |
|                                 Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    349,729.3 ns |    349,402.5 ns |     823.14 |   7.8125 |   50393 B |      120.85 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    373,987.0 ns |    373,907.7 ns |     880.02 |   8.3008 |   53973 B |      129.43 |
|                                   Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    352,494.4 ns |    351,855.0 ns |     829.42 |   7.8125 |   50397 B |      120.86 |
|                             Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    375,667.4 ns |    373,245.7 ns |     882.43 |   8.3008 |   53597 B |      128.53 |
|                                 Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    770,498.3 ns |    770,528.9 ns |   1,812.96 |  17.5781 |  116683 B |      279.82 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    824,751.1 ns |    826,932.5 ns |   1,940.27 |  19.5313 |  123346 B |      295.79 |
|                                   Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    565,431.8 ns |    665,425.0 ns |   1,687.82 |  17.5781 |  116683 B |      279.82 |
|                             Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    816,794.9 ns |    823,003.0 ns |   1,914.89 |  19.5313 |  123346 B |      295.79 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 61,683,726.8 ns | 61,222,077.8 ns | 144,866.39 | 111.1111 |  923895 B |    2,215.58 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 67,963,150.8 ns | 68,145,775.0 ns | 159,779.65 | 125.0000 |  930831 B |    2,232.21 |
|                                                                      RawAdoNet | .NET Framework 4.7.2 |        425.0 ns |        425.5 ns |       1.00 |   0.0663 |     417 B |        1.00 |
|                                 Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    301,299.8 ns |    302,784.7 ns |     708.25 |   6.8359 |   43917 B |      105.32 |
|                           Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    306,671.6 ns |    305,634.5 ns |     721.32 |   6.8359 |   44419 B |      106.52 |
|                                   Small_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    271,064.9 ns |    266,679.6 ns |     637.79 |   6.8359 |   43149 B |      103.47 |
|                             Small_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    309,696.6 ns |    310,247.4 ns |     728.79 |   6.8359 |   44553 B |      106.84 |
|                                 Large_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    683,893.8 ns |    688,102.9 ns |   1,608.28 |  15.6250 |  100315 B |      240.56 |
|                           Large_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    701,792.6 ns |    706,095.7 ns |   1,652.73 |  15.6250 |  101730 B |      243.96 |
|                                   Large_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    564,022.9 ns |    668,832.2 ns |   1,350.95 |  15.6250 |  100315 B |      240.56 |
|                             Large_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    697,013.7 ns |    696,919.6 ns |   1,640.52 |  15.6250 |  101730 B |      243.96 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 73,814,623.5 ns | 73,819,414.3 ns | 173,684.50 | 142.8571 |  954997 B |    2,290.16 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 75,107,086.7 ns | 75,276,942.9 ns | 176,733.37 | 142.8571 |  956130 B |    2,292.88 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |      9,137.7 ns |      9,140.4 ns |      21.50 |   0.7019 |    4461 B |       10.70 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload | .NET Framework 4.7.2 |    637,878.8 ns |    670,527.6 ns |   1,624.07 |  15.6250 |  101921 B |      244.41 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    762,125.9 ns |    764,763.6 ns |   1,791.76 |  15.6250 |  102435 B |      245.65 |
