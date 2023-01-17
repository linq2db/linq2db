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
|                    Method |              Runtime |          Mean |        Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |--------------:|--------------:|------:|-------:|----------:|------------:|
|           TypeMapperEmpty |             .NET 6.0 |    10.3008 ns |    10.3003 ns |     ? | 0.0038 |      64 B |           ? |
|         DirectAccessEmpty |             .NET 6.0 |     1.4120 ns |     1.4119 ns |     ? |      - |         - |           ? |
|   TypeMapperAddFireRemove |             .NET 6.0 |   115.9862 ns |   116.7608 ns |     ? | 0.0134 |     224 B |           ? |
| DirectAccessAddFireRemove |             .NET 6.0 |    35.3189 ns |    35.2522 ns |     ? | 0.0114 |     192 B |           ? |
|      TypeMapperSubscribed |             .NET 6.0 |    51.0997 ns |    51.0596 ns |     ? | 0.0057 |      96 B |           ? |
|    DirectAccessSubscribed |             .NET 6.0 |     9.3316 ns |     9.3373 ns |     ? | 0.0038 |      64 B |           ? |
|       TypeMapperAddRemove |             .NET 6.0 | 1,054.9822 ns | 1,052.8042 ns |     ? | 0.0191 |     344 B |           ? |
|     DirectAccessAddRemove |             .NET 6.0 |    72.6565 ns |    73.1223 ns |     ? | 0.0091 |     152 B |           ? |
|           TypeMapperEmpty |             .NET 7.0 |    26.5131 ns |    27.8161 ns |     ? | 0.0038 |      64 B |           ? |
|         DirectAccessEmpty |             .NET 7.0 |     0.5176 ns |     0.5394 ns |     ? |      - |         - |           ? |
|   TypeMapperAddFireRemove |             .NET 7.0 |    93.6355 ns |    93.6724 ns |     ? | 0.0134 |     224 B |           ? |
| DirectAccessAddFireRemove |             .NET 7.0 |    60.4331 ns |    60.0327 ns |     ? | 0.0114 |     192 B |           ? |
|      TypeMapperSubscribed |             .NET 7.0 |    45.5078 ns |    44.9274 ns |     ? | 0.0057 |      96 B |           ? |
|    DirectAccessSubscribed |             .NET 7.0 |     8.4397 ns |     8.2777 ns |     ? | 0.0038 |      64 B |           ? |
|       TypeMapperAddRemove |             .NET 7.0 | 1,014.3705 ns | 1,014.1324 ns |     ? | 0.0191 |     344 B |           ? |
|     DirectAccessAddRemove |             .NET 7.0 |    54.0783 ns |    54.0402 ns |     ? | 0.0091 |     152 B |           ? |
|           TypeMapperEmpty |        .NET Core 3.1 |     8.0231 ns |     6.2502 ns |     ? | 0.0038 |      64 B |           ? |
|         DirectAccessEmpty |        .NET Core 3.1 |     0.9599 ns |     0.9844 ns |     ? |      - |         - |           ? |
|   TypeMapperAddFireRemove |        .NET Core 3.1 |   113.2414 ns |   113.1470 ns |     ? | 0.0134 |     224 B |           ? |
| DirectAccessAddFireRemove |        .NET Core 3.1 |    82.4313 ns |    82.9828 ns |     ? | 0.0114 |     192 B |           ? |
|      TypeMapperSubscribed |        .NET Core 3.1 |    53.9023 ns |    53.9750 ns |     ? | 0.0057 |      96 B |           ? |
|    DirectAccessSubscribed |        .NET Core 3.1 |    10.4927 ns |    10.5855 ns |     ? | 0.0038 |      64 B |           ? |
|       TypeMapperAddRemove |        .NET Core 3.1 |   823.1230 ns |   567.1335 ns |     ? | 0.0200 |     344 B |           ? |
|     DirectAccessAddRemove |        .NET Core 3.1 |    73.4689 ns |    73.4582 ns |     ? | 0.0091 |     152 B |           ? |
|           TypeMapperEmpty | .NET Framework 4.7.2 |    11.5005 ns |    11.4999 ns |     ? | 0.0102 |      64 B |           ? |
|         DirectAccessEmpty | .NET Framework 4.7.2 |     0.6684 ns |     0.9151 ns |     ? |      - |         - |           ? |
|   TypeMapperAddFireRemove | .NET Framework 4.7.2 |   146.1428 ns |   146.7405 ns |     ? | 0.0355 |     225 B |           ? |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    79.7417 ns |    79.4642 ns |     ? | 0.0305 |     193 B |           ? |
|      TypeMapperSubscribed | .NET Framework 4.7.2 |    81.3772 ns |    81.7788 ns |     ? | 0.0153 |      96 B |           ? |
|    DirectAccessSubscribed | .NET Framework 4.7.2 |    10.1985 ns |    10.1920 ns |     ? | 0.0102 |      64 B |           ? |
|       TypeMapperAddRemove | .NET Framework 4.7.2 | 1,377.8450 ns | 1,378.8269 ns |     ? | 0.0534 |     345 B |           ? |
|     DirectAccessAddRemove | .NET Framework 4.7.2 |    70.6406 ns |    70.7131 ns |     ? | 0.0242 |     152 B |           ? |
