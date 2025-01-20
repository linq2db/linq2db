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
| Method                                | Runtime              | Mean        | Allocated |
|-------------------------------------- |--------------------- |------------:|----------:|
| TestCastConvertTypeMapper             | .NET 6.0             |   5.1225 ns |         - |
| TestCastConvertDirectAccess           | .NET 6.0             |   0.8526 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 6.0             |  21.4979 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 6.0             |   0.9118 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 6.0             |  47.4445 ns |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET 6.0             |   0.8851 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 6.0             |   5.1219 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 6.0             |   0.9503 ns |         - |
| TestCastConvertTypeMapper             | .NET 8.0             |   3.6152 ns |         - |
| TestCastConvertDirectAccess           | .NET 8.0             |   0.4638 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 8.0             |  21.9988 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 8.0             |   0.4933 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 8.0             |  21.7000 ns |         - |
| TestDictionaryCastConvertDirectAccess | .NET 8.0             |   0.4558 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 8.0             |   2.8378 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 8.0             |   0.4594 ns |         - |
| TestCastConvertTypeMapper             | .NET 9.0             |   2.6795 ns |         - |
| TestCastConvertDirectAccess           | .NET 9.0             |   0.5021 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 9.0             |  21.8527 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 9.0             |   0.4505 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 9.0             |  20.1059 ns |         - |
| TestDictionaryCastConvertDirectAccess | .NET 9.0             |   0.4656 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 9.0             |   2.8192 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 9.0             |   0.4299 ns |         - |
| TestCastConvertTypeMapper             | .NET Framework 4.6.2 |  22.0963 ns |         - |
| TestCastConvertDirectAccess           | .NET Framework 4.6.2 |   0.9122 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET Framework 4.6.2 |  68.8212 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET Framework 4.6.2 |   0.9649 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET Framework 4.6.2 | 113.7320 ns |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.6.2 |   0.9506 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET Framework 4.6.2 |  24.5809 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET Framework 4.6.2 |   0.9118 ns |         - |
