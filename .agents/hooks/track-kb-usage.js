#!/usr/bin/env node
// PostToolUse hook: record knowledge-base *consultation* events to
// .build/.agents/kb-usage.jsonl (gitignored, persists locally across sessions).
//
// This is the cheap, always-on counterpart to the authoritative transcript
// scanner `.agents/scripts/kb-usage-audit.ps1`. The scanner is retroactive and
// area-aware; this hook gives `/kb-status` a zero-parse "since installed" view.
//
// Wired in `.agents/settings.local.json` (gitignored) under hooks.PostToolUse
// with matcher "Read|Grep|Glob|Skill|Agent|Task" and async:true so it never
// blocks the tool. Early-exits on the vast majority of calls (non-KB) after a
// cheap string check.
//
// Payload (stdin JSON): { session_id, cwd, tool_name, tool_input, ... }.

const fs = require('fs');
const path = require('path');

let input = '';
process.stdin.on('data', c => (input += c));
process.stdin.on('end', () => {
    try {
        const data = JSON.parse(input || '{}');
        const tool = data.tool_name;
        const ti = data.tool_input || {};

        const isKbPath = p =>
            typeof p === 'string' &&
            p.indexOf('knowledge-base') >= 0 &&
            p.replace(/\\/g, '/').indexOf('knowledge-base/state') < 0;

        const areaOf = p => {
            if (typeof p !== 'string') return null;
            const s = p.replace(/\\/g, '/');
            let m = s.match(/knowledge-base\/(areas\/[^/"']+)/);
            if (m) return m[1];
            m = s.match(/knowledge-base\/(architecture|conventions|history|detected-issues|github|glossary|README)/);
            if (m) return 'knowledge-base/' + m[1];
            return 'knowledge-base/(other)';
        };

        let kind = null, detail = null, area = null;

        if (tool === 'Skill' && typeof ti.skill === 'string' && ti.skill.startsWith('kb-')) {
            const maint = ['kb-build', 'kb-refresh', 'kb-status'].includes(ti.skill) ||
                ti.skill.startsWith('kb-coverage') || ti.skill.startsWith('kb-fetch');
            kind = maint ? 'maintenance-skill' : 'ask-skill';
            detail = ti.skill;
        } else if ((tool === 'Agent' || tool === 'Task') && typeof ti.subagent_type === 'string' &&
                   ti.subagent_type.startsWith('kb-')) {
            const maint = ['kb-architect', 'kb-historian', 'kb-github-curator', 'kb-issue-detector']
                .includes(ti.subagent_type);
            kind = maint ? 'maintenance-agent' : (ti.subagent_type === 'kb-research' ? 'research' : 'agent');
            detail = ti.subagent_type;
        } else if (tool === 'Read' && isKbPath(ti.file_path)) {
            kind = 'read'; detail = ti.file_path; area = areaOf(ti.file_path);
        } else if (tool === 'Grep' || tool === 'Glob') {
            // Glob's `pattern` is a path glob, so it identifies a KB consultation.
            // Grep's `pattern` is a *content* regex — testing it against isKbPath
            // logs a false "search" for any content search over the literal string
            // "knowledge-base" in non-KB files. For Grep, only path-ish inputs count.
            const candidates = tool === 'Glob'
                ? [ti.path, ti.pattern]
                : [ti.path, ti.glob];
            const hit = candidates.find(isKbPath);
            if (hit) { kind = 'search'; detail = hit; area = areaOf(hit); }
        }

        if (!kind) { process.exit(0); }

        const base = data.cwd || process.cwd();
        const dir = path.join(base, '.build', '.agents');
        fs.mkdirSync(dir, { recursive: true });
        const rec = {
            ts: new Date().toISOString(),
            session_id: data.session_id || null,
            tool,
            kind,
            detail: typeof detail === 'string' ? detail.slice(0, 300) : detail,
            area,
        };
        fs.appendFileSync(path.join(dir, 'kb-usage.jsonl'), JSON.stringify(rec) + '\n');
    } catch {
        // Never block a tool call on a logging hook error.
    }
    process.exit(0);
});
