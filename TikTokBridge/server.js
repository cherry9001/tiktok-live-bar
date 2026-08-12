/* ═══════════════════════════════════════════════════════════
   ÔNG CHÚ MMO — TikTok Live Bridge Server
   © 2025 ÔNG CHÚ MMO — ongchummo.com
   Zalo: 0977.896.644 | Website: https://ongchummo.com
   Phần mềm thuộc sở hữu của ÔNG CHÚ MMO. Nghiêm cấm sao chép
   hoặc phân phối lại khi chưa được cấp phép bằng văn bản.
═══════════════════════════════════════════════════════════ */

const express = require('express');
const http = require('http');
const path = require('path');
const fs = require('fs/promises');
const WebSocket = require('ws');
const { TikTokLiveConnection } = require('tiktok-live-connector');
const { normalizeTikFinityMessage } = require('./src/tiktok/normalize-tikfinity-event');
const {
    normalizeChat,
    normalizeMember,
    normalizeGift,
    normalizeLike,
    normalizeSocial,
    isPendingGiftStreak
} = require('./src/tiktok/normalize-event');

const gameConfig = require('./config/game.json');
const giftConfig = require('./config/gifts.json');
const initialMasterConfig = require('./config/master.json');
const initialObservedGifts = require('./config/observed-gifts.json');
const { normalizeText, sanitizeMasterConfig, resolveMasterRule, applyRule, applyBuiltInChatCommand } = require('./src/master/rules');
const {
    sanitizeGameEvent,
    isLoopbackAddress,
    isAllowedHost,
    isAllowedOrigin,
    consumeRateLimit
} = require('./src/security');

const app = express();
const server = http.createServer(app);

const PORT = Number(process.env.PORT) || 3000;
const HOST = process.env.HOST || '127.0.0.1';
const ALLOW_LAN = process.env.ALLOW_LAN === '1';
const wss = new WebSocket.Server({
    noServer: true,
    maxPayload: 32 * 1024,
    perMessageDeflate: false,
    clientTracking: true
});
const publicDir = path.join(__dirname, 'public');
const assetsDir = path.join(__dirname, 'assets');
const gifsDir = path.join(assetsDir, 'gifs');
const masterConfigPath = path.join(__dirname, 'config', 'master.json');
const observedGiftsPath = path.join(__dirname, 'config', 'observed-gifts.json');
const LIVE_PROVIDER = String(process.env.LIVE_PROVIDER || gameConfig.liveProvider || 'tikfinity').toLowerCase();
const TIKFINITY_WS_URL = String(process.env.TIKFINITY_WS_URL || gameConfig.tikfinityWsUrl || 'ws://127.0.0.1:21213/');

let liveConnection = null;
let connectionAttempt = 0;
let desiredUsername = null;
let reconnectTimer = null;
let reconnectFailures = 0;
let demoTimer = null;
const demoTimeouts = new Set();
let connectionStatus = {
    state: 'idle',
    username: null,
    message: 'Chưa kết nối'
};
let metrics = createMetrics();
let masterConfig = sanitizeMasterConfig(initialMasterConfig);
const recentEventIds = new Map();
const sessionPlayers = new Map();
const sessionVipScores = new Map();
const observedGifts = new Map((Array.isArray(initialObservedGifts) ? initialObservedGifts : [])
    .map(gift => [String(gift.giftId || gift.giftName || ''), gift])
    .filter(([key]) => key));
let observedGiftSaveTimer = null;

if (!ALLOW_LAN && !isLoopbackAddress(HOST) && HOST.toLowerCase() !== 'localhost') {
    throw new Error('Từ chối mở Node ra mạng LAN. Chỉ đặt ALLOW_LAN=1 khi đã có lớp xác thực bên ngoài.');
}

function createMetrics() {
    return {
        source: 'idle',
        events: 0,
        members: 0,
        chats: 0,
        gifts: 0,
        diamonds: 0,
        likes: 0,
        startedAt: null
    };
}

app.disable('x-powered-by');
app.use((req, res, next) => {
    if (!isAllowedHost(req.headers.host, PORT, ALLOW_LAN)) {
        return res.status(403).type('text/plain').send('Forbidden');
    }
    res.setHeader('X-Content-Type-Options', 'nosniff');
    res.setHeader('X-Frame-Options', 'DENY');
    res.setHeader('Referrer-Policy', 'no-referrer');
    res.setHeader('Permissions-Policy', 'camera=(), microphone=(), geolocation=(), payment=()');
    res.setHeader('Cross-Origin-Resource-Policy', 'same-origin');
    res.setHeader('Content-Security-Policy', [
        "default-src 'self'",
        "base-uri 'none'",
        "object-src 'none'",
        "frame-ancestors 'none'",
        "form-action 'self'",
        "script-src 'self' 'unsafe-inline'",
        "style-src 'self' 'unsafe-inline'",
        "img-src 'self' data: blob: https:",
        "media-src 'self' blob:",
        `connect-src 'self' ws://127.0.0.1:${PORT} ws://localhost:${PORT}`
    ].join('; '));
    if (req.path.startsWith('/api/') || req.path.endsWith('.html')) {
        res.setHeader('Cache-Control', 'no-store');
    }
    next();
});

