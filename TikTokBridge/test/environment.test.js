'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const test = require('node:test');

const {
    getServerSettings,
    loadEnvironmentFile,
    parseEnvironment
} = require('../src/config/environment');

test('parses comments, quotes, and values containing equals signs', () => {
    assert.deepEqual(parseEnvironment([
        '# comment',
        'PORT=3100',
        'HOST="127.0.0.1"',
        "LIVE_PROVIDER='tikfinity'",
        'TOKEN=value=with=equals'
    ].join('\n')), {
        PORT: '3100',
        HOST: '127.0.0.1',
        LIVE_PROVIDER: 'tikfinity',
        TOKEN: 'value=with=equals'
    });
});

test('loads .env without overwriting variables supplied by the operating system', () => {
    const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'ongchu-env-'));
    const envPath = path.join(directory, '.env');
    fs.writeFileSync(envPath, 'PORT=3100\nHOST=0.0.0.0\n', 'utf8');
    const environment = { PORT: '3200' };

    try {
        assert.equal(loadEnvironmentFile(envPath, environment), true);
        assert.equal(environment.PORT, '3200');
        assert.equal(environment.HOST, '0.0.0.0');
    } finally {
        fs.rmSync(directory, { recursive: true, force: true });
    }
});

test('uses safe defaults when PORT is invalid', () => {
    assert.deepEqual(getServerSettings({ PORT: 'not-a-port' }), {
        port: 3000,
        host: '127.0.0.1',
        allowLan: false
    });
    assert.equal(getServerSettings({ PORT: '3100' }).port, 3100);
    assert.equal(getServerSettings({ PORT: '70000' }).port, 3000);
});
