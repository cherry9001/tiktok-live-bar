'use strict';

const fs = require('node:fs');
const path = require('node:path');

function parseEnvironment(content) {
    const parsed = {};
    for (const sourceLine of String(content || '').split(/\r?\n/)) {
        let line = sourceLine.trim();
        if (!line || line.startsWith('#')) continue;
        if (line.startsWith('export ')) line = line.slice(7).trim();

        const separator = line.indexOf('=');
        if (separator <= 0) continue;
        const key = line.slice(0, separator).trim();
        if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(key)) continue;

        let value = line.slice(separator + 1).trim();
        if (value.length >= 2) {
            const first = value[0];
            const last = value[value.length - 1];
            if ((first === '"' && last === '"') || (first === "'" && last === "'")) {
                value = value.slice(1, -1);
            }
        }
        parsed[key] = value;
    }
    return parsed;
}

function loadEnvironmentFile(
    filePath = path.join(__dirname, '..', '..', '.env'),
    environment = process.env
) {
    let content;
    try {
        content = fs.readFileSync(filePath, 'utf8');
    } catch (error) {
        if (error.code === 'ENOENT') return false;
        throw error;
    }

    for (const [key, value] of Object.entries(parseEnvironment(content))) {
        if (environment[key] === undefined) environment[key] = value;
    }
    return true;
}

function getServerSettings(environment = process.env) {
    const requestedPort = Number(environment.PORT);
    const port = Number.isInteger(requestedPort) && requestedPort >= 1 && requestedPort <= 65535
        ? requestedPort
        : 3000;
    const host = String(environment.HOST || '127.0.0.1').trim() || '127.0.0.1';
    return {
        port,
        host,
        allowLan: environment.ALLOW_LAN === '1'
    };
}

module.exports = { getServerSettings, loadEnvironmentFile, parseEnvironment };