const staticOptions = { dotfiles: 'deny', fallthrough: true, etag: true, index: ['index.html'] };
app.get('/', (_req, res) => res.redirect('/control.html'));
app.use(express.static(publicDir, staticOptions));
app.use('/assets', express.static(assetsDir, staticOptions));
app.use('/vendor/three', express.static(path.join(__dirname, 'node_modules', 'three', 'build'), staticOptions));

app.get('/api/config', (_req, res) => {
    res.json({ game: gameConfig, gifts: giftConfig, master: masterConfig, observedGifts: [...observedGifts.values()] });
});

app.get('/api/gifs', async (_req, res) => {
    try {
        const files = await fs.readdir(gifsDir);
        res.json(files.filter(file => file.toLowerCase().endsWith('.gif')));
    } catch (error) {
        if (error.code === 'ENOENT') return res.json([]);
        console.error('Không thể đọc thư mục GIF:', error);
        res.status(500).json({ error: 'Không thể đọc danh sách GIF' });
    }
});

app.use((_req, res) => res.status(404).type('text/plain').send('Not found'));

function broadcast(data) {
    const payload = JSON.stringify(data);
    for (const client of wss.clients) {
        if (client.readyState !== WebSocket.OPEN) continue;
        if (client.bufferedAmount > 512 * 1024) {
            client.terminate();
            continue;
        }
        client.send(payload);
    }
}

function send(ws, data) {
    if (ws.readyState !== WebSocket.OPEN) return;
    if (ws.bufferedAmount > 512 * 1024) return ws.terminate();
    ws.send(JSON.stringify(data));
}

function setStatus(state, username, message) {
    connectionStatus = { state, username, message };
    broadcast({ type: 'status', ...connectionStatus });
}

function broadcastMetrics() {
    broadcast({ type: 'metrics', ...metrics });
}

function giftCatalogMessage() {
    return {
        type: 'gift_catalog',
        gifts: [...observedGifts.values()].sort((a, b) =>
            (Number(a.diamondCount) || 0) - (Number(b.diamondCount) || 0) ||
            String(a.giftName || '').localeCompare(String(b.giftName || '')))
    };
}

function viewerGuideMessage() {
    const joinRule = (masterConfig.rules || []).find(rule => rule.enabled && rule.source === 'chat' && rule.action === 'join');
    const giftRules = (masterConfig.rules || [])
        .filter(rule => rule.enabled && rule.source === 'gift' &&
            (Number(rule.displayDiamonds) > 0 || (rule.giftId && observedGifts.has(String(rule.giftId)))));
    const actionOrder = new Map([['walk', 0], ['medal', 1], ['fireworks', 2], ['grow', 3], ['topdj', 4]]);
    const items = giftRules
        .filter(rule => actionOrder.has(rule.action))
        .sort((a, b) => actionOrder.get(a.action) - actionOrder.get(b.action))
        .slice(0, 5)
        .map(rule => {
            const learned = rule.giftId ? observedGifts.get(String(rule.giftId)) : null;
            return {
                giftId: rule.giftId || learned?.giftId || '',
                giftName: learned?.giftName || String(rule.trigger || '').split(',')[0].trim() || 'Gift',
                diamondCount: Number(learned?.diamondCount) || Number(rule.displayDiamonds) || 0,
                giftPictureUrl: learned?.giftPictureUrl || '',
                label: rule.label || rule.action.toUpperCase()
            };
        });
    const zoomRule = giftRules.find(rule => rule.action === 'camera');
    const zoomLearned = zoomRule?.giftId ? observedGifts.get(String(zoomRule.giftId)) : null;
    return {
        type: 'viewer_guide',
        joinCommand: joinRule ? String(joinRule.trigger || '').split(',')[0].trim() : '',
        zoomGiftName: zoomLearned?.giftName || (zoomRule ? String(zoomRule.trigger || '').split(',')[0].trim() : ''),
        guideItems: items
    };
}

