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
| TypeMapperAsEnum        | .NET 8.0             | 10.4106 ns |         - |
| DirectAccessAsEnum      | .NET 8.0             |  0.3589 ns |         - |
| TypeMapperAsObject      | .NET 8.0             | 14.9012 ns |      24 B |
| DirectAccessAsObject    | .NET 8.0             |  9.1787 ns |      24 B |
| TypeMapperAsDecimal     | .NET 8.0             |  1.6630 ns |         - |
| DirectAccessAsDecimal   | .NET 8.0             |  0.4926 ns |         - |
| TypeMapperAsBoolean     | .NET 8.0             |  1.3735 ns |         - |
| DirectAccessAsBoolean   | .NET 8.0             |  0.8337 ns |         - |
| TypeMapperAsString      | .NET 8.0             |  0.9363 ns |         - |
| DirectAccessAsString    | .NET 8.0             |  0.3503 ns |         - |
| TypeMapperAsInt         | .NET 8.0             |  1.6583 ns |         - |
| DirectAccessAsInt       | .NET 8.0             |  0.8307 ns |         - |
| TypeMapperAsBool        | .NET 8.0             |  0.8025 ns |         - |
| DirectAccessAsBool      | .NET 8.0             |  0.8533 ns |         - |
| TypeMapperAsKnownEnum   | .NET 8.0             |  3.0825 ns |         - |
| DirectAccessAsKnownEnum | .NET 8.0             |  0.3134 ns |         - |
| TypeMapperAsEnum        | .NET 9.0             | 10.1136 ns |         - |
| DirectAccessAsEnum      | .NET 9.0             |  0.3263 ns |         - |
| TypeMapperAsObject      | .NET 9.0             | 15.0550 ns |      24 B |
| DirectAccessAsObject    | .NET 9.0             |  3.4416 ns |      24 B |
| TypeMapperAsDecimal     | .NET 9.0             |  2.3852 ns |         - |
| DirectAccessAsDecimal   | .NET 9.0             |  0.4286 ns |         - |
| TypeMapperAsBoolean     | .NET 9.0             |  1.0198 ns |         - |
| DirectAccessAsBoolean   | .NET 9.0             |  0.4780 ns |         - |
| TypeMapperAsString      | .NET 9.0             |  1.7854 ns |         - |
| DirectAccessAsString    | .NET 9.0             |  0.4704 ns |         - |
| TypeMapperAsInt         | .NET 9.0             |  1.9028 ns |         - |
| DirectAccessAsInt       | .NET 9.0             |  1.3228 ns |         - |
| TypeMapperAsBool        | .NET 9.0             |  1.9878 ns |         - |
| DirectAccessAsBool      | .NET 9.0             |  1.6439 ns |         - |
| TypeMapperAsKnownEnum   | .NET 9.0             |  1.1624 ns |         - |
| DirectAccessAsKnownEnum | .NET 9.0             |  0.5112 ns |         - |
| TypeMapperAsEnum        | .NET Framework 4.6.2 | 53.0624 ns |      24 B |
| DirectAccessAsEnum      | .NET Framework 4.6.2 |  0.9660 ns |         - |
| TypeMapperAsObject      | .NET Framework 4.6.2 | 51.1239 ns |      48 B |
| DirectAccessAsObject    | .NET Framework 4.6.2 |  6.2387 ns |      24 B |
| TypeMapperAsDecimal     | .NET Framework 4.6.2 | 10.5206 ns |         - |
| DirectAccessAsDecimal   | .NET Framework 4.6.2 |  1.4143 ns |         - |
| TypeMapperAsBoolean     | .NET Framework 4.6.2 |  2.8887 ns |         - |
| DirectAccessAsBoolean   | .NET Framework 4.6.2 |  0.8582 ns |         - |
| TypeMapperAsString      | .NET Framework 4.6.2 |  6.1961 ns |         - |
| DirectAccessAsString    | .NET Framework 4.6.2 |  0.3149 ns |         - |
| TypeMapperAsInt         | .NET Framework 4.6.2 |  9.6501 ns |         - |
| DirectAccessAsInt       | .NET Framework 4.6.2 |  0.6068 ns |         - |
| TypeMapperAsBool        | .NET Framework 4.6.2 |  9.1108 ns |         - |
| DirectAccessAsBool      | .NET Framework 4.6.2 |  2.3250 ns |         - |
| TypeMapperAsKnownEnum   | .NET Framework 4.6.2 | 10.6354 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.6.2 |  0.9125 ns |         - |
