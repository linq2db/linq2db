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
| Method       | Runtime              | Mean      | Allocated |
|------------- |--------------------- |----------:|----------:|
| BuildAction  | .NET 8.0             | 0.4170 ns |         - |
| DirectAccess | .NET 8.0             | 0.4927 ns |         - |
| BuildAction  | .NET 9.0             | 0.6352 ns |         - |
| DirectAccess | .NET 9.0             | 0.4558 ns |         - |
| BuildAction  | .NET Framework 4.6.2 | 3.9758 ns |         - |
| DirectAccess | .NET Framework 4.6.2 | 0.8617 ns |         - |