function scheduleObservedGiftSave() {
    clearTimeout(observedGiftSaveTimer);
    observedGiftSaveTimer = setTimeout(() => {
        observedGiftSaveTimer = null;
        fs.writeFile(observedGiftsPath, `${JSON.stringify([...observedGifts.values()], null, 2)}\n`, 'utf8')
            .catch(error => console.error('Không thể lưu thư viện gift:', error));
    }, 250);
}

function learnObservedGift(event) {
    const giftId = String(event.giftId || '').trim();
    const giftName = String(event.giftName || 'Gift').trim();
    const key = giftId || giftName.toLocaleLowerCase('vi');
    if (!key) return;
    const previous = observedGifts.get(key) || {};
    const repeats = Math.max(1, Number(event.repeatCount) || 1);
    const unitDiamonds = Math.max(0,
        Number(event.unitDiamondCount) || Math.round((Number(event.diamondCount) || 0) / repeats));
    const learnedGift = {
        ...previous,
        giftId,
        giftName,
        diamondCount: unitDiamonds || Number(previous.diamondCount) || 0,
        giftPictureUrl: nonEmpty(event.giftPictureUrl, previous.giftPictureUrl || ''),
        lastSeenAt: Date.now()
    };
    observedGifts.set(key, learnedGift);
    if (observedGifts.size > 500) {
        const oldest = [...observedGifts.entries()]
            .sort((a, b) => (Number(a[1].lastSeenAt) || 0) - (Number(b[1].lastSeenAt) || 0));
        for (let index = 0; index < oldest.length - 500; index += 1) observedGifts.delete(oldest[index][0]);
    }
    let masterChanged = false;
    for (const rule of masterConfig.rules || []) {
        if (!giftId) break;
        if (rule.source !== 'gift' || rule.giftId) continue;
        const aliases = String(rule.trigger || '').split(',').map(normalizeText).filter(Boolean);
        if (!aliases.includes(normalizeText(giftName))) continue;
        rule.giftId = giftId;
        rule.displayDiamonds = unitDiamonds || rule.displayDiamonds || 0;
        masterChanged = true;
        break;
    }
    if (masterChanged) {
        fs.writeFile(masterConfigPath, `${JSON.stringify(masterConfig, null, 2)}\n`, 'utf8')
            .catch(error => console.error('Không thể tự gắn Gift ID vào Master:', error));
        broadcast({ type: 'master_config', master: masterConfig });
    }
    scheduleObservedGiftSave();
    broadcast(giftCatalogMessage());
    broadcast(viewerGuideMessage());
}

function isRealObservedGift(event) {
    const giftId = String(event.giftId || '');
    return event.userId !== 'master-test' && !giftId.startsWith('demo-') && !giftId.startsWith('master-');
}

function resetSessionState(source = metrics.source) {
    metrics = { ...createMetrics(), source, startedAt: source === 'idle' ? null : Date.now() };
    sessionPlayers.clear();
    sessionVipScores.clear();
    recentEventIds.clear();
}

function createSnapshot() {
    pruneSessionPlayers();
    return {
        type: 'snapshot',
        players: [...sessionPlayers.values()],
        vipScores: [...sessionVipScores.values()]
    };
}

function pruneSessionPlayers(now = Date.now()) {
    const cutoff = now - Math.max(1000, Number(gameConfig.playerTtlMs) || 600000);
    for (const [userId, player] of sessionPlayers) {
        if ((Number(player.lastActive) || 0) < cutoff) sessionPlayers.delete(userId);
    }
    const maximum = Math.max(1, Number(gameConfig.maxPlayers) || 200);
    if (sessionPlayers.size <= maximum) return;
    const oldest = [...sessionPlayers.values()]
        .sort((a, b) => (Number(a.lastActive) || 0) - (Number(b.lastActive) || 0));
    for (let index = 0; index < oldest.length - maximum; index += 1) {
        sessionPlayers.delete(oldest[index].userId);
    }
}

function normalizeUsername(value) {
    if (typeof value !== 'string') return null;
    const username = value.trim().replace(/^@/, '');
    return /^[A-Za-z0-9._]{2,24}$/.test(username) ? username : null;
}

function nonEmpty(value, fallback = '') {
    return typeof value === 'string' && value.trim() ? value.trim() : fallback;
}

function dedupeKey(event) {
    if (event.eventId) return `id:${event.eventId}`;
    return `fallback:${[
        event.type,
        event.userId,
        event.giftId,
        event.giftName,
        event.repeatCount,
        event.diamondCount,
        event.likeCount,
        event.comment
    ].map(value => String(value ?? '')).join('|')}`;
}

