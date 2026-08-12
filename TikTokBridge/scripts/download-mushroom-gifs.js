const fs = require('fs/promises');
const path = require('path');
const crypto = require('crypto');

const outputDir = path.join(__dirname, '..', 'assets', 'gifs');
const maxGifBytes = 1_500_000;

// Các file này có nền thật hoặc chứa cả một nhóm nhân vật nên không phù hợp
// để đại diện cho một người xem trên sàn nhảy.
const excludedAssetUrls = new Set([
    'https://wx4.sinaimg.cn/large/ceeb653ely1fmnq7xvsh6g206o06oahu.gif',
    'https://wx1.sinaimg.cn/large/a6d0124fly1ff2i021t0wg20e607vn8n.gif',
    'https://5b0988e595225.cdn.sohucs.com/images/20180523/13fd8cdc1d31482caa2d24d0d76c8cde.gif',
    'https://img.230558.xyz/biaoqing/201807/7d606893c6c54a406953c4ae4a2bdc23.gif',
    'https://i.ecywang.com/upload/1/img2.baidu.com/it/u%3D4117748196%2C225280191%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=300&w=300',
    'https://wx2.sinaimg.cn/large/006BkP2Hly1fds70lm93hg308c08c0ve.gif',
    'https://wx1.sinaimg.cn/large/814268e3ly1fmonhho9mwg20b40b4di5.gif',
    'https://wx3.sinaimg.cn/large/62528dc5gy1fitgj8ollrg206o06omy8.gif',
    'https://wx1.sinaimg.cn/large/62528dc5gy1ffoigkh2dxg206o06omyd.gif',
    'https://wx2.sinaimg.cn/large/006BkP2Hly1fds70ewa01g304x06bq39.gif',
    'https://wx1.sinaimg.cn/large/62528dc5gy1fdufbcll3ug207v07vdgg.gif',
    'https://wx1.sinaimg.cn/large/62528dc5gy1fduf8botaag207v07r3zc.gif',
    'https://wx3.sinaimg.cn/large/006BkP2Hly1fds70h3ue3g304x04xjtz.gif',
    'https://media.giphy.com/media/GR1rLRFEaxnR9sfR3z/giphy.gif'
]);

