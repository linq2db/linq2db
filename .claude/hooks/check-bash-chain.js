#!/usr/bin/env node
// Enforces CLAUDE.md § Bash command rules: one command per Bash call,
// no shell control flow, no command-substitution chains. Pipes `|` are allowed.
// Runs as a PreToolUse hook on the Bash tool.

let input = '';
process.stdin.on('data', chunk => (input += chunk));
process.stdin.on('end', () => {
    let data = {};
    try {
        data = JSON.parse(input);
    } catch {
        process.exit(0);
    }

    const cmd = data?.tool_input?.command;
    if (typeof cmd !== 'string' || cmd.length === 0) {
        process.exit(0);
    }

    // Strip heredoc bodies so JSON/markdown content inside them doesn't trip
    // the chain regexes. Handles <<EOF, <<'EOF', <<"EOF", and <<-EOF variants.
    let stripped = cmd.replace(
        /<<-?\s*['"]?(\w+)['"]?[\s\S]*?\n\s*\1\b/g,
        '<<HEREDOC>>'
    );

    // Strip quoted strings so their content can't trip the regexes below.
    stripped = stripped
        .replace(/'[^']*'/g, "''")
        .replace(/"(?:\\.|[^"\\])*"/g, '""');

    const violations = [];

    // 1. Boolean chains.
    if (/&&|\|\|/.test(stripped)) {
        violations.push("'&&' or '||' chaining");
    }

    // 2. Semicolon chains. After heredoc/quote stripping any `;` is a chain
    //    (case's `;;` would be caught here too, but `case` is banned in rule 3
    //    anyway so the overlap is deliberate).
    if (/;/.test(stripped)) {
        violations.push("';' chaining");
    }

    // 3. Shell control flow at a command position (line start or after a pipe).
    //    The custom boundaries `(?<![-\w])…(?![-\w])` exclude flag-style tokens
    //    like `git for-each-ref`, `--use-case=…`, `--while-idle`, etc.
    const controlFlow = /(^|\n|\|\s*)\s*(?<![-\w])(for|while|until|case|if|function)(?![-\w])\s/;
    if (controlFlow.test(stripped)) {
        violations.push('shell control flow (for/while/until/case/if/function)');
    }

    // 4. Nested chains inside command substitution $(...).
    //    Plain substitutions like $(git rev-parse HEAD) are fine; `$(a && b)`,
    //    `$(a || b)`, and `$(a; b)` are not.
    if (/\$\([^)]*(&&|\|\||;)[^)]*\)/.test(stripped)) {
        violations.push('command-substitution with nested chain ($(a && b) / $(a; b))');
    }

    if (violations.length > 0) {
        process.stderr.write(
            'Bash command violates CLAUDE.md § Bash command rules:\n' +
            violations.map(v => '  - ' + v).join('\n') + '\n' +
            'Split into separate tool calls (parallel if independent, sequential if dependent). ' +
            'Pipes `|` are allowed.\n'
        );
        process.exit(2);
    }

    process.exit(0);
});