function isDuplicate(event) {
    const now = Date.now();
    const key = dedupeKey(event);
    const previous = recentEventIds.get(key);
    const retentionMs = event.eventId ? 10 * 60 * 1000 : 2500;
    if (previous && now - previous < retentionMs) return true;
    recentEventIds.set(key, now);
    if (recentEventIds.size > 2000) {
        for (const [id, timestamp] of recentEventIds) {
            if (now - timestamp > 10 * 60 * 1000) recentEventIds.delete(id);
        }
        while (recentEventIds.size > 2000) recentEventIds.delete(recentEventIds.keys().next().value);
    }
    return false;
}

function resolveGiftRule(event) {
    const exact = giftConfig.byGiftId[String(event.giftId)];
    if (exact) return exact;
    const giftName = normalizeGiftName(event.giftName);
    const named = (giftConfig.byGiftName || []).find(rule =>
        (rule.aliases || []).some(alias => normalizeGiftName(alias) === giftName)
    );
    if (named) return named;
    const value = Number(event.diamondCount) || 0;
    return [...giftConfig.diamondBands]
        .sort((a, b) => b.minimum - a.minimum)
        .find(rule => value >= rule.minimum);
}

function normalizeGiftName(value) {
    return String(value || '')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .trim()
        .toLocaleLowerCase('vi');
}

function emitGameEvent(event) {
    if (isDuplicate(event)) return;
    const now = Date.now();
    let giftRule = null;
    if (event.type === 'gift') {
        giftRule = event.masterRuleId ? null : resolveGiftRule(event);
        if (giftRule) {
            event.action = giftRule.action;
            event.durationMs = giftRule.durationMs;
            event.label = giftRule.label;
            event.variant = giftRule.variant;
            event.fireworkBursts = giftRule.fireworkBursts;
        }
    }
    if (event.userId && !event.spectatorOnly) {
        const previous = sessionPlayers.get(event.userId) || {};
        const playerState = {
            ...previous,
            userId: event.userId,
            uniqueId: nonEmpty(event.uniqueId, previous.uniqueId || event.userId),
            nickname: nonEmpty(event.nickname, previous.nickname || event.uniqueId || 'TikTok user'),
            avatar: nonEmpty(event.avatar, previous.avatar || ''),
            lastActive: now
        };
        if (event.type === 'gift') {
            playerState.giftPower =
                Math.max(0, Number(previous.giftPower) || 0) +
                Math.max(0, Number(event.diamondCount) || 0);
            if (event.action === 'medal') {
                playerState.titleLabel = event.label;
                playerState.titleVariant = event.variant || 'fire';
                playerState.titleExpiresAt = now + Math.max(3000, Number(event.durationMs) || 60000);
            }
        }
        sessionPlayers.set(event.userId, playerState);
        pruneSessionPlayers(now);
    }
    metrics.events += 1;
    if (event.type === 'member') metrics.members += 1;
    if (event.type === 'chat') metrics.chats += 1;
    if (event.type === 'gift') {
        metrics.gifts += 1;
        metrics.diamonds += event.diamondCount;
        const vip = sessionVipScores.get(event.userId) || {
            userId: event.userId,
            nickname: event.nickname,
            avatar: event.avatar,
            score: 0
        };
        vip.nickname = nonEmpty(event.nickname, vip.nickname || 'TikTok user');
        vip.avatar = nonEmpty(event.avatar, vip.avatar || '');
        vip.score += event.diamondCount;
        sessionVipScores.set(event.userId, vip);
    }
    if (event.type === 'like') metrics.likes += event.likeCount;
    broadcast(event);
    broadcastMetrics();
}

function processGameEvent(inputEvent) {
    const safeEvent = sanitizeGameEvent(inputEvent);
    if (!safeEvent) return;
    const event = applyBuiltInChatCommand(applyRule(safeEvent, resolveMasterRule(masterConfig, safeEvent)));
    const isKnownPlayer = event.userId && sessionPlayers.has(event.userId);
    const joinsByKeyword = event.type === 'chat' && event.action === 'join';
    const joinsBySocial = event.type === 'follow' || event.type === 'share';
    event.joinedNow = Boolean((joinsByKeyword || joinsBySocial) && !isKnownPlayer);
    if (metrics.source !== 'demo' && masterConfig.joinMode === 'keyword_only' && !isKnownPlayer) {
        const joinsByGift = event.type === 'gift' && masterConfig.giftAlwaysJoins;
        event.spectatorOnly = !(joinsByKeyword || joinsByGift || joinsBySocial);
    }
    if (event.type === 'gift') {
        if (isRealObservedGift(event)) {
            learnObservedGift(event);
            broadcast({
                type: 'gift_observed',
                giftId: event.giftId,
                giftName: event.giftName,
                diamondCount: event.unitDiamondCount || Math.round(event.diamondCount / Math.max(1, event.repeatCount || 1)),
                giftPictureUrl: event.giftPictureUrl || ''
            });
        }
    }
    emitGameEvent(event);
}

