```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                  | Runtime              | Mean       | Allocated |
|------------------------ |--------------------- |-----------:|----------:|
| TypeMapperAsEnum        | .NET 6.0             | 24.3786 ns |      24 B |
| DirectAccessAsEnum      | .NET 6.0             |  1.1368 ns |         - |
| TypeMapperAsObject      | .NET 6.0             | 30.3909 ns |      48 B |
| DirectAccessAsObject    | .NET 6.0             |  6.2093 ns |      24 B |
| TypeMapperAsDecimal     | .NET 6.0             |  0.1654 ns |         - |
| DirectAccessAsDecimal   | .NET 6.0             |  0.8195 ns |         - |
| TypeMapperAsBoolean     | .NET 6.0             |  1.8204 ns |         - |
| DirectAccessAsBoolean   | .NET 6.0             |  0.9196 ns |         - |
| TypeMapperAsString      | .NET 6.0             |  2.3927 ns |         - |
| DirectAccessAsString    | .NET 6.0             |  0.6048 ns |         - |
| TypeMapperAsInt         | .NET 6.0             |  1.8798 ns |         - |
| DirectAccessAsInt       | .NET 6.0             |  0.8280 ns |         - |
| TypeMapperAsBool        | .NET 6.0             |  2.9954 ns |         - |
| DirectAccessAsBool      | .NET 6.0             |  0.8502 ns |         - |
| TypeMapperAsKnownEnum   | .NET 6.0             |  2.8093 ns |         - |
| DirectAccessAsKnownEnum | .NET 6.0             |  0.1601 ns |         - |
| TypeMapperAsEnum        | .NET 7.0             | 12.0865 ns |         - |
| DirectAccessAsEnum      | .NET 7.0             |  0.4823 ns |         - |
| TypeMapperAsObject      | .NET 7.0             | 18.9392 ns |      24 B |
| DirectAccessAsObject    | .NET 7.0             |  8.5122 ns |      24 B |
| TypeMapperAsDecimal     | .NET 7.0             |  3.2718 ns |         - |
| DirectAccessAsDecimal   | .NET 7.0             |  0.3730 ns |         - |
| TypeMapperAsBoolean     | .NET 7.0             |  1.6294 ns |         - |
| DirectAccessAsBoolean   | .NET 7.0             |  0.5841 ns |         - |
| TypeMapperAsString      | .NET 7.0             |  1.8270 ns |         - |
| DirectAccessAsString    | .NET 7.0             |  0.3318 ns |         - |
| TypeMapperAsInt         | .NET 7.0             |  1.9239 ns |         - |
| DirectAccessAsInt       | .NET 7.0             |  0.4334 ns |         - |
| TypeMapperAsBool        | .NET 7.0             |  1.3754 ns |         - |
| DirectAccessAsBool      | .NET 7.0             |  0.5283 ns |         - |
| TypeMapperAsKnownEnum   | .NET 7.0             |  1.7002 ns |         - |
| DirectAccessAsKnownEnum | .NET 7.0             |  0.4918 ns |         - |
| TypeMapperAsEnum        | .NET Core 3.1        | 32.7906 ns |      24 B |
| DirectAccessAsEnum      | .NET Core 3.1        |  0.9051 ns |         - |
| TypeMapperAsObject      | .NET Core 3.1        | 36.5993 ns |      48 B |
| DirectAccessAsObject    | .NET Core 3.1        |  5.6490 ns |      24 B |
| TypeMapperAsDecimal     | .NET Core 3.1        |  3.3118 ns |         - |
| DirectAccessAsDecimal   | .NET Core 3.1        |  1.4904 ns |         - |
| TypeMapperAsBoolean     | .NET Core 3.1        |  2.2161 ns |         - |
| DirectAccessAsBoolean   | .NET Core 3.1        |  0.8621 ns |         - |
| TypeMapperAsString      | .NET Core 3.1        |  2.2289 ns |         - |
| DirectAccessAsString    | .NET Core 3.1        |  0.9122 ns |         - |
| TypeMapperAsInt         | .NET Core 3.1        |  0.0186 ns |         - |
| DirectAccessAsInt       | .NET Core 3.1        |  1.3394 ns |         - |
| TypeMapperAsBool        | .NET Core 3.1        |  2.7271 ns |         - |
| DirectAccessAsBool      | .NET Core 3.1        |  0.7496 ns |         - |
| TypeMapperAsKnownEnum   | .NET Core 3.1        |  0.5073 ns |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1        |  0.8910 ns |         - |
| TypeMapperAsEnum        | .NET Framework 4.7.2 | 54.6255 ns |      24 B |
| DirectAccessAsEnum      | .NET Framework 4.7.2 |  1.3974 ns |         - |
| TypeMapperAsObject      | .NET Framework 4.7.2 | 59.0427 ns |      48 B |
| DirectAccessAsObject    | .NET Framework 4.7.2 |  5.5793 ns |      24 B |
| TypeMapperAsDecimal     | .NET Framework 4.7.2 |  9.0747 ns |         - |
| DirectAccessAsDecimal   | .NET Framework 4.7.2 |  0.9628 ns |         - |
| TypeMapperAsBoolean     | .NET Framework 4.7.2 |  9.9903 ns |         - |
| DirectAccessAsBoolean   | .NET Framework 4.7.2 |  1.3192 ns |         - |
| TypeMapperAsString      | .NET Framework 4.7.2 | 10.1576 ns |         - |
| DirectAccessAsString    | .NET Framework 4.7.2 |  0.5898 ns |         - |
| TypeMapperAsInt         | .NET Framework 4.7.2 |  9.2702 ns |         - |
| DirectAccessAsInt       | .NET Framework 4.7.2 |  1.3509 ns |         - |
| TypeMapperAsBool        | .NET Framework 4.7.2 | 10.1170 ns |         - |
| DirectAccessAsBool      | .NET Framework 4.7.2 |  1.3360 ns |         - |
| TypeMapperAsKnownEnum   | .NET Framework 4.7.2 | 10.2480 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.4081 ns |         - |
