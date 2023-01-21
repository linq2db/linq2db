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
|                                                                         Method |              Runtime |             Mean |           Median |      Ratio |     Gen0 | Allocated | Alloc Ratio |
|------------------------------------------------------------------------------- |--------------------- |-----------------:|-----------------:|-----------:|---------:|----------:|------------:|
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |     196,618.3 ns |     204,388.9 ns |     301.73 |   1.9531 |   36225 B |       86.87 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |     214,121.7 ns |     201,283.1 ns |     326.87 |   1.9531 |   38745 B |       92.91 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 6.0 |     184,975.9 ns |     194,128.8 ns |     279.36 |   1.9531 |   36225 B |       86.87 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |     225,768.9 ns |     239,069.9 ns |     347.65 |   2.1973 |   38745 B |       92.91 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |     487,334.0 ns |     497,168.4 ns |     746.32 |   4.8828 |   85474 B |      204.97 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |     435,818.9 ns |     458,146.3 ns |     664.98 |   4.8828 |   86825 B |      208.21 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 6.0 |     477,157.7 ns |     502,866.1 ns |     725.20 |   4.8828 |   84769 B |      203.28 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |     523,050.3 ns |     550,446.4 ns |     798.89 |   4.8828 |   86474 B |      207.37 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 |  77,876,227.0 ns |  82,249,478.6 ns | 118,306.64 |        - |  792241 B |    1,899.86 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 |  70,197,539.3 ns |  70,256,825.0 ns | 107,121.89 |        - |  791447 B |    1,897.95 |
|                                                                      RawAdoNet |             .NET 6.0 |         253.6 ns |         259.2 ns |       0.39 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 6.0 |     147,284.9 ns |     144,412.2 ns |     225.77 |   1.7090 |   32304 B |       77.47 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |     156,245.6 ns |     163,719.9 ns |     237.54 |   1.7090 |   31880 B |       76.45 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 6.0 |     156,410.9 ns |     164,449.3 ns |     242.89 |   1.7090 |   31600 B |       75.78 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |     187,061.0 ns |     204,271.5 ns |     287.16 |   1.4648 |   31881 B |       76.45 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 6.0 |     427,799.5 ns |     431,378.1 ns |     656.60 |   3.9063 |   71745 B |      172.05 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |     483,855.5 ns |     535,247.1 ns |     744.26 |   3.9063 |   72377 B |      173.57 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 6.0 |     505,596.0 ns |     537,324.5 ns |     765.41 |   3.9063 |   71745 B |      172.05 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |     478,720.4 ns |     528,979.9 ns |     734.10 |   3.9063 |   72377 B |      173.57 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 108,417,242.6 ns | 113,587,100.0 ns | 164,881.80 |        - |  830536 B |    1,991.69 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 |  92,872,277.8 ns |  90,428,233.3 ns | 141,705.56 |        - |  830775 B |    1,992.27 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 6.0 |       8,785.0 ns |       9,188.3 ns |      13.43 |   0.2594 |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 6.0 |     452,371.8 ns |     457,914.9 ns |     703.58 |   3.9063 |   68713 B |      164.78 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 6.0 |     399,379.5 ns |     410,345.0 ns |     604.37 |   3.9063 |   68977 B |      165.41 |
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |     102,579.6 ns |     100,930.2 ns |     154.78 |   1.2207 |   21232 B |       50.92 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |     112,089.0 ns |     118,599.7 ns |     170.53 |   1.2207 |   21480 B |       51.51 |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 7.0 |     136,827.4 ns |     149,710.4 ns |     209.64 |   1.2207 |   21264 B |       50.99 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |     135,884.6 ns |     143,479.3 ns |     208.07 |   1.2207 |   21800 B |       52.28 |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |     346,859.8 ns |     377,738.5 ns |     529.21 |   1.9531 |   48913 B |      117.30 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |     339,764.8 ns |     377,391.0 ns |     524.68 |   2.9297 |   49257 B |      118.12 |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 7.0 |     342,417.9 ns |     369,022.1 ns |     527.46 |   2.4414 |   48593 B |      116.53 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |     320,675.1 ns |     343,920.3 ns |     493.64 |   2.9297 |   49529 B |      118.77 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 |  89,476,583.2 ns |  97,779,175.0 ns | 138,070.25 |        - |  769480 B |    1,845.28 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 |  87,858,164.8 ns |  93,092,725.0 ns | 133,579.28 |        - |  766352 B |    1,837.77 |
|                                                                      RawAdoNet |             .NET 7.0 |         237.4 ns |         242.8 ns |       0.36 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 7.0 |      90,518.3 ns |      92,451.9 ns |     139.16 |   1.0986 |   18944 B |       45.43 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |     109,556.6 ns |     120,468.3 ns |     167.72 |   1.0986 |   19000 B |       45.56 |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 7.0 |     112,136.9 ns |     117,345.7 ns |     170.69 |   1.0986 |   19104 B |       45.81 |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |     120,501.4 ns |     126,577.2 ns |     184.92 |   1.1597 |   19512 B |       46.79 |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 7.0 |     293,525.9 ns |     315,051.1 ns |     451.82 |   2.4414 |   42465 B |      101.83 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |     246,440.9 ns |     269,729.0 ns |     376.94 |   2.4414 |   43289 B |      103.81 |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 7.0 |     297,033.5 ns |     327,813.5 ns |     458.51 |   2.4414 |   42305 B |      101.45 |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |     261,928.2 ns |     243,850.3 ns |     398.37 |   2.4414 |   42905 B |      102.89 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 |  96,256,303.4 ns | 100,404,960.0 ns | 147,226.68 |        - |  810790 B |    1,944.34 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 |  92,494,822.8 ns |  99,097,691.7 ns | 140,962.07 |        - |  811952 B |    1,947.13 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 7.0 |       7,044.5 ns |       7,395.7 ns |      10.79 |   0.2594 |    4368 B |       10.47 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 7.0 |     310,499.6 ns |     331,294.1 ns |     480.08 |   1.9531 |   39705 B |       95.22 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 7.0 |     271,225.0 ns |     266,447.8 ns |     418.15 |   1.9531 |   39729 B |       95.27 |
|                                 Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |     258,699.4 ns |     273,088.8 ns |     395.65 |   1.9531 |   36528 B |       87.60 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |     263,298.7 ns |     281,527.9 ns |     402.44 |   1.9531 |   37352 B |       89.57 |
|                                   Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |     250,953.1 ns |     274,669.1 ns |     387.39 |   1.9531 |   36528 B |       87.60 |
|                             Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |     268,672.0 ns |     279,695.5 ns |     413.61 |   2.1973 |   37352 B |       89.57 |
|                                 Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |     589,975.3 ns |     617,160.0 ns |     904.66 |   4.8828 |   85473 B |      204.97 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |     510,497.1 ns |     513,363.2 ns |     776.46 |   4.8828 |   86297 B |      206.95 |
|                                   Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |     519,829.2 ns |     560,660.4 ns |     798.59 |   4.8828 |   85483 B |      205.00 |
|                             Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |     570,665.9 ns |     606,007.2 ns |     877.50 |   4.8828 |   86297 B |      206.95 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 |  58,339,161.6 ns |  63,749,931.2 ns |  88,657.00 |        - |  786614 B |    1,886.36 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 |  63,584,243.9 ns |  66,763,207.1 ns |  96,594.66 |        - |  795053 B |    1,906.60 |
|                                                                      RawAdoNet |        .NET Core 3.1 |         279.0 ns |         291.6 ns |       0.43 |   0.0215 |     360 B |        0.86 |
|                                 Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |     225,363.7 ns |     240,313.4 ns |     344.61 |   1.9531 |   32912 B |       78.93 |
|                           Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |     248,898.9 ns |     258,548.8 ns |     382.57 |   1.7090 |   32632 B |       78.25 |
|                                   Small_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |     220,110.5 ns |     219,911.0 ns |     338.72 |   1.7090 |   32560 B |       78.08 |
|                             Small_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |     213,661.3 ns |     224,389.3 ns |     326.97 |   1.7090 |   32632 B |       78.25 |
|                                 Large_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |     484,504.9 ns |     507,604.2 ns |     741.94 |   3.9063 |   72865 B |      174.74 |
|                           Large_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |     528,994.9 ns |     544,671.0 ns |     809.08 |   3.9063 |   72937 B |      174.91 |
|                                   Large_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |     442,666.2 ns |     456,147.0 ns |     670.67 |   3.9063 |   72865 B |      174.74 |
|                             Large_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |     455,927.8 ns |     476,506.5 ns |     698.36 |   3.9063 |   72937 B |      174.91 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 |  60,045,536.7 ns |  64,464,390.9 ns |  92,288.20 |        - |  821923 B |    1,971.04 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 |  59,171,861.9 ns |  62,946,437.5 ns |  89,818.90 |        - |  822366 B |    1,972.10 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |       9,144.9 ns |       9,458.6 ns |      13.90 |   0.2441 |    4336 B |       10.40 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |        .NET Core 3.1 |     432,978.4 ns |     454,208.0 ns |     662.51 |   3.9063 |   68377 B |      163.97 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |        .NET Core 3.1 |     421,363.9 ns |     430,944.5 ns |     641.27 |   3.9063 |   70145 B |      168.21 |
|                                 Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |     288,337.5 ns |     304,551.0 ns |     438.73 |   6.3477 |   42513 B |      101.95 |
|                           Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |     292,975.9 ns |     314,691.0 ns |     453.44 |   6.3477 |   40705 B |       97.61 |
|                                   Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |     282,086.5 ns |     292,081.0 ns |     436.83 |   6.3477 |   42513 B |      101.95 |
|                             Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |     295,631.6 ns |     306,312.0 ns |     452.75 |   6.3477 |   40315 B |       96.68 |
|                                 Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |     709,638.8 ns |     740,980.7 ns |   1,083.38 |  14.6484 |   97962 B |      234.92 |
|                           Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |     624,443.0 ns |     650,124.2 ns |     946.78 |  14.6484 |   92306 B |      221.36 |
|                                   Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |     649,038.3 ns |     649,831.0 ns |     994.58 |  15.1367 |   97954 B |      234.90 |
|                             Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |     667,561.7 ns |     698,515.9 ns |   1,024.04 |  14.6484 |   92678 B |      222.25 |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 |  92,138,543.3 ns |  96,193,933.3 ns | 140,788.79 |        - |  875179 B |    2,098.75 |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 |  90,627,067.0 ns |  93,032,450.0 ns | 138,315.98 |        - |  868352 B |    2,082.38 |
|                                                                      RawAdoNet | .NET Framework 4.7.2 |         697.8 ns |         743.9 ns |       1.00 |   0.0658 |     417 B |        1.00 |
|                                 Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |     304,765.7 ns |     335,260.7 ns |     465.18 |   5.3711 |   34208 B |       82.03 |
|                           Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |     321,836.4 ns |     345,268.4 ns |     493.34 |   5.3711 |   35097 B |       84.17 |
|                                   Small_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |     335,636.5 ns |     362,273.7 ns |     509.28 |   5.3711 |   33824 B |       81.11 |
|                             Small_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |     314,327.1 ns |     345,007.9 ns |     481.05 |   5.3711 |   34716 B |       83.25 |
|                                 Large_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |     602,551.8 ns |     633,346.7 ns |     930.84 |  11.7188 |   76690 B |      183.91 |
|                           Large_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |     593,598.5 ns |     626,271.9 ns |     903.97 |  11.7188 |   77593 B |      186.07 |
|                                   Large_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |     525,439.6 ns |     553,971.3 ns |     799.16 |  11.7188 |   76690 B |      183.91 |
|                             Large_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |     529,514.2 ns |     572,856.4 ns |     795.34 |  11.7188 |   77593 B |      186.07 |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 |  88,198,271.0 ns |  95,858,933.3 ns | 136,231.76 |        - |  907947 B |    2,177.33 |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 |  97,553,422.0 ns | 100,814,087.5 ns | 149,111.46 | 125.0000 |  911377 B |    2,185.56 |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |      12,116.2 ns |      12,760.0 ns |      18.40 |   0.7019 |    4461 B |       10.70 |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload | .NET Framework 4.7.2 |     646,348.9 ns |     681,465.6 ns |     989.45 |  11.7188 |   76497 B |      183.45 |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |     568,312.8 ns |     619,670.0 ns |     876.00 |  11.7188 |   77378 B |      185.56 |
