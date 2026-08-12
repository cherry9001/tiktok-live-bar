let generatedEventSequence = 0;

function firstText(...values) {
    for (const value of values) {
        if (typeof value === 'string' && value.trim()) return value.trim();
        if (typeof value === 'number' && Number.isFinite(value)) return String(value);
    }
    return '';
}

function eventType(raw = {}) {
    return firstText(raw.event, raw.eventType, raw.type, raw.name)
        .toLowerCase()
        .replace(/[^a-z]/g, '');
}

function avatarUrl(user = {}, data = {}) {
    const candidates = [
        user.userDetails?.profilePictureUrls,
        data.userDetails?.profilePictureUrls,
        user.profilePictureUrls,
        data.profilePictureUrls,
        user.profilePictureUrlHD,
        user.profilePictureUrl,
        user.avatarUrl,
        user.avatar,
        user.picture,
        data.profilePictureUrl,
        data.avatarUrl,
        data.avatar
    ].flatMap(value => Array.isArray(value) ? value : [value])
        .filter(value => typeof value === 'string' && /^https?:\/\//i.test(value));

    // UnityWebRequestTexture reliably decodes JPEG/PNG but not TikTok's WebP
    // avatar response, so prefer the JPEG variant TikFinity includes.
    return candidates.find(value => /\.(?:jpe?g|png)(?:[?~]|$)/i.test(value)) || candidates[0] || '';
}

function giftPictureUrl(data = {}, gift = {}) {
    const candidates = [
        data.giftPictureUrl,
        gift.giftPictureUrl,
        gift.imageUrl,
        gift.pictureUrl,
        gift.giftImage?.url,
        gift.image?.url,
        data.extendedGiftInfo?.image?.url,
        data.extendedGiftInfo?.pictureUrl
    ].flatMap(value => Array.isArray(value) ? value : [value]);
    return candidates.find(value => typeof value === 'string' && /^https?:\/\//i.test(value)) || '';
}

function normalizeUser(data = {}) {
    const user = data.user || data.userData || data.author || data;
    const uniqueId = firstText(user.uniqueId, user.unique_id, user.username, data.uniqueId, data.username);
    const nickname = firstText(user.nickname, user.displayName, user.name, data.nickname, uniqueId, 'TikTok user');
    const userId = firstText(user.userId, user.user_id, user.id, data.userId, data.user_id, uniqueId, nickname);
    const avatar = avatarUrl(user, data);
    return { userId, uniqueId: uniqueId || userId, nickname, avatar };
}

function normalizeOne(raw) {
    if (!raw || typeof raw !== 'object') return null;
    const data = raw.data && typeof raw.data === 'object' && !Array.isArray(raw.data) ? raw.data : raw;
    const typeKey = eventType(raw) || eventType(data);
    const aliases = {
        chat: 'chat', comment: 'chat', message: 'chat',
        member: 'member', join: 'member', viewerjoin: 'member',
        gift: 'gift', like: 'like', follow: 'follow', share: 'share'
    };
    const type = aliases[typeKey];
    if (!type) return null;

    const id = firstText(raw.eventId, raw.msgId, raw.id, data.eventId, data.msgId, data.id) ||
        `tikfinity-${Date.now()}-${++generatedEventSequence}`;
    const base = { type, eventId: id, ...normalizeUser(data) };
    if (type === 'chat') {
        return { ...base, comment: firstText(data.comment, data.message, data.text) };
    }
    if (type === 'like') {
        return {
            ...base,
            likeCount: Math.max(1, Number(data.likeCount ?? data.count) || 1),
            totalLikeCount: Math.max(0, Number(data.totalLikeCount ?? data.total) || 0)
        };
    }
    if (type !== 'gift') return base;

    const gift = data.gift || data.giftDetails || data.extendedGiftInfo || {};
    const repeatCount = Math.max(1, Number(data.repeatCount ?? data.repeat_count ?? data.count) || 1);
    const unitDiamonds = Math.max(0, Number(gift.diamondCount ?? gift.diamond_count ?? data.diamondCount ?? data.coins) || 0);
    const totalDiamonds = Number(data.totalDiamondCount ?? data.giftValue ?? data.totalCoins);
    return {
        ...base,
        giftId: firstText(data.giftId, data.gift_id, gift.id, gift.giftId),
        giftName: firstText(data.giftName, gift.giftName, gift.name, 'Gift'),
        giftType: Number(data.giftType ?? gift.giftType) || 0,
        repeatCount,
        repeatEnd: data.repeatEnd === undefined ? true : Boolean(data.repeatEnd),
        unitDiamondCount: unitDiamonds,
        giftPictureUrl: giftPictureUrl(data, gift),
        diamondCount: Number.isFinite(totalDiamonds) && totalDiamonds >= 0
            ? totalDiamonds
            : unitDiamonds * repeatCount
    };
}

function normalizeTikFinityMessage(message) {
    let parsed = message;
    if (Buffer.isBuffer(parsed)) parsed = parsed.toString('utf8');
    if (typeof parsed === 'string') {
        try {
            parsed = JSON.parse(parsed);
        } catch {
            return [];
        }
    }
    const items = Array.isArray(parsed) ? parsed : [parsed];
    return items.map(normalizeOne).filter(Boolean);
}

module.exports = { normalizeTikFinityMessage };