// “蘑菇头跳舞” (mushroom-head dancing meme) collections.
// Keep the page URL alongside every asset so its origin remains traceable.
const sources = [
    {
        page: 'https://weibo.com/ttarticle/p/show?id=2310474229905415015455',
        urls: [
            'https://wx4.sinaimg.cn/large/62528dc5gy1fmnblezdjdg206o06odie.gif',
            'https://wx1.sinaimg.cn/large/62528dc5gy1fl2oj3384jg206o06o7ai.gif',
            'https://wx2.sinaimg.cn/large/006BkP2Hly1fds70e8g4bg304x04xmxf.gif',
            'https://wx4.sinaimg.cn/large/ceeb653ely1fmnq7xvsh6g206o06oahu.gif',
            'https://wx1.sinaimg.cn/large/a9cf8ef6ly1fo2c9pf7uxg206o06odm2.gif',
            'https://wx4.sinaimg.cn/large/006BkP2Hly1fds70ep590g304x04xmyt.gif',
            'https://wx1.sinaimg.cn/large/a6d0124fly1ff2i021t0wg20e607vn8n.gif',
            'https://wx3.sinaimg.cn/large/62528dc5gy1ffoiglzee5g206o06ogn3.gif',
            'https://wx3.sinaimg.cn/large/006BkP2Hly1fds70fas22g307o07omyg.gif'
        ]
    },
    {
        page: 'https://www.sohu.com/a/232589787_99910071',
        urls: [
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/13fd8cdc1d31482caa2d24d0d76c8cde.gif',
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/df38ada8b14342e69edc3c5e43fb8be9.gif',
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/c1eba9dffbe447b19bcd69cc7d4c7e89.gif',
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/dccb589108724b2b81859039d72f9981.gif',
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/9f0d408b130b4ed68cb02b5d507714c3.gif',
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/733545c758e34d25ad60c5a50927486b.gif',
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/cd02c3d5a7ec490fab7bb1917f5a353a.gif',
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/cfe2200c07834818a27a205e1bd279ae.gif',
            'https://5b0988e595225.cdn.sohucs.com/images/20180523/c9e8fe6be31c4d50993d349e3efdf606.gif'
        ]
    },
    {
        page: 'https://www.weilegetu.com/image/214.html',
        urls: [
            'https://www.weilegetu.com/images/202108/16285272177834.gif'
        ]
    },
    {
        page: 'https://www.biaoqingjia.com/gaoxiao/3810.html',
        urls: [
            'https://img.230558.xyz/biaoqing/201807/7d606893c6c54a406953c4ae4a2bdc23.gif'
        ]
    },
    {
        page: 'https://ecywang.com/pic/%E8%98%91%E8%8F%87%E5%A4%B4%E8%A1%A8%E6%83%85%E5%8C%85%E5%8A%A8%E5%9B%BEgif/',
        staticOnly: true,
        urls: [
            'https://i.ecywang.com/upload/1/img0.baidu.com/it/u%3D235455949%2C3156816635%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=228&w=228',
            'https://i.ecywang.com/upload/1/img2.baidu.com/it/u%3D430630605%2C383342387%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=168&w=168',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D1669469128%2C1067691572%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=285&w=285',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D3112052954%2C275129727%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=240&w=240',
            'https://i.ecywang.com/upload/1/img2.baidu.com/it/u%3D1459115107%2C2301133842%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=228&w=228',
            'https://i.ecywang.com/upload/1/img0.baidu.com/it/u%3D3555705346%2C1625590459%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=300&w=300',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D3093791251%2C1411676775%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=168&w=168',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D3996760121%2C1351511708%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=228&w=228',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D2265543375%2C1772807920%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=240&w=240',
            'https://i.ecywang.com/upload/1/img0.baidu.com/it/u%3D1750356653%2C94701869%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=268&w=268',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D2464699192%2C1414002034%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=262&w=262',
            'https://i.ecywang.com/upload/1/img2.baidu.com/it/u%3D3073871904%2C315165930%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=228&w=228',
            'https://i.ecywang.com/upload/1/img2.baidu.com/it/u%3D4117748196%2C225280191%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=300&w=300',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D2902836319%2C4268075240%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=228&w=228',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D457019813%2C20308941%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=240&w=240',
            'https://i.ecywang.com/upload/1/img2.baidu.com/it/u%3D1884090381%2C2155575431%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=327&w=327',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D3383960592%2C1968882325%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=240&w=240',
            'https://i.ecywang.com/upload/1/img2.baidu.com/it/u%3D3439215531%2C1107017020%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=240&w=240',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D2646777776%2C1370208018%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=456&w=456',
            'https://i.ecywang.com/upload/1/img1.baidu.com/it/u%3D17427255%2C3970598724%26fm%3D253%26fmt%3Dauto%26app%3D138%26f%3DGIF?h=240&w=240'
        ]
    },
    {
        page: 'https://weibo.com/ttarticle/p/show?id=2310474229905415015455',
        urls: [
            'https://wx2.sinaimg.cn/large/006BkP2Hly1fds70lm93hg308c08c0ve.gif',
            'https://wx2.sinaimg.cn/large/006BkP2Hly1fds70f43seg304x04xaaf.gif',
            'https://wx1.sinaimg.cn/large/814268e3ly1fmonhho9mwg20b40b4di5.gif',
            'https://wx3.sinaimg.cn/large/62528dc5gy1fitgj8ollrg206o06omy8.gif',
            'https://wx1.sinaimg.cn/large/62528dc5gy1ffoigkh2dxg206o06omyd.gif',
            'https://wx2.sinaimg.cn/large/62528dc5gy1febnopkp9rg206o06oq6m.gif',
            'https://wx2.sinaimg.cn/large/006BkP2Hly1fds70ewa01g304x06bq39.gif',
            'https://wx4.sinaimg.cn/large/62528dc5gy1ffg8jdvk8ig206o06o74q.gif',
            'https://wx1.sinaimg.cn/large/62528dc5gy1fdufbcll3ug207v07vdgg.gif',
            'https://wx2.sinaimg.cn/large/a6d0124fly1ff2i0080zog20b40b4q79.gif',
            'https://wx1.sinaimg.cn/large/62528dc5gy1fduf8botaag207v07r3zc.gif',
            'https://wx3.sinaimg.cn/large/006BkP2Hly1fds70h3ue3g304x04xjtz.gif',
            'https://wx4.sinaimg.cn/large/62528dc5gy1fdvmxo1ihug206o06o75w.gif',
            'https://ws4.sinaimg.cn/large/9150e4e5ly1fe8lg1napug20rs0rsq6v.gif',
            'https://wx1.sinaimg.cn/large/62528dc5gy1ffoigli7yig206o06ojso.gif'
        ]
    },
    {
        page: 'https://giphy.com/explore/cat-dance-stickers',
        urls: [
            'https://media.giphy.com/media/GR1rLRFEaxnR9sfR3z/giphy.gif',
            'https://media.giphy.com/media/pNug6VsMANqKtTA1yR/giphy.gif',
            'https://media.giphy.com/media/YOun5uEXWmBEa8D9y7/giphy.gif',
            'https://media.giphy.com/media/ggpoVsIg0LwtHfTBEY/giphy.gif',
            'https://media.giphy.com/media/0DmZAKSSERVJk9vfP1/giphy.gif',
            'https://media.giphy.com/media/udQgnMLoEiQes/giphy.gif',
            'https://media.giphy.com/media/4Lt25yHnq8qveLZGBB/giphy.gif'
        ]
    },
    {
        page: 'https://giphy.com/explore/dancing-character-stickers',
        urls: [
            'https://media.giphy.com/media/WAa0z619AHO3m0zivH/giphy.gif',
            'https://media.giphy.com/media/1ynmJnZbDvTBJtho1K/giphy.gif',
            'https://media.giphy.com/media/dyAfHg3E2EWN3abZBr/giphy.gif',
            'https://media.giphy.com/media/dvCPWYNNaaQpjrpl0d/giphy.gif',
            'https://media.giphy.com/media/65iuKnmagop44xtMno/giphy.gif',
            'https://media.giphy.com/media/QNpeBIyfSJXVL0JmaB/giphy.gif',
            'https://media.giphy.com/media/1NRaZ4REktLkjUi5p7/giphy.gif',
            'https://media.giphy.com/media/tbJhA2xAcqOz2ThVMZ/giphy.gif'
        ]
    }
];

