const test = require('node:test');
const assert = require('node:assert/strict');
const {
    sanitizeGameEvent,
    isLoopbackAddress,
    isAllowedHost,
    isAllowedOrigin,
    consumeRateLimit
} = require('../src/security');

test('accepts only local HTTP hosts and origins by default', () => {
    assert.equal(isAllowedHost('127.0.0.1:3000', 3000), true);
    assert.equal(isAllowedHost('evil.example:3000', 3000), false);
    assert.equal(isAllowedOrigin('http://localhost:3000', 3000), true);
    assert.equal(isAllowedOrigin('https://evil.example', 3000), false);
    assert.equal(isLoopbackAddress('::ffff:127.0.0.1'), true);
});

test('sanitizes untrusted TikTok event fields and URLs', () => {
    const event = sanitizeGameEvent({
        type: 'gift',
        userId: 'user\u0000id',
        nickname: 'A'.repeat(200),
        avatar: 'javascript:alert(1)',
        giftPictureUrl: 'https://cdn.example/gift.png',
        diamondCount: Infinity,
        comment: 'hello\nworld'
    });
    assert.equal(event.userId, 'user id');
    assert.equal(event.nickname.length, 80);
    assert.equal(event.avatar, '');
    assert.equal(event.giftPictureUrl, 'https://cdn.example/gift.png');
    assert.equal(event.diamondCount, 0);
    assert.equal(event.comment, 'hello world');
    assert.equal(sanitizeGameEvent({ type: 'admin' }), null);
});

test('rate limiter closes the window after its configured capacity', () => {
    const state = {};
    assert.equal(consumeRateLimit(state, 1000, 2, 5000), true);
    assert.equal(consumeRateLimit(state, 1001, 2, 5000), true);
    assert.equal(consumeRateLimit(state, 1002, 2, 5000), false);
    assert.equal(consumeRateLimit(state, 7000, 2, 5000), true);
});
