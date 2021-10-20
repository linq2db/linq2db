``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-HJIAFD : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-YNWNNJ : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-BTZBTH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                   Method |              Runtime |     Mean | Ratio | Allocated |
|------------------------- |--------------------- |---------:|------:|----------:|
|          Update_Nullable |             .NET 5.0 | 278.0 μs |  1.23 |     63 KB |
| Compiled_Update_Nullable |             .NET 5.0 | 102.2 μs |  0.45 |     31 KB |
|                   Update |             .NET 5.0 | 256.9 μs |  1.23 |     56 KB |
|          Compiled_Update |             .NET 5.0 | 104.2 μs |  0.50 |     31 KB |
|          Update_Nullable |        .NET Core 3.1 | 311.9 μs |  1.34 |     62 KB |
| Compiled_Update_Nullable |        .NET Core 3.1 | 112.7 μs |  0.52 |     31 KB |
|                   Update |        .NET Core 3.1 | 330.1 μs |  1.53 |     55 KB |
|          Compiled_Update |        .NET Core 3.1 | 118.7 μs |  0.57 |     31 KB |
|          Update_Nullable | .NET Framework 4.7.2 | 849.0 μs |  4.02 |     72 KB |
| Compiled_Update_Nullable | .NET Framework 4.7.2 | 276.1 μs |  1.32 |     32 KB |
|                   Update | .NET Framework 4.7.2 | 732.5 μs |  3.52 |     64 KB |
|          Compiled_Update | .NET Framework 4.7.2 | 218.4 μs |  1.00 |     32 KB |