function stopDemo() {
    if (demoTimer) clearInterval(demoTimer);
    demoTimer = null;
    for (const timeout of demoTimeouts) clearTimeout(timeout);
    demoTimeouts.clear();
}

function mockUser(index, manualName = null) {
    return {
        userId: manualName ? manualName : `demo-${index}`,
        uniqueId: manualName ? manualName : `dancer_${index}`,
        nickname: manualName ? manualName : `Dancer ${index}`,
        avatar: ''
    };
}

function emitDemoMember(index, manualName = null) {
    processGameEvent({
        type: 'member',
        action: 'join',
        joinedNow: true,
        eventId: `demo-join-${Date.now()}-${Math.random()}`,
        ...mockUser(index, manualName)
    });
}

function emitDemoAction(action, userIndex = 1, value = 1, giftName = '', manualName = null) {
    const user = mockUser(userIndex, manualName);
    const eventId = `demo-${action}-${Date.now()}-${Math.random()}`;
    if (action === 'member') return emitDemoMember(userIndex, manualName);
    if (action === 'dance') {
        return processGameEvent({ type: 'chat', eventId, ...user, comment: 'dance' });
    }
    if (action === 'jump') {
        return processGameEvent({ type: 'chat', eventId, ...user, comment: 'jump' });
    }
    if (action === 'walk') {
        return processGameEvent({ type: 'chat', eventId, ...user, comment: 'đi vòng' });
    }
    if (action === 'change') {
        return processGameEvent({ type: 'chat', eventId, ...user, comment: 'đổi nv' });
    }
    if (action === 'like') {
        return processGameEvent({ type: 'like', eventId, ...user, likeCount: value });
    }
    if (action === 'follow' || action === 'share') {
        return processGameEvent({ type: action, eventId, ...user });
    }
    if (action === 'gift') {
        const demoGiftNames = {
            1: 'Rose',
            5: 'Dance Pop',
            20: 'Camera Star',
            50: 'Fire Crown',
            100: 'VIP Gift',
            200: 'Super VIP',
            500: 'Firework Rain',
            1000: 'Party Universe'
        };
        return processGameEvent({
            type: 'gift',
            eventId,
            ...user,
            giftId: `demo-${value}`,
            giftName: giftName || demoGiftNames[value] || (value >= 100 ? 'Fireworks' : value >= 10 ? 'VIP Gift' : 'Rose'),
            repeatCount: 1,
            diamondCount: value
        });
    }
}

function startDemo(count) {
    stopDemo();
    resetSessionState('demo');
    setStatus('demo', null, `Chế độ demo: ${count} người`);
    broadcast({ type: 'reset' });

    for (let index = 1; index <= count; index += 1) {
        const timeout = setTimeout(() => {
            demoTimeouts.delete(timeout);
            emitDemoMember(index);
        }, index * 35);
        demoTimeouts.add(timeout);
    }

    demoTimer = setInterval(() => {
        const userIndex = 1 + Math.floor(Math.random() * count);
        const roll = Math.random();
        if (roll < 0.45) emitDemoAction('dance', userIndex);
        else if (roll < 0.65) emitDemoAction('walk', userIndex);
        else if (roll < 0.78) emitDemoAction('change', userIndex);
        else if (roll < 0.93) emitDemoAction('like', userIndex, 10);
        else emitDemoAction('gift', userIndex, Math.random() < 0.2 ? 100 : 10);
    }, 700);
}

async function disconnectCurrentConnection() {
    const previous = liveConnection;
    liveConnection = null;
    if (!previous) return;
    try {
        if (typeof previous.disconnect === 'function') await previous.disconnect();
        else if (typeof previous.close === 'function') previous.close();
    } catch (error) {
        console.warn('Không thể đóng kết nối TikTok cũ:', error.message);
    }
}

function cancelReconnect() {
    if (reconnectTimer) clearTimeout(reconnectTimer);
    reconnectTimer = null;
}

function scheduleReconnect(username) {
    if (!username || desiredUsername !== username || reconnectTimer) return;
    const delayMs = Math.min(30000, 2000 * (2 ** Math.min(reconnectFailures, 4)));
    reconnectFailures += 1;
    setStatus(
        'reconnecting',
        username,
        LIVE_PROVIDER === 'tikfinity'
            ? `Chưa thấy TikFinity Desktop. Tự thử lại sau ${Math.ceil(delayMs / 1000)} giây...`
            : `Mất kết nối TikTok. Tự kết nối lại sau ${Math.ceil(delayMs / 1000)} giây...`
    );
    reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        if (desiredUsername === username) {
            void connectToLiveProvider(username, { resetSession: false, isReconnect: true });
        }
    }, delayMs);
}

