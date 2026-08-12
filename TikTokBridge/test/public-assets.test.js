'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

test('local assets referenced by the control panel exist', () => {
    const publicDirectory = path.join(__dirname, '..', 'public');
    const html = fs.readFileSync(path.join(publicDirectory, 'control.html'), 'utf8');
    const references = [...html.matchAll(/(?:src|href)="([^"]+)"/g)]
        .map(match => match[1])
        .filter(reference => !/^(?:https?:|mailto:|#)/i.test(reference));

    const missing = references.filter(reference => {
        const relativePath = reference.replace(/^\//, '');
        return !fs.existsSync(path.join(publicDirectory, relativePath));
    });

    assert.deepEqual(missing, []);
});
