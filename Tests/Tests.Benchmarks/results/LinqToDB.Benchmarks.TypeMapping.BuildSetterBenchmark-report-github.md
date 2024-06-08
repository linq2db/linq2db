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
| TypeMapperAsEnum        | .NET 6.0             |  9.2499 ns |         - |
| DirectAccessAsEnum      | .NET 6.0             |  0.8333 ns |         - |
| TypeMapperAsKnownEnum   | .NET 6.0             |  2.3163 ns |         - |
| DirectAccessAsKnownEnum | .NET 6.0             |  0.4283 ns |         - |
| TypeMapperAsString      | .NET 6.0             |  5.1278 ns |         - |
| DirectAccessAsString    | .NET 6.0             |  3.7514 ns |         - |
| TypeMapperAsEnum        | .NET 7.0             | 10.5815 ns |         - |
| DirectAccessAsEnum      | .NET 7.0             |  0.4617 ns |         - |
| TypeMapperAsKnownEnum   | .NET 7.0             |  1.8503 ns |         - |
| DirectAccessAsKnownEnum | .NET 7.0             |  0.5302 ns |         - |
| TypeMapperAsString      | .NET 7.0             |  5.6626 ns |         - |
| DirectAccessAsString    | .NET 7.0             |  4.2465 ns |         - |
| TypeMapperAsEnum        | .NET Core 3.1        | 13.4469 ns |         - |
| DirectAccessAsEnum      | .NET Core 3.1        |  0.9597 ns |         - |
| TypeMapperAsKnownEnum   | .NET Core 3.1        |  1.9118 ns |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1        |  0.9189 ns |         - |
| TypeMapperAsString      | .NET Core 3.1        |  4.1973 ns |         - |
| DirectAccessAsString    | .NET Core 3.1        |  1.4212 ns |         - |
| TypeMapperAsEnum        | .NET Framework 4.7.2 | 30.4620 ns |         - |
| DirectAccessAsEnum      | .NET Framework 4.7.2 |  1.4404 ns |         - |
| TypeMapperAsKnownEnum   | .NET Framework 4.7.2 | 10.3045 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.2544 ns |         - |
| TypeMapperAsString      | .NET Framework 4.7.2 | 13.1603 ns |         - |
| DirectAccessAsString    | .NET Framework 4.7.2 |  0.5161 ns |         - |
