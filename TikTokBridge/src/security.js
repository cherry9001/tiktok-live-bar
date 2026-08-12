'use strict';

const EVENT_TYPES = new Set(['member', 'chat', 'gift', 'like', 'follow', 'share']);

function cleanText(value, maximumLength) {
    return String(value ?? '')
        .replace(/[\u0000-\u001f\u007f]/g, ' ')
        .replace(/\s+/g, ' ')
        .trim()
        .slice(0, maximumLength);
}

function boundedNumber(value, minimum, maximum, fallback = 0) {
    const number = Number(value);
    if (!Number.isFinite(number)) return fallback;
    return Math.min(maximum, Math.max(minimum, number));
}

function safeHttpsUrl(value) {
    const text = cleanText(value, 2048);
    if (!text) return '';
    try {
        const url = new URL(text);
        return url.protocol === 'https:' ? url.toString() : '';
    } catch {
        return '';
    }
}

function sanitizeGameEvent(input) {
    if (!input || typeof input !== 'object' || Array.isArray(input) || !EVENT_TYPES.has(input.type)) return null;
    return {
        type: input.type,
        eventId: cleanText(input.eventId, 180),
        userId: cleanText(input.userId, 128),
        uniqueId: cleanText(input.uniqueId, 64),
        nickname: cleanText(input.nickname, 80) || 'TikTok user',
        avatar: safeHttpsUrl(input.avatar),
        comment: cleanText(input.comment, 300),
        giftId: cleanText(input.giftId, 80),
        giftName: cleanText(input.giftName, 100),
        giftPictureUrl: safeHttpsUrl(input.giftPictureUrl),
        repeatCount: Math.floor(boundedNumber(input.repeatCount, 0, 100000, 0)),
        unitDiamondCount: Math.floor(boundedNumber(input.unitDiamondCount, 0, 100000000, 0)),
        diamondCount: Math.floor(boundedNumber(input.diamondCount, 0, 1000000000, 0)),
        likeCount: Math.floor(boundedNumber(input.likeCount, 0, 1000000, 0)),
        spectatorOnly: input.spectatorOnly === true
    };
}

function isLoopbackAddress(value) {
    const address = String(value || '').toLowerCase().replace(/^::ffff:/, '');
    return address === '127.0.0.1' || address === '::1';
}

function isAllowedHost(value, port, allowLan = false) {
    const host = String(value || '').trim().toLowerCase();
    if (!host || host.length > 255 || /[\s\\/]/.test(host)) return false;
    if (allowLan) return true;
    return new Set([`127.0.0.1:${port}`, `localhost:${port}`, `[::1]:${port}`]).has(host);
}

function isAllowedOrigin(value, port) {
    if (!value) return true;
    try {
        const origin = new URL(String(value));
        const hostname = origin.hostname.toLowerCase();
        return origin.protocol === 'http:' &&
            (hostname === '127.0.0.1' || hostname === 'localhost' || hostname === '[::1]' || hostname === '::1') &&
            origin.port === String(port);
    } catch {
        return false;
    }
}

function consumeRateLimit(state, now = Date.now(), maximum = 40, windowMs = 5000) {
    if (!state.windowStartedAt || now - state.windowStartedAt >= windowMs) {
        state.windowStartedAt = now;
        state.count = 0;
    }
    state.count += 1;
    return state.count <= maximum;
}

module.exports = {
    cleanText,
    sanitizeGameEvent,
    isLoopbackAddress,
    isAllowedHost,
    isAllowedOrigin,
    consumeRateLimit
};