function isGif(buffer) {
    const signature = buffer.subarray(0, 6).toString('ascii');
    return signature === 'GIF87a' || signature === 'GIF89a';
}

function skipGifSubBlocks(buffer, start) {
    let offset = start;
    while (offset < buffer.length) {
        const size = buffer[offset];
        offset += 1;
        if (size === 0) return offset;
        offset += size;
    }
    return buffer.length;
}

function countGifFrames(buffer) {
    if (!isGif(buffer) || buffer.length < 13) return 0;

    const globalColorTable = (buffer[10] & 0x80) !== 0;
    const globalColorTableSize = 3 * (2 ** ((buffer[10] & 0x07) + 1));
    let offset = 13 + (globalColorTable ? globalColorTableSize : 0);
    let frames = 0;

    while (offset < buffer.length) {
        const marker = buffer[offset];
        offset += 1;

        if (marker === 0x3b) break;
        if (marker === 0x21) {
            offset += 1;
            offset = skipGifSubBlocks(buffer, offset);
            continue;
        }
        if (marker !== 0x2c || offset + 9 > buffer.length) break;

        const localColorTable = (buffer[offset + 8] & 0x80) !== 0;
        const localColorTableSize = 3 * (2 ** ((buffer[offset + 8] & 0x07) + 1));
        offset += 9 + (localColorTable ? localColorTableSize : 0);
        if (offset >= buffer.length) break;

        offset += 1;
        offset = skipGifSubBlocks(buffer, offset);
        frames += 1;
    }

    return frames;
}

