'use strict';

const assert = require('node:assert/strict');
const http = require('node:http');
const WebSocket = require('ws');

const port = Number(process.env.PORT) || 3000;
const baseUrl = `ws://127.0.0.1:${port}`;

function httpStatus(host) {
    return new Promise((resolve, reject) => {
        const request = http.get({ hostname: '127.0.0.1', port, path: '/api/config', headers: { Host: host } }, response => {
            response.resume();
            response.on('end', () => resolve({ status: response.statusCode, headers: response.headers }));
        });
        request.on('error', reject);
    });
}

function foreignOriginStatus() {
    return new Promise((resolve, reject) => {
        const socket = new WebSocket(baseUrl, { origin: 'https://evil.example' });
        socket.once('unexpected-response', (_request, response) => {
            response.resume();
            resolve(response.statusCode);
        });
        socket.once('open', () => reject(new Error('Foreign WebSocket origin was accepted')));
        socket.once('error', error => {
            if (!String(error.message).includes('Unexpected server response')) reject(error);
        });
    });
}

function expectMessage(messages, predicate, timeoutMs = 3000) {
    return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => reject(new Error('Timed out waiting for WebSocket response')), timeoutMs);
        messages.push(message => {
            if (!predicate(message)) return false;
            clearTimeout(timeout);
            resolve(message);
            return true;
        });
    });
}

async function sameOriginRoleTest() {
    const socket = new WebSocket(baseUrl, { origin: `http://127.0.0.1:${port}` });
    const waiters = [];
    socket.on('message', payload => {
        const message = JSON.parse(payload.toString());
        for (let index = waiters.length - 1; index >= 0; index -= 1) {
            if (waiters[index](message)) waiters.splice(index, 1);
        }
    });
    await new Promise((resolve, reject) => {
        socket.once('open', resolve);
        socket.once('error', reject);
    });
    socket.send(JSON.stringify({ type: 'register', role: 'overlay' }));
    await expectMessage(waiters, message => message.type === 'config');
    socket.send(JSON.stringify({ type: 'master_test', ruleId: 'none' }));
    const denial = await expectMessage(waiters, message => message.type === 'error' && /Master/.test(message.message));
    assert.match(denial.message, /Master/);
    socket.close();
}

async function unregisteredTest() {
    const socket = new WebSocket(baseUrl);
    const waiters = [];
    socket.on('message', payload => {
        const message = JSON.parse(payload.toString());
        for (let index = waiters.length - 1; index >= 0; index -= 1) {
            if (waiters[index](message)) waiters.splice(index, 1);
        }
    });
    await new Promise((resolve, reject) => {
        socket.once('open', resolve);
        socket.once('error', reject);
    });
    socket.send(JSON.stringify({ type: 'reset_game' }));
    const denial = await expectMessage(waiters, message => message.type === 'error' && /chưa đăng ký/.test(message.message));
    assert.match(denial.message, /chưa đăng ký/);
    socket.close();
}

async function main() {
    const local = await httpStatus(`127.0.0.1:${port}`);
    assert.equal(local.status, 200);
    assert.equal(local.headers['x-content-type-options'], 'nosniff');
    assert.ok(local.headers['content-security-policy']);
    assert.equal((await httpStatus(`evil.example:${port}`)).status, 403);
    assert.equal(await foreignOriginStatus(), 403);
    await sameOriginRoleTest();
    await unregisteredTest();
    console.log('SECURITY_SMOKE_OK');
}

main().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
