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
|                                                                         Method |              Runtime |            Mean |          Median |      Ratio |     Gen0 |    Gen1 | Allocated | Alloc Ratio |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------------:|-----------:|---------:|--------:|----------:|------------:|
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    165,250.8 ns |    161,825.0 ns |     398.74 |   2.1973 |       - |   38625 B |       92.63 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    193,873.7 ns |    195,197.2 ns |     466.27 |   2.1973 |       - |   39033 B |       93.60 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    156,737.3 ns |    181,128.2 ns |     314.94 |   2.1973 |       - |   38625 B |       92.63 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    193,904.5 ns |    194,854.3 ns |     466.22 |   2.1973 |       - |   39033 B |       93.60 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    388,002.2 ns |    383,392.5 ns |     930.77 |   5.3711 |       - |   91731 B |      219.98 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    399,078.8 ns |    399,844.3 ns |     961.25 |   5.3711 |       - |   96347 B |      231.05 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    422,444.7 ns |    423,147.6 ns |   1,016.54 |   5.3711 |       - |   91731 B |      219.98 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    444,940.2 ns |    444,621.3 ns |   1,069.96 |   5.3711 |       - |   96347 B |      231.05 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 62,461,593.3 ns | 62,458,437.5 ns | 150,416.73 |        - |       - |  810677 B |    1,944.07 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 64,137,362.5 ns | 64,450,787.5 ns | 154,206.85 |        - |       - |  814217 B |    1,952.56 |
|                                                                      RawAdoNet |             .NET 6.0 |        162.5 ns |        162.0 ns |       0.39 |   0.0215 |       - |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    145,702.7 ns |    145,245.3 ns |     350.30 |   1.9531 |       - |   33921 B |       81.35 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    166,473.0 ns |    165,020.6 ns |     400.28 |   1.9531 |       - |   34393 B |       82.48 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 6.0 |    158,764.2 ns |    159,915.2 ns |     381.81 |   1.9531 |       - |   34113 B |       81.81 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    168,492.6 ns |    169,133.8 ns |     405.21 |   1.9531 |       - |   34393 B |       82.48 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    289,492.3 ns |    362,758.5 ns |     775.75 |   4.6387 |       - |   78579 B |      188.44 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    356,777.1 ns |    347,968.5 ns |     854.64 |   4.3945 |       - |   79220 B |      189.98 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 6.0 |    345,866.9 ns |    335,456.1 ns |     830.42 |   4.3945 |       - |   78579 B |      188.44 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    385,989.7 ns |    386,062.3 ns |     928.24 |   4.3945 |       - |   79211 B |      189.95 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 71,109,708.6 ns | 70,379,085.7 ns | 170,985.00 |        - |       - |  843037 B |    2,021.67 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 70,678,492.4 ns | 69,989,400.0 ns | 169,987.02 |        - |       - |  844247 B |    2,024.57 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 6.0 |      5,498.1 ns |      5,497.7 ns |      13.24 |   0.2594 |       - |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 6.0 |    394,370.4 ns |    394,251.0 ns |     950.73 |   4.3945 |       - |   76141 B |      182.59 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 6.0 |    403,417.4 ns |    404,659.5 ns |     972.59 |   4.3945 |       - |   76405 B |      183.23 |
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |    115,747.7 ns |    123,958.8 ns |     259.08 |   1.5869 |       - |   27201 B |       65.23 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |    134,166.9 ns |    133,017.6 ns |     322.68 |   1.4648 |       - |   27897 B |       66.90 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 7.0 |    126,220.4 ns |    126,639.0 ns |     303.53 |   1.4648 |       - |   26977 B |       64.69 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |    133,313.0 ns |    134,022.3 ns |     320.54 |   1.4648 |       - |   27577 B |       66.13 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |    259,408.1 ns |    300,587.1 ns |     672.89 |   3.6621 |       - |   63666 B |      152.68 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |    303,173.0 ns |    303,179.6 ns |     729.50 |   3.4180 |       - |   64410 B |      154.46 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 7.0 |    303,349.1 ns |    303,308.6 ns |     729.93 |   3.4180 |       - |   64114 B |      153.75 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |    295,912.9 ns |    311,728.7 ns |     632.01 |   3.4180 |       - |   65034 B |      155.96 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 67,274,187.8 ns | 67,246,964.3 ns | 161,876.97 |        - |       - |  800465 B |    1,919.58 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 67,135,047.3 ns | 67,101,487.5 ns | 161,549.55 |        - |       - |  797255 B |    1,911.88 |
|                                                                      RawAdoNet |             .NET 7.0 |        144.0 ns |        144.0 ns |       0.35 |   0.0215 |       - |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 7.0 |    107,157.1 ns |    107,148.1 ns |     257.84 |   1.4648 |       - |   24721 B |       59.28 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |    116,044.7 ns |    116,319.8 ns |     279.23 |   1.4648 |       - |   25257 B |       60.57 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 7.0 |    110,060.3 ns |    109,883.2 ns |     264.83 |   1.4648 |       - |   24977 B |       59.90 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |    112,971.4 ns |    112,263.5 ns |     271.65 |   1.4648 |       - |   25001 B |       59.95 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 7.0 |    272,794.9 ns |    272,306.5 ns |     655.93 |   3.4180 |       - |   57858 B |      138.75 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |    276,274.6 ns |    276,191.9 ns |     664.77 |   3.4180 |       - |   58522 B |      140.34 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 7.0 |    269,344.0 ns |    269,180.5 ns |     649.34 |   3.4180 |       - |   57858 B |      138.75 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |    277,869.6 ns |    277,892.0 ns |     668.62 |   3.4180 |       - |   57658 B |      138.27 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 75,225,596.9 ns | 75,150,157.1 ns | 181,010.91 |        - |       - |  837625 B |    2,008.69 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 69,743,777.9 ns | 75,031,985.7 ns | 158,264.14 |        - |       - |  841569 B |    2,018.15 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 7.0 |      5,123.9 ns |      5,122.7 ns |      12.34 |   0.2594 |       - |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 7.0 |    292,087.6 ns |    293,739.5 ns |     702.27 |   2.9297 |       - |   55451 B |      132.98 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 7.0 |    297,804.5 ns |    297,618.6 ns |     717.17 |   2.9297 |       - |   56371 B |      135.18 |
|                                 Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    210,885.4 ns |    209,735.4 ns |     507.48 |   2.1973 |       - |   38900 B |       93.29 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    228,537.6 ns |    227,342.2 ns |     550.92 |   2.4414 |       - |   42379 B |      101.63 |
|                                   Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    223,988.7 ns |    225,525.7 ns |     534.74 |   1.9531 |       - |   38897 B |       93.28 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    246,085.9 ns |    245,769.6 ns |     592.25 |   2.4414 |       - |   42380 B |      101.63 |
|                                 Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    510,338.9 ns |    510,403.6 ns |   1,227.98 |   4.8828 |       - |   91747 B |      220.02 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    535,331.4 ns |    534,527.0 ns |   1,288.08 |   4.8828 |       - |   95228 B |      228.36 |
|                                   Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    520,633.3 ns |    520,616.2 ns |   1,253.74 |   4.8828 |       - |   91751 B |      220.03 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    536,210.2 ns |    536,718.7 ns |   1,289.40 |   4.8828 |       - |   95228 B |      228.36 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 41,929,917.6 ns | 50,119,350.0 ns |  94,972.78 |        - |       - |  802156 B |    1,923.64 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 40,319,671.2 ns | 50,614,523.4 ns |  90,842.25 |  31.2500 |       - |  806686 B |    1,934.50 |
|                                                                      RawAdoNet |        .NET Core 3.1 |        162.2 ns |        161.1 ns |       0.39 |   0.0215 |       - |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    177,661.3 ns |    177,853.1 ns |     427.82 |   1.9531 |       - |   34321 B |       82.30 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    212,182.9 ns |    212,293.3 ns |     510.24 |   1.9531 |       - |   34747 B |       83.33 |
|                                   Small_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    200,149.3 ns |    199,462.5 ns |     481.67 |   1.9531 |       - |   34322 B |       82.31 |
|                             Small_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    215,544.2 ns |    215,601.1 ns |     518.31 |   1.9531 |       - |   35097 B |       84.17 |
|                                 Large_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    456,590.2 ns |    456,553.0 ns |   1,097.98 |   4.3945 |       - |   78789 B |      188.94 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    488,888.7 ns |    488,385.7 ns |   1,175.80 |   3.9063 |       - |   79567 B |      190.81 |
|                                   Large_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    429,077.4 ns |    429,992.1 ns |   1,029.21 |   3.9063 |       - |   78787 B |      188.94 |
|                             Large_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    433,637.7 ns |    442,305.4 ns |   1,033.87 |   3.9063 |       - |   79563 B |      190.80 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 56,311,844.4 ns | 56,287,588.9 ns | 135,391.23 |        - |       - |  839268 B |    2,012.63 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 57,014,224.4 ns | 56,955,222.2 ns | 137,079.57 |        - |       - |  839788 B |    2,013.88 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |      5,413.9 ns |      3,831.2 ns |      11.73 |   0.2556 |       - |    4336 B |       10.40 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |        .NET Core 3.1 |    498,683.3 ns |    499,142.8 ns |   1,199.03 |   3.9063 |       - |   76480 B |      183.41 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |        .NET Core 3.1 |    459,844.1 ns |    463,058.9 ns |   1,104.35 |   4.3945 |       - |   77104 B |      184.90 |
|                                 Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    328,484.7 ns |    321,471.0 ns |     785.33 |   7.8125 |       - |   50393 B |      120.85 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    374,700.9 ns |    373,279.5 ns |     900.98 |   8.3008 |       - |   53597 B |      128.53 |
|                                   Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    356,693.7 ns |    361,506.7 ns |     850.28 |   7.8125 |       - |   50397 B |      120.86 |
|                             Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    381,717.4 ns |    383,321.7 ns |     917.97 |   8.3008 |       - |   54369 B |      130.38 |
|                                 Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    797,405.9 ns |    812,938.3 ns |   1,830.81 |  17.5781 |       - |  116683 B |      279.82 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    756,158.7 ns |    742,062.4 ns |   1,804.75 |  19.5313 |       - |  123346 B |      295.79 |
|                                   Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    812,741.6 ns |    816,869.3 ns |   1,955.42 |  17.5781 |       - |  116683 B |      279.82 |
|                             Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    827,766.4 ns |    833,041.3 ns |   1,990.69 |  19.5313 |       - |  123346 B |      295.79 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 67,265,667.0 ns | 67,294,643.8 ns | 161,857.02 | 125.0000 |       - |  923670 B |    2,215.04 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 58,938,349.9 ns | 67,299,118.8 ns | 159,586.66 | 125.0000 | 62.5000 |  929803 B |    2,229.74 |
|                                                                      RawAdoNet | .NET Framework 4.7.2 |        417.5 ns |        419.6 ns |       1.00 |   0.0663 |       - |     417 B |        1.00 |
|                                 Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    267,333.4 ns |    296,910.0 ns |     594.78 |   6.8359 |       - |   43917 B |      105.32 |
|                           Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    315,128.5 ns |    317,993.3 ns |     757.77 |   6.8359 |       - |   44425 B |      106.53 |
|                                   Small_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    294,500.5 ns |    297,300.4 ns |     705.41 |   6.8359 |       - |   43149 B |      103.47 |
|                             Small_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    310,877.0 ns |    310,888.0 ns |     748.63 |   6.8359 |       - |   44553 B |      106.84 |
|                                 Large_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    675,807.3 ns |    675,709.2 ns |   1,627.43 |  15.6250 |       - |  100315 B |      240.56 |
|                           Large_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    693,547.7 ns |    693,713.0 ns |   1,670.17 |  15.6250 |       - |  101730 B |      243.96 |
|                                   Large_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    674,438.0 ns |    674,410.8 ns |   1,622.82 |  15.6250 |       - |  100315 B |      240.56 |
|                             Large_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    692,291.5 ns |    692,287.6 ns |   1,668.94 |  15.6250 |       - |  101730 B |      243.96 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 74,766,400.0 ns | 74,895,385.7 ns | 179,096.19 | 142.8571 |       - |  954997 B |    2,290.16 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 74,013,725.0 ns | 74,407,387.5 ns | 177,286.40 | 125.0000 |       - |  956422 B |    2,293.58 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |      9,049.8 ns |      9,048.2 ns |      21.76 |   0.7019 |       - |    4461 B |       10.70 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload | .NET Framework 4.7.2 |    745,682.3 ns |    745,393.7 ns |   1,797.68 |  15.6250 |       - |  101921 B |      244.41 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    762,860.8 ns |    763,228.8 ns |   1,834.48 |  15.6250 |       - |  102435 B |      245.65 |
