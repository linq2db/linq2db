# linq2db.cli Query PR Refinement Plan

PR: `linq2db/linq2db#5678`

Goal: refine the `dotnet linq2db query` command into a clear agent-oriented, result-set-focused database inspection workflow with explicit guardrail semantics, predictable output, bounded execution, and accurate skill documentation.

## Principles

- The CLI tool is not a security boundary.
- The SQL guard is a best-effort read-only guardrail.
- The agent remains responsible for every SQL statement it generates and executes.
- `query` is result-set oriented and should primarily mean `SELECT` / read-oriented SQL.
- General DDL/DML execution belongs to a future `execute` workflow.
- Output conversion should be centralized, predictable, and provider-aware where needed.
- Avoid provider-specific formatter class hierarchies.
- Timeout values are specified in seconds.

## Phase 1: Terminology

Status: completed

Rename safety terminology to guard terminology.

- [x] `QuerySafetyValidator` -> `ReadOnlySqlGuard`
- [x] `GenericQuerySafetyValidator` -> `GenericReadOnlySqlGuard`
- [x] `SqlServerQuerySafetyValidator` -> `SqlServerReadOnlySqlGuard`
- [x] `QuerySafetyResult` -> `SqlGuardResult`
- [x] `QuerySqlSafetyMode` -> `UnsafeSqlPolicy`
- [x] Update tests and user-facing text.
- [x] Prefer wording: read-only guard, best-effort guard, guardrail, unsafe SQL policy.

Done when: code, tests, help, and `SKILL.md` consistently describe best-effort SQL guardrails.

## Phase 2: Query Contract

Status: completed

Clarify that `query` is not a general SQL execution command.

- [x] Remove DDL examples from common examples.
- [x] Add explicit documentation: `unsafeSql` bypasses the read-only guard only; it does not turn `query` into a general DDL/DML command runner.
- [x] Document future `execute` workflow as the right home for general DDL/DML execution.
- [x] Keep multiple statements forbidden regardless of `unsafeSql`.
- [x] Leave PR body alignment to Phase 13.

Done when: `query` is documented as result-set oriented everywhere.

## Phase 3: Connection String Formatting Diagnostics

Status: completed

Replace raw `string.Format` failure with clear CLI diagnostics.

- [x] Catch `FormatException` around connection string formatting.
- [x] Explain `{0}` is user and `{1}` is password.
- [x] Explain literal braces must be escaped as `{{` and `}}`.
- [x] Include original exception message.
- [x] Add tests for invalid braces, escaped braces, and normal user/password replacement.

Done when: invalid connection string format returns `INVALID_ARGUMENTS` with actionable text.

## Phase 4: Environment Variables for Secrets

Status: completed

Add optional environment-variable based inputs.

- [x] Add CLI options: `--connection-string-env`, `--user-env`, `--password-env`.
- [x] Add config properties: `connectionStringEnv`, `userEnv`, `passwordEnv`.
- [x] Apply precedence: explicit CLI > CLI env option > profile explicit > profile env option > inherited default profile.
- [x] Missing env var must be a clear error.
- [x] Do not print secret values in diagnostics.
- [x] Add tests for password env, missing env, CLI override, connection string env, and user env.

Done when: secrets can be supplied without command-line or config literal values.

## Phase 5: Timeout Model

Status: completed

Add timeout controls in seconds.

- [x] Document connection timeout as a connection-string responsibility.
- [x] Add `--command-timeout <seconds>` / `commandTimeout`.
- [x] Add `--lock-timeout <seconds>` / `lockTimeout`.
- [x] Ensure all timeout values are seconds.
- [x] Implement `commandTimeout` before release.
- [x] Implement `lockTimeout` best-effort by provider where reasonable.
- [x] Document unsupported lock timeout behavior.
- [x] Add parsing and invalid-value tests.

Done when: exploratory agent queries have bounded execution by default or configuration.

## Phase 6: Output Value Conversion

Status: completed

Stop serializing provider values directly.

