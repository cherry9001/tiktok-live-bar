const crypto = require('node:crypto');

const SOURCES = new Set(['chat', 'gift']);
const ACTIONS = new Set(['join', 'dance', 'camera', 'change', 'walk', 'grow', 'medal', 'vip', 'topdj', 'fireworks']);

function normalizeText(value) {
    return String(value || '')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .trim()
        .toLocaleLowerCase('vi');
}

function splitTriggers(value) {
    return String(value || '').split(',').map(normalizeText).filter(Boolean).slice(0, 20);
}

function sanitizeRule(input = {}) {
    const generatedId = crypto.randomUUID();
    return {
        id: String(input.id || generatedId).replace(/[^a-zA-Z0-9_-]/g, '').slice(0, 64) || generatedId,
        enabled: input.enabled !== false,
        source: SOURCES.has(input.source) ? input.source : 'gift',
        trigger: String(input.trigger || '').trim().slice(0, 300),
        giftId: String(input.giftId || '').trim().slice(0, 80),
        match: input.match === 'contains' ? 'contains' : 'exact',
        action: ACTIONS.has(input.action) ? input.action : 'dance',
        displayDiamonds: Math.max(0, Math.min(1000000, Number(input.displayDiamonds) || 0)),
        durationMs: Math.max(0, Math.min(300000, Number(input.durationMs) || 0)),
        label: String(input.label || '').trim().slice(0, 80),
        variant: ['aura', 'neon', 'fire', 'royal'].includes(input.variant) ? input.variant : '',
        fireworkBursts: Math.max(0, Math.min(12, Number(input.fireworkBursts) || 0))
    };
}

function sanitizeMasterConfig(input = {}) {
    return {
        joinMode: input.joinMode === 'all_interactions' ? 'all_interactions' : 'keyword_only',
        giftAlwaysJoins: input.giftAlwaysJoins !== false,
        rules: Array.isArray(input.rules) ? input.rules.slice(0, 100).map(sanitizeRule) : []
    };
}

function matchesRule(rule, event) {
    if (!rule.enabled || rule.source !== event.type) return false;
    if (rule.source === 'gift' && rule.giftId && String(event.giftId || '') === rule.giftId) return true;
    const value = normalizeText(rule.source === 'chat' ? event.comment : event.giftName);
    if (!value) return false;
    return splitTriggers(rule.trigger).some(trigger => rule.match === 'contains' ? value.includes(trigger) : value === trigger);
}

function resolveMasterRule(config, event) {
    if (event.type === 'gift' && event.giftId) {
        const byId = (config.rules || []).find(rule =>
            rule.enabled && rule.source === 'gift' && rule.giftId === String(event.giftId));
        if (byId) return byId;
    }
    return (config.rules || []).find(rule => matchesRule(rule, event)) || null;
}

function applyRule(event, rule) {
    if (!rule) return { ...event };
    return {
        ...event,
        action: rule.action,
        durationMs: rule.durationMs,
        label: rule.label,
        variant: rule.variant,
        fireworkBursts: rule.fireworkBursts,
        masterRuleId: rule.id
    };
}

function applyBuiltInChatCommand(event) {
    if (event.type !== 'chat') return event;
    const command = normalizeText(event.comment);
    if (command !== 'jump' && command !== 'nhay') return event;
    return { ...event, action: 'jump', durationMs: 950, masterRuleId: '' };
}

module.exports = { normalizeText, sanitizeMasterConfig, resolveMasterRule, applyRule, applyBuiltInChatCommand };