async function connectToTikFinity(username, options = {}) {
    const resetSession = options.resetSession !== false;
    const isReconnect = options.isReconnect === true;
    stopDemo();
    cancelReconnect();
    desiredUsername = username;
    const attempt = ++connectionAttempt;
    await disconnectCurrentConnection();

    if (resetSession) {
        resetSessionState('tiktok');
        broadcast({ type: 'reset' });
        broadcastMetrics();
    }
    setStatus(
        isReconnect ? 'reconnecting' : 'connecting',
        username,
        isReconnect ? 'Đang kết nối lại TikFinity Desktop...' : 'Đang kết nối TikFinity Desktop...'
    );

    const connection = new WebSocket(TIKFINITY_WS_URL);
    liveConnection = connection;
    const active = () => attempt === connectionAttempt && liveConnection === connection;

    connection.on('open', () => {
        if (!active()) return;
        reconnectFailures = 0;
        setStatus('connected', username, `Đã kết nối TikFinity cho @${username}`);
        broadcastMetrics();
        console.log(`Đã kết nối TikFinity tại ${TIKFINITY_WS_URL}`);
    });
    connection.on('message', payload => {
        if (!active()) return;
        for (const event of normalizeTikFinityMessage(payload)) processGameEvent(event);
    });
    connection.on('error', error => {
        console.warn(`TikFinity WebSocket: ${error.message}`);
    });
    connection.on('close', () => {
        if (!active()) return;
        liveConnection = null;
        scheduleReconnect(username);
    });
}

function connectToLiveProvider(username, options = {}) {
    return LIVE_PROVIDER === 'tikfinity'
        ? connectToTikFinity(username, options)
        : connectToTikTok(username, options);
}

function attachTikTokEvents(connection) {
    const active = () => liveConnection === connection;
    // EventEmitter treats an unhandled `error` event as fatal. Keep the bridge
    // alive and let connect/reconnect report the connection state instead.
    connection.on('error', error => {
        console.warn('TikTok connection error:', error?.message || error);
    });
    connection.on('member', data => active() && processGameEvent(normalizeMember(data)));
    connection.on('chat', data => active() && processGameEvent(normalizeChat(data)));
    connection.on('like', data => active() && processGameEvent(normalizeLike(data)));
    connection.on('follow', data => active() && processGameEvent(normalizeSocial('follow', data)));
    connection.on('share', data => active() && processGameEvent(normalizeSocial('share', data)));
    connection.on('gift', data => {
        if (!active()) return;
        const event = normalizeGift(data);
        if (!isPendingGiftStreak(event)) processGameEvent(event);
    });
}

async function connectToTikTok(username, options = {}) {
    const resetSession = options.resetSession !== false;
    const isReconnect = options.isReconnect === true;
    stopDemo();
    cancelReconnect();
    desiredUsername = username;
    const attempt = ++connectionAttempt;
    await disconnectCurrentConnection();

    if (resetSession) {
        resetSessionState('tiktok');
        broadcast({ type: 'reset' });
        broadcastMetrics();
    }
    setStatus(
        isReconnect ? 'reconnecting' : 'connecting',
        username,
        isReconnect ? `Đang kết nối lại @${username}...` : `Đang kết nối @${username}...`
    );
    let connection;
    try {
        connection = new TikTokLiveConnection(username, {
            signApiKey: process.env.EULER_API_KEY || undefined,
            processInitialData: false,
            enableExtendedGiftInfo: false
        });
    } catch (error) {
        liveConnection = null;
        console.error(`Không thể tạo kết nối @${username}:`, error.message);
        setStatus('error', username, `Không thể khởi tạo kết nối TikTok: ${error.message}`);
        return;
    }
    liveConnection = connection;
    attachTikTokEvents(connection);

    connection.on('streamEnd', () => {
        if (liveConnection !== connection) return;
        liveConnection = null;
        desiredUsername = null;
        cancelReconnect();
        setStatus('ended', username, `Live @${username} đã kết thúc`);
    });
    connection.on('disconnected', () => {
        if (liveConnection !== connection) return;
        liveConnection = null;
        scheduleReconnect(username);
    });

    try {
        const state = await connection.connect();
        if (attempt !== connectionAttempt || liveConnection !== connection) {
            await connection.disconnect().catch(() => {});
            return;
        }
        reconnectFailures = 0;
        setStatus('connected', username, `Đã kết nối @${username}`);
        broadcastMetrics();
        console.log(`Đã kết nối @${username}, room ${state.roomId}`);
    } catch (error) {
        if (attempt !== connectionAttempt) return;
        liveConnection = null;
        console.error(`Không thể kết nối @${username}:`, error.message);
        if (desiredUsername === username) scheduleReconnect(username);
    }
}

