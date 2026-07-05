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

Status: pending

Rename safety terminology to guard terminology.

- [ ] `QuerySafetyValidator` -> `ReadOnlySqlGuard`
- [ ] `GenericQuerySafetyValidator` -> `GenericReadOnlySqlGuard`
- [ ] `SqlServerQuerySafetyValidator` -> `SqlServerReadOnlySqlGuard`
- [ ] `QuerySafetyResult` -> `SqlGuardResult`
- [ ] `QuerySqlSafetyMode` -> `UnsafeSqlPolicy`
- [ ] Update tests and user-facing text.
- [ ] Prefer wording: read-only guard, best-effort guard, guardrail, unsafe SQL policy.

Done when: code, tests, help, and `SKILL.md` no longer imply strict SQL safety guarantees.

## Phase 2: Query Contract

Status: pending

Clarify that `query` is not a general SQL execution command.

- [ ] Remove DDL examples from common examples.
- [ ] Add explicit documentation: `unsafeSql` bypasses the read-only guard only; it does not turn `query` into a general DDL/DML command runner.
- [ ] Document future `execute` workflow as the right home for general DDL/DML execution.
- [ ] Keep multiple statements forbidden regardless of `unsafeSql`.
- [ ] Update PR body after documentation changes.

Done when: `query` is documented as result-set oriented everywhere.

## Phase 3: Connection String Formatting Diagnostics

Status: pending

Replace raw `string.Format` failure with clear CLI diagnostics.

- [ ] Catch `FormatException` around connection string formatting.
- [ ] Explain `{0}` is user and `{1}` is password.
- [ ] Explain literal braces must be escaped as `{{` and `}}`.
- [ ] Include original exception message.
- [ ] Add tests for invalid braces, escaped braces, and normal user/password replacement.

Done when: invalid connection string format returns `INVALID_ARGUMENTS` with actionable text.

## Phase 4: Environment Variables for Secrets

Status: pending

Add optional environment-variable based inputs.

- [ ] Add CLI options: `--connection-string-env`, `--user-env`, `--password-env`.
- [ ] Add config properties: `connectionStringEnv`, `userEnv`, `passwordEnv`.
- [ ] Apply precedence: explicit CLI > CLI env option > profile explicit > profile env option > inherited default profile.
- [ ] Missing env var must be a clear error.
- [ ] Do not print secret values in diagnostics.
- [ ] Add tests for password env, missing env, CLI override, connection string env, and user env.

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

- [x] Add `QueryProviderSpecificFieldKind`.
- [x] Add centralized `QueryFieldKindMap`.
- [x] Read provider-specific field type once per column.
- [x] Add central `ReadFieldAsString(...)`.
- [x] Use one conversion path for JSON and CSV.
- [x] Emit JSON values as strings or null.
- [x] Convert bytes to base64.
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

Status: pending

Make `SKILL.md` a complete agent instruction document.

- [ ] Query command purpose and scenarios.
- [ ] Agent responsibility.
- [ ] SQL generation rules for agents.
- [ ] Read-only guard and unsafe SQL policy.
- [ ] Single-statement contract.
- [ ] Timeout options.
- [ ] Connection string formatting and brace escaping.
- [ ] Environment variable support for secrets.
- [ ] Output formats and string serialization.
- [ ] Result limits.
- [ ] Skill command usage.
- [ ] Non-goals / future `execute`.
- [ ] Validate all JSON examples.

Done when: `dotnet linq2db skill` accurately reflects command behavior.

## Phase 13: PR Body Final Pass

Status: pending

Keep PR description aligned with actual implementation.

- [ ] Update Summary.
- [ ] Update Why.
- [ ] Update Scenarios.
- [ ] Update Safety / guardrail model.
- [ ] Update Testing.
- [ ] Mention follow-ups explicitly if any planned items are deferred.

Done when: PR body describes the shipped behavior, not the original rough plan.
