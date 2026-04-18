#!/usr/bin/env node
const { execSync } = require('child_process');
const path = require('path');

let input = '';
process.stdin.on('data', chunk => (input += chunk));
process.stdin.on('end', () => {
    let data = {};
    try {
        data = JSON.parse(input);
    } catch {}

    const C = {
        reset: '\x1b[0m',
        dim: '\x1b[90m',
        cyan: '\x1b[36m',
        yellow: '\x1b[33m',
        green: '\x1b[32m',
        red: '\x1b[31m',
        magenta: '\x1b[35m',
        white: '\x1b[97m',
        blink: '\x1b[5m',
    };

    const model = data.model?.display_name || data.model?.id || 'Claude';
    const version = data.version ? 'v' + data.version : '';
    const projectDir = data.workspace?.project_dir || '';
    // Prefer the harness-supplied cwd fields; fall back to project_dir before
    // process.cwd() so we still point at the project even if the harness omits
    // both cwd signals (process.cwd() is the CLI's launch dir on Windows).
    const cwd = data.workspace?.current_dir || data.cwd || projectDir || process.cwd();

    let branch = '';
    try {
        branch = execSync('git rev-parse --abbrev-ref HEAD', {
            cwd,
            stdio: ['ignore', 'pipe', 'ignore'],
            timeout: 500,
        }).toString().trim();
    } catch {}

    let cwdLabel;
    if (projectDir && (cwd === projectDir || cwd.startsWith(projectDir + path.sep))) {
        const rel = path.relative(projectDir, cwd);
        cwdLabel = rel ? path.basename(projectDir) + path.sep + rel : path.basename(projectDir);
    } else {
        cwdLabel = path.basename(cwd) || cwd;
    }

    const cost = data.cost?.total_cost_usd;
    const costLabel = typeof cost === 'number' ? '$' + cost.toFixed(3) : '';

    const linesAdded = data.cost?.total_lines_added;
    const linesRemoved = data.cost?.total_lines_removed;
    let linesLabel = '';
    if (linesAdded || linesRemoved) {
        linesLabel = `+${linesAdded || 0}/-${linesRemoved || 0}`;
    }

    const totalIn = data.context_window?.total_input_tokens;
    const totalOut = data.context_window?.total_output_tokens;
    let tokensLabel = '';
    if (typeof totalIn === 'number' || typeof totalOut === 'number') {
        const formatK = n => typeof n === 'number' ? (n >= 1000 ? (n / 1000).toFixed(1) + 'k' : String(n)) : '0';
        tokensLabel = formatK(totalIn) + ' in / ' + formatK(totalOut) + ' out';
    }

    const ctxWarn = data.exceeds_200k_tokens ? C.red + 'ctx>200k' + C.reset : '';

    // Helper: pick color by usage-percentage thresholds (90 → red+blink, 80 → red, 50 → yellow, below → lowColor)
    function usageColor(pct, lowColor) {
        if (pct >= 90) return C.red + C.blink;
        if (pct >= 80) return C.red;
        if (pct >= 50) return C.yellow;
        return lowColor;
    }

    // Helper: build a 10-char block progress bar
    function progressBar(pct, total) {
        const filled = Math.round((pct / 100) * total);
        const empty = total - filled;
        return '\u2588'.repeat(filled) + '\u2591'.repeat(empty);
    }

    // Segment: context used percentage (null early in session — omit when null/undefined)
    const ctxUsedPct = data.context_window?.used_percentage;
    let ctxUsedLabel = '';
    if (typeof ctxUsedPct === 'number') {
        const pct = Math.round(ctxUsedPct);
        ctxUsedLabel = usageColor(pct, C.dim) + 'ctx:' + pct + '%' + C.reset;
    }

    // Segment: 5-hour rate-limit usage (optional — only for subscribers, only after first API response)
    const fiveHourPct = data.rate_limits?.five_hour?.used_percentage;
    const fiveHourResetsAt = data.rate_limits?.five_hour?.resets_at;
    let fiveHourLabel = '';
    if (typeof fiveHourPct === 'number') {
        const pct = Math.round(fiveHourPct);
        const color = usageColor(pct, C.white);
        const bar = progressBar(pct, 10);
        let resetSuffix = '';
        if (typeof fiveHourResetsAt === 'number') {
            const resetDate = new Date(fiveHourResetsAt * 1000);
            const hh = String(resetDate.getHours()).padStart(2, '0');
            const mm = String(resetDate.getMinutes()).padStart(2, '0');
            resetSuffix = C.dim + ' (' + hh + ':' + mm + ')' + color;
        }
        fiveHourLabel = color + '5h:' + bar + ' ' + pct + '%' + resetSuffix + C.reset;
    }

    // Segment: 7-day rate-limit usage (optional — only for subscribers, only after first API response)
    const sevenDayPct = data.rate_limits?.seven_day?.used_percentage;
    const sevenDayResetsAt = data.rate_limits?.seven_day?.resets_at;
    let sevenDayLabel = '';
    if (typeof sevenDayPct === 'number') {
        const pct = Math.round(sevenDayPct);
        const color = usageColor(pct, C.white);
        let resetSuffix = '';
        if (typeof sevenDayResetsAt === 'number') {
            const resetDate = new Date(sevenDayResetsAt * 1000);
            const mo = String(resetDate.getMonth() + 1).padStart(2, '0');
            const dd = String(resetDate.getDate()).padStart(2, '0');
            const hh = String(resetDate.getHours()).padStart(2, '0');
            const mm = String(resetDate.getMinutes()).padStart(2, '0');
            resetSuffix = C.dim + ' (' + mo + '-' + dd + ' ' + hh + ':' + mm + ')' + color;
        }
        sevenDayLabel = color + '7d:' + pct + '%' + resetSuffix + C.reset;
    }

    const parts = [
        C.cyan + model + C.reset,
        version ? C.dim + version + C.reset : null,
        branch ? C.yellow + branch + C.reset : null,
        C.dim + cwdLabel + C.reset,
        costLabel ? C.green + costLabel + C.reset : null,
        tokensLabel ? C.dim + tokensLabel + C.reset : null,
        ctxUsedLabel || null,
        fiveHourLabel || null,
        sevenDayLabel || null,
        linesLabel ? C.dim + linesLabel + C.reset : null,
        ctxWarn || null,
    ].filter(Boolean);

    process.stdout.write(parts.join(C.dim + ' │ ' + C.reset));
});
