const test = require('node:test');
const assert = require('node:assert/strict');
const {
    normalizeChat,
    normalizeMember,
    normalizeGift,
    isPendingGiftStreak
} = require('../src/tiktok/normalize-event');
const { normalizeTikFinityMessage } = require('../src/tiktok/normalize-tikfinity-event');

test('normalizes v2 nested chat user data', () => {
    const event = normalizeChat({
        event: { msgId: 'chat-1' },
        user: {
            userId: '42',
            uniqueId: 'viewer',
            nickname: 'Viewer',
            profilePicture: { urls: ['https://example.com/avatar.jpg'] }
        },
        comment: 'nhảy'
    });

    assert.equal(event.userId, '42');
    assert.equal(event.nickname, 'Viewer');
    assert.equal(event.avatar, 'https://example.com/avatar.jpg');
    assert.equal(event.comment, 'nhảy');
});

test('normalizes avatar URLs from legacy and raw TikTok user shapes', () => {
    const legacy = normalizeMember({
        userId: 'legacy-1',
        uniqueId: 'legacy_viewer',
        nickname: 'Legacy Viewer',
        profilePictureUrl: 'https://example.com/legacy.jpg'
    });
    const raw = normalizeMember({
        user: {
            userId: 'raw-1',
            uniqueId: 'raw_viewer',
            nickname: 'Raw Viewer',
            avatarThumb: { urlList: ['https://example.com/raw.jpg'] }
        }
    });

    assert.equal(legacy.avatar, 'https://example.com/legacy.jpg');
    assert.equal(raw.avatar, 'https://example.com/raw.jpg');
});

test('normalizes gift value and detects unfinished streaks', () => {
    const pending = normalizeGift({
        user: { userId: '7', uniqueId: 'gifter' },
        giftId: 5655,
        repeatCount: 3,
        repeatEnd: 0,
        giftDetails: {
            giftName: 'Rose',
            giftType: 1,
            diamondCount: 1
        }
    });

    assert.equal(pending.diamondCount, 3);
    assert.equal(isPendingGiftStreak(pending), true);

    const finished = { ...pending, repeatEnd: true };
    assert.equal(isPendingGiftStreak(finished), false);
});

test('normalizes TikFinity chat and gift envelopes', () => {
    const [chat] = normalizeTikFinityMessage(JSON.stringify({
        event: 'chat',
        data: {
            msgId: 'tf-chat-1',
            user: { userId: '21', uniqueId: 'viewer21', nickname: 'Viewer 21' },
            comment: 'hey'
        }
    }));
    const [gift] = normalizeTikFinityMessage({
        event: 'gift',
        data: {
            eventId: 'tf-gift-1',
            userId: '21',
            uniqueId: 'viewer21',
            nickname: 'Viewer 21',
            gift: {
                id: 5655,
                name: 'Rose',
                diamondCount: 1,
                giftImage: { url: ['https://example.com/rose.png'] }
            },
            repeatCount: 3
        }
    });

    assert.equal(chat.type, 'chat');
    assert.equal(chat.comment, 'hey');
    assert.equal(gift.type, 'gift');
    assert.equal(gift.giftName, 'Rose');
    assert.equal(gift.diamondCount, 3);
    assert.equal(gift.unitDiamondCount, 1);
    assert.equal(gift.giftPictureUrl, 'https://example.com/rose.png');
    assert.equal(gift.repeatEnd, true);
});

test('prefers TikFinity JPEG avatar variant that Unity can decode', () => {
    const [chat] = normalizeTikFinityMessage({
        event: 'chat',
        data: {
            userId: '22',
            uniqueId: 'viewer22',
            nickname: 'Viewer 22',
            profilePictureUrl: 'https://example.com/avatar.webp?token=1',
            userDetails: {
                profilePictureUrls: [
                    'https://example.com/avatar.webp?token=2',
                    'https://example.com/avatar.jpeg?token=3'
                ]
            },
            comment: 'hey'
        }
    });

    assert.equal(chat.avatar, 'https://example.com/avatar.jpeg?token=3');
});