- [x] Add `QueryActualFieldType`.
- [x] Keep actual field type detection close to query output metadata creation.
- [x] Read provider-specific field type once per column.
- [x] Add central `ReadFieldAsString(...)`.
- [x] Use one conversion path for JSON and CSV.
- [x] Emit JSON values as strings or null.
- [x] Convert bytes to SQL-style hexadecimal strings (`0x...`).
- [x] Convert dates with round-trip format.
- [x] Handle SQL Server `SqlDecimal` without CLR decimal overflow.
- [x] Add conversion tests for primitive, binary, date/time, null, and `SqlDecimal`.

Done when: `JsonSerializer.Serialize(reader.GetValue(...))` is gone from query output.

## Phase 7: Duplicate Column Names

Status: completed

Avoid ambiguous JSON object output.

- [x] Detect duplicate projected column names for `--output json`.
- [x] Return a diagnostic asking for explicit aliases or `json-table`.
- [x] Add tests for duplicate names and explicit aliases.

Done when: duplicate JSON object property names are not silently emitted.

## Phase 8: `json-table` Output

Status: completed

Add metadata-rich, duplicate-safe output format.

- [x] Add `--output json-table`.
- [x] Emit `columns` metadata with ordinal, name, field type, provider-specific field type, and data type name.
- [x] Emit `rows` as arrays of string/null values.
- [x] Emit `rowCount`.
- [x] Emit `truncated`.
- [x] Add tests for metadata, duplicate names by ordinal, and rows-as-arrays.

Done when: agents can request mechanically robust result output.

## Phase 9: Result Limits

Status: completed

Protect against accidental broad reads.

- [x] Add `--max-rows <count>` / `maxRows`.
- [x] Default to a conservative value, proposed: `1000`.
- [x] Stop reading after `maxRows`.
- [x] Report truncation to stderr for `json` and `csv`.
- [x] Report `truncated: true` for `json-table`.
- [x] Add tests for default limit, configured limit, and truncation diagnostics.

Done when: unlimited output is not the default.

## Phase 10: Streaming Output

Status: completed

Avoid buffering full result sets.

- [x] Stream CSV row by row.
- [x] Stream JSON array row by row.
- [x] Write output files directly through a writer/stream.
- [x] Remove full-result `MemoryStream.ToArray()` and full CSV `StringBuilder` buffering.
- [x] Keep implementation simple and reviewable.

Done when: output path does not build the entire result as one string.

## Phase 11: Output File Overwrite Policy

Status: completed

Avoid silent file replacement.

- [x] Add `--overwrite`.
- [x] If output file exists and `--overwrite` is not specified, return an error.
- [x] If `--overwrite` is specified, replace the file.
- [x] Add tests.

Done when: output files are not overwritten silently.

## Phase 12: `SKILL.md` Final Pass

Status: completed

Make `SKILL.md` a complete agent instruction document.

- [x] Query command purpose and scenarios.
- [x] Agent responsibility.
- [x] SQL generation rules for agents.
- [x] Read-only guard and unsafe SQL policy.
- [x] Single-statement contract.
- [x] Timeout options.
- [x] Connection string formatting and brace escaping.
- [x] Environment variable support for secrets.
- [x] Output formats and string serialization.
- [x] Result limits.
- [x] Skill command usage.
- [x] Non-goals / future `execute`.
- [x] Validate all JSON examples.

Done when: `dotnet linq2db skill` accurately reflects command behavior.

## Phase 13: Provider-Specific Type Coverage

Status: completed

Expand provider-specific value conversion beyond SQL Server.

- [x] Keep one provider-specific conversion validation test file in the main test assembly.
- [x] Validate provider-specific read and string conversion behavior for provider families covered in this PR.
- [x] Add conversion handling only for provider-specific types that need it.
- [x] Document provider-specific conversion coverage and limitations.
- [x] Keep standard CLR type conversion in the shared query output path.

Done when: provider-specific output conversion is validated beyond the original SQL Server-focused coverage.

## Phase 14: PR Body Final Pass

Status: completed

Keep PR description aligned with actual implementation.

- [x] Update Summary.
- [x] Update Why.
- [x] Update Scenarios.
- [x] Update Guardrail model.
- [x] Update Testing.
- [x] Mention follow-ups explicitly if any planned items are deferred.

Done when: PR body describes the shipped behavior, not the original rough plan.
