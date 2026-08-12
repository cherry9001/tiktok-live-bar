function normalizeUser(data = {}) {
    const user = data.user || data;
    const profilePicture = user.profilePicture || {};
    const avatarSources = [
        profilePicture.urls,
        profilePicture.urlList,
        user.avatarThumb?.urlList,
        user.avatarMedium?.urlList,
        user.avatarLarge?.urlList,
        user.profilePictureUrls
    ];
    const avatar = avatarSources
        .flatMap(source => Array.isArray(source) ? source : [])
        .find(url => typeof url === 'string' && /^https?:\/\//i.test(url)) ||
        user.profilePictureUrl ||
        '';
    const fallbackId = user.uniqueId || user.nickname || `guest-${Date.now()}`;

    return {
        userId: String(user.userId || fallbackId),
        uniqueId: String(user.uniqueId || fallbackId),
        nickname: String(user.nickname || user.uniqueId || 'TikTok user'),
        avatar: String(avatar)
    };
}

function eventId(data = {}) {
    return String(data.event?.msgId || data.common?.msgId || data.msgId || '');
}

function normalizeChat(data) {
    return {
        type: 'chat',
        eventId: eventId(data),
        ...normalizeUser(data),
        comment: String(data.comment || '')
    };
}

function normalizeMember(data) {
    return {
        type: 'member',
        eventId: eventId(data),
        ...normalizeUser(data)
    };
}

function normalizeGift(data) {
    const details = data.giftDetails || data.extendedGiftInfo || {};
    const repeatCount = Math.max(1, Number(data.repeatCount) || 1);
    const diamondCount = Math.max(
        0,
        Number(details.diamondCount ?? data.diamondCount) || 0
    );

    return {
        type: 'gift',
        eventId: eventId(data),
        ...normalizeUser(data),
        giftId: String(data.giftId || details.id || ''),
        giftName: String(details.giftName || data.giftName || 'Gift'),
        giftType: Number(details.giftType ?? data.giftType) || 0,
        repeatCount,
        repeatEnd: Boolean(data.repeatEnd),
        diamondCount: diamondCount * repeatCount
    };
}

function normalizeLike(data) {
    return {
        type: 'like',
        eventId: eventId(data),
        ...normalizeUser(data),
        likeCount: Math.max(1, Number(data.likeCount) || 1),
        totalLikeCount: Math.max(0, Number(data.totalLikeCount) || 0)
    };
}

function normalizeSocial(type, data) {
    return {
        type,
        eventId: eventId(data),
        ...normalizeUser(data)
    };
}

function isPendingGiftStreak(event) {
    return event.giftType === 1 && !event.repeatEnd;
}

module.exports = {
    normalizeUser,
    normalizeChat,
    normalizeMember,
    normalizeGift,
    normalizeLike,
    normalizeSocial,
    isPendingGiftStreak
};