function hasTransparentFrame(buffer) {
    // A GIF Graphic Control Extension stores the transparency flag in bit 0.
    for (let index = 0; index < buffer.length - 4; index += 1) {
        if (
            buffer[index] === 0x21 &&
            buffer[index + 1] === 0xf9 &&
            buffer[index + 2] === 0x04 &&
            (buffer[index + 3] & 0x01) === 0x01
        ) {
            return true;
        }
    }
    return false;
}

async function download(url, referer) {
    const response = await fetch(url, {
        redirect: 'follow',
        headers: {
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36',
            Referer: referer
        },
        signal: AbortSignal.timeout(20_000)
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const buffer = Buffer.from(await response.arrayBuffer());
    if (!isGif(buffer)) throw new Error('Nội dung tải về không phải GIF');
    if (buffer.length > maxGifBytes) throw new Error('GIF quá nặng để dùng cho sàn đông người');
    if (countGifFrames(buffer) < 2) throw new Error('GIF chỉ có một frame tĩnh');
    if (!hasTransparentFrame(buffer)) throw new Error('GIF không có nền trong suốt');
    return buffer;
}

async function main() {
    await fs.mkdir(outputDir, { recursive: true });
    const manifest = [];
    const contentHashes = new Set();
    let index = 0;

    for (const source of sources) {
        for (const url of source.urls) {
            index += 1;
            const filename = `mushroom_dance_${String(index).padStart(2, '0')}.gif`;
            const target = path.join(outputDir, filename);
            if (source.staticOnly) {
                console.warn(`− ${filename}: bỏ qua nguồn chỉ có ảnh GIF một frame`);
                continue;
            }
            if (excludedAssetUrls.has(url)) {
                console.warn(`− ${filename}: bỏ qua asset không phù hợp làm nhân vật`);
                continue;
            }
            try {
                const buffer = await download(url, source.page);
                const hash = crypto.createHash('sha256').update(buffer).digest('hex');
                if (contentHashes.has(hash)) {
                    console.warn(`= ${filename}: bỏ qua nội dung trùng`);
                    continue;
                }
                contentHashes.add(hash);
                await fs.writeFile(target, buffer);
                manifest.push({ filename, bytes: buffer.length, sha256: hash, sourcePage: source.page, assetUrl: url });
                console.log(`✓ ${filename} (${Math.round(buffer.length / 1024)} KB)`);
            } catch (error) {
                try {
                    const previous = await fs.readFile(target);
                    if (
                        !isGif(previous) ||
                        previous.length > maxGifBytes ||
                        countGifFrames(previous) < 2 ||
                        !hasTransparentFrame(previous)
                    ) {
                        throw new Error();
                    }
                    const hash = crypto.createHash('sha256').update(previous).digest('hex');
                    if (contentHashes.has(hash)) throw new Error();
                    contentHashes.add(hash);
                    manifest.push({
                        filename,
                        bytes: previous.length,
                        sha256: hash,
                        sourcePage: source.page,
                        assetUrl: url
                    });
                    console.warn(`≈ ${filename}: giữ bản hợp lệ đã tải trước đó`);
                } catch {
                    console.warn(`✗ ${filename}: ${error.message}`);
                }
            }
        }
    }

    await fs.writeFile(
        path.join(outputDir, 'sources.json'),
        `${JSON.stringify(manifest, null, 2)}\n`,
        'utf8'
    );

    const acceptedFiles = new Set(manifest.map(item => item.filename));
    const outputFiles = await fs.readdir(outputDir);
    for (const filename of outputFiles) {
        if (/^mushroom_dance_\d+\.gif$/.test(filename) && !acceptedFiles.has(filename)) {
            await fs.rm(path.join(outputDir, filename));
        }
    }

    console.log(`Đã tải ${manifest.length}/${index} GIF vào ${outputDir}`);
    if (manifest.length === 0) process.exitCode = 1;
}

main().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
