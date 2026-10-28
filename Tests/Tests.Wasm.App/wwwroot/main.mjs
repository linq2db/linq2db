import { dotnet } from './_framework/dotnet.js';

// .NET's streaming-request feature check calls `new Request("")`, which resolves only where `location` exists.
// Without this, every HttpClient request with a body fails under Node before fetch is called.
const NodeRequest = globalThis.Request;

globalThis.Request = class extends NodeRequest {
	constructor(input, init) {
		super(input === '' ? 'http://localhost/' : input, init);
	}
};

const { runMain } = await dotnet.create();

const exitCode = await runMain('linq2db.Tests.Wasm.App', process.argv.slice(2));

// Pipes are asynchronous on POSIX: process.exit would drop report lines still queued for stdout.
await new Promise(resolve => process.stdout.write('', resolve));

process.exit(exitCode);
