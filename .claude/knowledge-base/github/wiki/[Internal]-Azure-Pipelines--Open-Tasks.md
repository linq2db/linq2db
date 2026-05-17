Azure pipelines known issues

- move to own images at least for problematic/missing databases:
  - `Sybase ASE` image missing utf-8 support
- flaky tests:
  - `TruncateIdentityNoResetTest("Oracle.11.Managed")` Expected 4, got 22