async function handleClientMessage(ws, message) {
    if (message.type === 'ping') {
        return send(ws, { type: 'pong', timestamp: Date.now() });
    }

    if (message.type === 'register') {
        if (ws.registered) return send(ws, { type: 'error', message: 'Client đã đăng ký quyền.' });
        if (message.role !== 'control' && message.role !== 'overlay') {
            return send(ws, { type: 'error', message: 'Vai trò client không hợp lệ.' });
        }
        ws.role = message.role;
        ws.registered = true;
        send(ws, { type: 'config', game: gameConfig, gifts: giftConfig });
        send(ws, { type: 'status', ...connectionStatus });
        send(ws, { type: 'metrics', ...metrics });
        if (ws.role === 'control') send(ws, { type: 'master_config', master: masterConfig });
        if (ws.role === 'control') send(ws, giftCatalogMessage());
        if (ws.role === 'overlay') {
            send(ws, createSnapshot());
            send(ws, viewerGuideMessage());
        }
        return;
    }

    if (!ws.registered) return send(ws, { type: 'error', message: 'Client chưa đăng ký quyền.' });

    const controlOnly = message.type === 'master_save' || message.type === 'master_test';
    const operatorOnly = new Set([
        'set_username', 'disconnect_tiktok', 'demo_start', 'demo_stop', 'demo_event', 'reset_game'
    ]).has(message.type);
    if (controlOnly && ws.role !== 'control') {
        return send(ws, { type: 'error', message: 'Lệnh này chỉ dành cho bảng Master.' });
    }
    if (operatorOnly && ws.role !== 'control' && !ws.nativeClient) {
        return send(ws, { type: 'error', message: 'Client không có quyền điều khiển.' });
    }

    if (message.type === 'master_save') {
        masterConfig = sanitizeMasterConfig(message.master);
        await fs.writeFile(masterConfigPath, `${JSON.stringify(masterConfig, null, 2)}\n`, 'utf8');
        broadcast({ type: 'master_config', master: masterConfig });
        broadcast(viewerGuideMessage());
        return send(ws, { type: 'master_saved', message: 'Đã lưu và áp dụng Master.' });
    }

    if (message.type === 'master_test') {
        const rule = masterConfig.rules.find(item => item.id === message.ruleId && item.enabled);
        if (!rule) return send(ws, { type: 'error', message: 'Không tìm thấy luật Master để test.' });
        const user = { userId: 'master-test', uniqueId: 'master_test', nickname: 'Master Test', avatar: '' };
        if (rule.source === 'chat') {
            return processGameEvent({
                type: 'chat',
                eventId: `master-test-${Date.now()}`,
                ...user,
                comment: rule.trigger.split(',')[0].trim()
            });
        }
        return processGameEvent({
            type: 'gift',
            eventId: `master-test-${Date.now()}`,
            ...user,
            giftId: rule.giftId || `master-${rule.id}`,
            giftName: rule.trigger.split(',')[0].trim() || 'Master Gift',
            repeatCount: 1,
            diamondCount: Math.max(1, Number(message.diamonds) || 1)
        });
    }

    if (message.type === 'set_username') {
        const username = normalizeUsername(message.username);
        if (!username) {
            return send(ws, {
                type: 'error',
                message: 'Username chỉ được gồm chữ, số, dấu chấm hoặc gạch dưới.'
            });
        }
        return connectToLiveProvider(username);
    }

    if (message.type === 'disconnect_tiktok') {
        desiredUsername = null;
        reconnectFailures = 0;
        cancelReconnect();
        connectionAttempt += 1;
        await disconnectCurrentConnection();
        setStatus('idle', null, 'Đã ngắt kết nối');
        return;
    }

    if (message.type === 'demo_start') {
        desiredUsername = null;
        reconnectFailures = 0;
        cancelReconnect();
        connectionAttempt += 1;
        await disconnectCurrentConnection();
        const count = Math.min(gameConfig.maxPlayers, Math.max(1, Number(message.count) || 20));
        return startDemo(count);
    }
    if (message.type === 'demo_stop') {
        stopDemo();
        setStatus('idle', null, 'Đã dừng demo');
        return;
    }
    if (message.type === 'demo_event') {
        return emitDemoAction(
            String(message.action || 'dance'),
            Math.max(1, Number(message.userIndex) || 1),
            Math.max(1, Number(message.value) || 1),
            String(message.giftName || ''),
            message.manualUsername || null
        );
    }
    if (message.type === 'reset_game') {
        const source = metrics.source;
        const startedAt = metrics.startedAt;
        resetSessionState(source);
        metrics.startedAt = startedAt;
        broadcast({ type: 'reset' });
        broadcastMetrics();
        return;
    }

    send(ws, { type: 'error', message: 'Lệnh không được hỗ trợ.' });
}

