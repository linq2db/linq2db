```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.17763.7553/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X 3.39GHz, 2 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FTOCRB : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-DHTNJT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-QIENBV : .NET Framework 4.8 (4.8.4795.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                  | Runtime              | Mean       | Allocated |
|------------------------ |--------------------- |-----------:|----------:|
| TypeMapperAsEnum        | .NET 8.0             | 10.8408 ns |         - |
| DirectAccessAsEnum      | .NET 8.0             |  0.4574 ns |         - |
| TypeMapperAsObject      | .NET 8.0             | 17.2082 ns |      24 B |
| DirectAccessAsObject    | .NET 8.0             |  3.6641 ns |      24 B |
| TypeMapperAsDecimal     | .NET 8.0             |  2.8303 ns |         - |
| DirectAccessAsDecimal   | .NET 8.0             |  0.3434 ns |         - |
| TypeMapperAsBoolean     | .NET 8.0             |  1.3700 ns |         - |
| DirectAccessAsBoolean   | .NET 8.0             |  0.3206 ns |         - |
| TypeMapperAsString      | .NET 8.0             |  1.0847 ns |         - |
| DirectAccessAsString    | .NET 8.0             |  0.3039 ns |         - |
| TypeMapperAsInt         | .NET 8.0             |  1.4730 ns |         - |
| DirectAccessAsInt       | .NET 8.0             |  0.6178 ns |         - |
| TypeMapperAsBool        | .NET 8.0             |  2.1327 ns |         - |
| DirectAccessAsBool      | .NET 8.0             |  0.5666 ns |         - |
| TypeMapperAsKnownEnum   | .NET 8.0             |  0.8404 ns |         - |
| DirectAccessAsKnownEnum | .NET 8.0             |  0.4619 ns |         - |
| TypeMapperAsEnum        | .NET 9.0             |  7.8394 ns |         - |
| DirectAccessAsEnum      | .NET 9.0             |  0.3045 ns |         - |
| TypeMapperAsObject      | .NET 9.0             | 16.2603 ns |      24 B |
| DirectAccessAsObject    | .NET 9.0             |  7.7366 ns |      24 B |
| TypeMapperAsDecimal     | .NET 9.0             |  2.7940 ns |         - |
| DirectAccessAsDecimal   | .NET 9.0             |  0.4815 ns |         - |
| TypeMapperAsBoolean     | .NET 9.0             |  0.5757 ns |         - |
| DirectAccessAsBoolean   | .NET 9.0             |  0.2966 ns |         - |
| TypeMapperAsString      | .NET 9.0             |  2.3705 ns |         - |
| DirectAccessAsString    | .NET 9.0             |  0.4554 ns |         - |
| TypeMapperAsInt         | .NET 9.0             |  1.3874 ns |         - |
| DirectAccessAsInt       | .NET 9.0             |  0.0000 ns |         - |
| TypeMapperAsBool        | .NET 9.0             |  0.6611 ns |         - |
| DirectAccessAsBool      | .NET 9.0             |  0.4935 ns |         - |
| TypeMapperAsKnownEnum   | .NET 9.0             |  1.1997 ns |         - |
| DirectAccessAsKnownEnum | .NET 9.0             |  0.5753 ns |         - |
| TypeMapperAsEnum        | .NET Framework 4.6.2 | 53.3708 ns |      24 B |
| DirectAccessAsEnum      | .NET Framework 4.6.2 |  0.9761 ns |         - |
| TypeMapperAsObject      | .NET Framework 4.6.2 | 58.2542 ns |      48 B |
| DirectAccessAsObject    | .NET Framework 4.6.2 |  4.6032 ns |      24 B |
| TypeMapperAsDecimal     | .NET Framework 4.6.2 | 10.2595 ns |         - |
| DirectAccessAsDecimal   | .NET Framework 4.6.2 |  1.8792 ns |         - |
| TypeMapperAsBoolean     | .NET Framework 4.6.2 |  4.1173 ns |         - |
| DirectAccessAsBoolean   | .NET Framework 4.6.2 |  1.3236 ns |         - |
| TypeMapperAsString      | .NET Framework 4.6.2 |  9.2800 ns |         - |
| DirectAccessAsString    | .NET Framework 4.6.2 |  1.0735 ns |         - |
| TypeMapperAsInt         | .NET Framework 4.6.2 |  8.0842 ns |         - |
| DirectAccessAsInt       | .NET Framework 4.6.2 |  2.5917 ns |         - |
| TypeMapperAsBool        | .NET Framework 4.6.2 |  9.4668 ns |         - |
| DirectAccessAsBool      | .NET Framework 4.6.2 |  1.5599 ns |         - |
| TypeMapperAsKnownEnum   | .NET Framework 4.6.2 |  9.7608 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.6.2 |  1.4290 ns |         - |
