#!/usr/bin/env node
// SessionStart hook: record every session start to
// .build/.claude/kb-sessions.jsonl (gitignored, local). This is the
// denominator for the KB-usage "since installed" view in /kb-status — the
// count of sessions that started, so usage can be reported as a ratio even for
// sessions that never touch the KB.
//
// Wired in `.claude/settings.local.json` (gitignored) under hooks.SessionStart
// with async:true. Pairs with `track-kb-usage.js` (the numerator).
//
// Payload (stdin JSON): { session_id, cwd, source, hook_event_name, ... }.

const fs = require('fs');
const path = require('path');

let input = '';
process.stdin.on('data', c => (input += c));
process.stdin.on('end', () => {
    try {
        const data = JSON.parse(input || '{}');
        const base = data.cwd || process.cwd();
        const dir = path.join(base, '.build', '.claude');
        fs.mkdirSync(dir, { recursive: true });
        const rec = {
            ts: new Date().toISOString(),
            session_id: data.session_id || null,
            source: data.source || null,
        };
        fs.appendFileSync(path.join(dir, 'kb-sessions.jsonl'), JSON.stringify(rec) + '\n');
    } catch {
        // Never block startup on a logging hook error.
    }
    process.exit(0);
});