function rejectUpgrade(socket, status = '403 Forbidden') {
    socket.write(`HTTP/1.1 ${status}\r\nConnection: close\r\nContent-Length: 0\r\n\r\n`);
    socket.destroy();
}

server.on('upgrade', (request, socket, head) => {
    const remoteAddress = request.socket.remoteAddress;
    const origin = request.headers.origin || '';
    if ((!ALLOW_LAN && !isLoopbackAddress(remoteAddress)) ||
        !isAllowedHost(request.headers.host, PORT, ALLOW_LAN) ||
        !isAllowedOrigin(origin, PORT)) {
        return rejectUpgrade(socket);
    }
    wss.handleUpgrade(request, socket, head, ws => wss.emit('connection', ws, request));
});

wss.on('connection', (ws, request) => {
    ws.registered = false;
    ws.role = null;
    ws.nativeClient = !request.headers.origin && isLoopbackAddress(request.socket.remoteAddress);
    ws.rateLimit = { windowStartedAt: Date.now(), count: 0 };
    ws.invalidMessages = 0;
    ws.isAlive = true;
    ws.on('pong', () => { ws.isAlive = true; });
    send(ws, { type: 'status', ...connectionStatus });
    ws.on('message', (rawMessage, isBinary) => {
        if (isBinary) return ws.close(1003, 'Text messages only');
        if (!consumeRateLimit(ws.rateLimit)) return ws.close(1008, 'Rate limit exceeded');
        let message;
        try {
            message = JSON.parse(rawMessage.toString());
        } catch {
            ws.invalidMessages += 1;
            if (ws.invalidMessages >= 3) return ws.close(1008, 'Invalid messages');
            return send(ws, { type: 'error', message: 'Dữ liệu không hợp lệ' });
        }
        if (!message || typeof message !== 'object' || Array.isArray(message) ||
            typeof message.type !== 'string' || message.type.length > 40) {
            return send(ws, { type: 'error', message: 'Cấu trúc lệnh không hợp lệ.' });
        }
        void handleClientMessage(ws, message).catch(error => {
            console.error('Không thể xử lý lệnh WebSocket:', error?.message || error);
            send(ws, { type: 'error', message: 'Không thể xử lý lệnh.' });
        });
    });
    ws.on('error', error => console.warn('WebSocket client error:', error.message));
});

const heartbeatTimer = setInterval(() => {
    for (const ws of wss.clients) {
        if (!ws.isAlive) {
            ws.terminate();
            continue;
        }
        ws.isAlive = false;
        ws.ping();
    }
}, 30000);
heartbeatTimer.unref();

server.requestTimeout = 10000;
server.headersTimeout = 5000;
server.keepAliveTimeout = 5000;
server.maxHeadersCount = 64;

server.listen(PORT, HOST, () => {
    console.log(`TikTok Live Game: http://${HOST}:${PORT}`);
    console.log(`Bảng điều khiển: http://${HOST}:${PORT}/control.html`);
});

server.on('error', (err) => {
    if (err.code === 'EADDRINUSE') {
        console.error(`\n❌ Lỗi: Cổng ${PORT} đang bị chương trình khác chiếm.`);
        console.error(`   Có thể bạn đã chạy server trước đó mà chưa tắt.\n`);
        console.error(`   Cách khắc phục:`);
        console.error(`   1. Tắt cửa sổ cmd/terminal cũ đang chạy server`);
        console.error(`   2. Hoặc chạy lệnh: npx kill-port ${PORT}`);
        console.error(`   3. Hoặc đổi PORT trong file .env sang số khác (vd: 3001)\n`);
        process.exit(1);
    } else {
        console.error('Lỗi server:', err);
        process.exit(1);
    }
});

async function shutdown() {
    stopDemo();
    clearInterval(heartbeatTimer);
    desiredUsername = null;
    cancelReconnect();
    connectionAttempt += 1;
    await disconnectCurrentConnection();
    for (const ws of wss.clients) ws.terminate();
    wss.close();
    server.close(() => process.exit(0));
}

process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
