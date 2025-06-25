```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                  | Runtime              | Mean       | Allocated |
|------------------------ |--------------------- |-----------:|----------:|
| TypeMapperAsEnum        | .NET 6.0             | 26.5633 ns |      24 B |
| DirectAccessAsEnum      | .NET 6.0             |  0.9618 ns |         - |
| TypeMapperAsObject      | .NET 6.0             | 30.6699 ns |      48 B |
| DirectAccessAsObject    | .NET 6.0             |  6.1984 ns |      24 B |
| TypeMapperAsDecimal     | .NET 6.0             |  3.0474 ns |         - |
| DirectAccessAsDecimal   | .NET 6.0             |  0.4319 ns |         - |
| TypeMapperAsBoolean     | .NET 6.0             |  1.6269 ns |         - |
| DirectAccessAsBoolean   | .NET 6.0             |  0.7607 ns |         - |
| TypeMapperAsString      | .NET 6.0             |  1.3708 ns |         - |
| DirectAccessAsString    | .NET 6.0             |  0.7453 ns |         - |
| TypeMapperAsInt         | .NET 6.0             |  1.8137 ns |         - |
| DirectAccessAsInt       | .NET 6.0             |  1.4580 ns |         - |
| TypeMapperAsBool        | .NET 6.0             |  1.9125 ns |         - |
| DirectAccessAsBool      | .NET 6.0             |  0.8399 ns |         - |
| TypeMapperAsKnownEnum   | .NET 6.0             |  2.3733 ns |         - |
| DirectAccessAsKnownEnum | .NET 6.0             |  0.9210 ns |         - |
| TypeMapperAsEnum        | .NET 8.0             | 10.7802 ns |         - |
| DirectAccessAsEnum      | .NET 8.0             |  0.4169 ns |         - |
| TypeMapperAsObject      | .NET 8.0             | 17.2068 ns |      24 B |
| DirectAccessAsObject    | .NET 8.0             |  8.1580 ns |      24 B |
| TypeMapperAsDecimal     | .NET 8.0             |  2.3627 ns |         - |
| DirectAccessAsDecimal   | .NET 8.0             |  0.5439 ns |         - |
| TypeMapperAsBoolean     | .NET 8.0             |  1.4843 ns |         - |
| DirectAccessAsBoolean   | .NET 8.0             |  0.5496 ns |         - |
| TypeMapperAsString      | .NET 8.0             |  1.4205 ns |         - |
| DirectAccessAsString    | .NET 8.0             |  1.9328 ns |         - |
| TypeMapperAsInt         | .NET 8.0             |  1.4255 ns |         - |
| DirectAccessAsInt       | .NET 8.0             |  0.5121 ns |         - |
| TypeMapperAsBool        | .NET 8.0             |  2.2687 ns |         - |
| DirectAccessAsBool      | .NET 8.0             |  0.5556 ns |         - |
| TypeMapperAsKnownEnum   | .NET 8.0             |  2.9501 ns |         - |
| DirectAccessAsKnownEnum | .NET 8.0             |  0.4970 ns |         - |
| TypeMapperAsEnum        | .NET 9.0             | 10.0090 ns |         - |
| DirectAccessAsEnum      | .NET 9.0             |  0.4980 ns |         - |
| TypeMapperAsObject      | .NET 9.0             | 15.4696 ns |      24 B |
| DirectAccessAsObject    | .NET 9.0             |  7.6447 ns |      24 B |
| TypeMapperAsDecimal     | .NET 9.0             |  3.3241 ns |         - |
| DirectAccessAsDecimal   | .NET 9.0             |  0.6354 ns |         - |
| TypeMapperAsBoolean     | .NET 9.0             |  1.7938 ns |         - |
| DirectAccessAsBoolean   | .NET 9.0             |  0.3776 ns |         - |
| TypeMapperAsString      | .NET 9.0             |  1.6477 ns |         - |
| DirectAccessAsString    | .NET 9.0             |  0.3737 ns |         - |
| TypeMapperAsInt         | .NET 9.0             |  1.8674 ns |         - |
| DirectAccessAsInt       | .NET 9.0             |  0.0000 ns |         - |
| TypeMapperAsBool        | .NET 9.0             |  1.2660 ns |         - |
| DirectAccessAsBool      | .NET 9.0             |  0.7588 ns |         - |
| TypeMapperAsKnownEnum   | .NET 9.0             |  1.4919 ns |         - |
| DirectAccessAsKnownEnum | .NET 9.0             |  0.4185 ns |         - |
| TypeMapperAsEnum        | .NET Framework 4.6.2 | 53.2540 ns |      24 B |
| DirectAccessAsEnum      | .NET Framework 4.6.2 |  0.9557 ns |         - |
| TypeMapperAsObject      | .NET Framework 4.6.2 | 62.3033 ns |      48 B |
| DirectAccessAsObject    | .NET Framework 4.6.2 |  5.7299 ns |      24 B |
| TypeMapperAsDecimal     | .NET Framework 4.6.2 | 10.5205 ns |         - |
| DirectAccessAsDecimal   | .NET Framework 4.6.2 |  1.0101 ns |         - |
| TypeMapperAsBoolean     | .NET Framework 4.6.2 |  8.5707 ns |         - |
| DirectAccessAsBoolean   | .NET Framework 4.6.2 |  0.8653 ns |         - |
| TypeMapperAsString      | .NET Framework 4.6.2 |  8.4181 ns |         - |
| DirectAccessAsString    | .NET Framework 4.6.2 |  0.7130 ns |         - |
| TypeMapperAsInt         | .NET Framework 4.6.2 |  9.8025 ns |         - |
| DirectAccessAsInt       | .NET Framework 4.6.2 |  0.8456 ns |         - |
| TypeMapperAsBool        | .NET Framework 4.6.2 |  9.6246 ns |         - |
| DirectAccessAsBool      | .NET Framework 4.6.2 |  0.7285 ns |         - |
| TypeMapperAsKnownEnum   | .NET Framework 4.6.2 |  9.7380 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.6.2 |  0.6235 ns |         - |
