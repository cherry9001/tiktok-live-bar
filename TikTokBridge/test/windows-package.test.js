'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

test('Windows launch scripts use CRLF line endings', () => {
    const repositoryRoot = path.join(__dirname, '..', '..');
    for (const fileName of ['run.bat', 'build.bat']) {
        const content = fs.readFileSync(path.join(repositoryRoot, fileName), 'utf8');
        assert.match(content, /\r\n/, `${fileName} must contain CRLF line endings`);
        assert.doesNotMatch(content, /(^|[^\r])\n/, `${fileName} contains bare LF line endings`);
    }
});
