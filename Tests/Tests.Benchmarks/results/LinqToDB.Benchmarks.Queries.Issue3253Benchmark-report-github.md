``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-DILSWK : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-RYHTIE : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-XGIXLU : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                                                Method |              Runtime |            Mean |          Median |      Ratio | Allocated |
|---------------------------------------------------------------------- |--------------------- |----------------:|----------------:|-----------:|----------:|
|                        Small_UpdateStatement_With_Variable_Parameters |             .NET 5.0 |    222,496.9 ns |    222,962.0 ns |     587.80 |  54,900 B |
|                  Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 5.0 |    232,411.2 ns |    230,249.0 ns |     626.45 |  55,581 B |
|                        Small_InsertStatement_With_Variable_Parameters |             .NET 5.0 |    121,197.9 ns |    120,458.2 ns |     318.61 |  28,412 B |
|                  Small_InsertStatement_With_Variable_Parameters_Async |             .NET 5.0 |    167,094.2 ns |    165,552.3 ns |     372.26 |  29,541 B |
|                          Small_UpdateStatement_With_Static_Parameters |             .NET 5.0 |    233,230.6 ns |    230,942.9 ns |     644.03 |  54,900 B |
|                    Small_UpdateStatement_With_Static_Parameters_Async |             .NET 5.0 |    287,805.9 ns |    271,331.5 ns |     712.29 |  55,581 B |
|                        Large_UpdateStatement_With_Variable_Parameters |             .NET 5.0 |    516,429.6 ns |    513,302.8 ns |   1,321.29 | 132,216 B |
|                  Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 5.0 |    519,063.4 ns |    510,915.7 ns |   1,373.91 | 132,640 B |
|                          Large_UpdateStatement_With_Static_Parameters |             .NET 5.0 |    519,167.5 ns |    514,224.6 ns |   1,361.12 | 132,216 B |
|                    Large_UpdateStatement_With_Static_Parameters_Async |             .NET 5.0 |    646,854.5 ns |    618,581.4 ns |   1,475.08 | 132,640 B |
|       Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 5.0 | 46,950,072.0 ns | 46,905,556.0 ns | 124,180.58 | 888,307 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 5.0 | 48,490,096.2 ns | 48,130,689.8 ns | 125,874.84 | 886,827 B |
|                                                             RawAdoNet |             .NET 5.0 |        192.0 ns |        190.1 ns |       0.51 |     360 B |
|                        Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    248,688.9 ns |    249,037.8 ns |     639.92 |  54,452 B |
|                  Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    252,142.5 ns |    252,968.7 ns |     663.19 |  54,284 B |
|                        Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    414,317.7 ns |    357,080.8 ns |   1,088.48 |  30,272 B |
|                  Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    141,793.2 ns |    140,671.2 ns |     377.91 |  30,165 B |
|                          Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    237,161.6 ns |    234,816.8 ns |     624.82 |  54,452 B |
|                    Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    331,796.4 ns |    306,240.0 ns |     854.80 |  54,284 B |
|                        Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    562,280.2 ns |    558,212.6 ns |   1,460.99 | 131,624 B |
|                  Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    588,312.0 ns |    585,295.8 ns |   1,604.62 | 131,456 B |
|                          Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    553,271.5 ns |    550,984.9 ns |   1,459.41 | 131,624 B |
|                    Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    571,589.1 ns |    572,529.8 ns |   1,514.95 | 131,457 B |
|       Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 62,979,839.9 ns | 59,206,013.8 ns | 146,191.46 | 871,275 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 54,623,664.4 ns | 54,240,769.6 ns | 144,632.38 | 871,318 B |
|                                                             RawAdoNet |        .NET Core 3.1 |        183.3 ns |        182.3 ns |       0.49 |     360 B |
|                        Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    434,356.1 ns |    425,980.8 ns |   1,288.89 |  65,536 B |
|                  Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    544,054.3 ns |    510,825.9 ns |   1,401.12 |  65,536 B |
|                        Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    177,416.9 ns |    176,171.7 ns |     485.77 |  32,935 B |
|                  Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    377,879.5 ns |    328,994.1 ns |   1,154.90 |  32,768 B |
|                          Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    537,194.1 ns |    503,219.1 ns |   1,299.80 |  65,536 B |
|                    Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    662,541.0 ns |    578,409.4 ns |   1,852.41 |  65,536 B |
|                        Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    934,502.8 ns |    885,607.2 ns |   2,426.82 | 147,456 B |
|                  Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    979,248.2 ns |    934,758.8 ns |   2,657.88 | 147,456 B |
|                          Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    888,432.1 ns |    865,127.3 ns |   2,247.43 | 147,456 B |
|                    Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |  1,013,547.4 ns |    957,871.8 ns |   2,486.01 | 147,456 B |
|       Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 67,567,214.4 ns | 66,596,957.2 ns | 189,499.14 | 991,232 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 66,052,592.7 ns | 65,163,514.0 ns | 176,401.38 | 991,232 B |
|                                                             RawAdoNet | .NET Framework 4.7.2 |        379.4 ns |        382.3 ns |       1.00 |     417 B |
