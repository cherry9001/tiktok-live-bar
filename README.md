# 🎵 ÔNG CHÚ MMO — TikTok Live 3D Dance Floor

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Node.js](https://img.shields.io/badge/Node.js-%3E%3D20-green.svg)](https://nodejs.org/)
[![Unity](https://img.shields.io/badge/Unity-6000.x-black.svg)](https://unity.com/)

**Biến phiên TikTok LIVE thành sàn nhảy 3D tương tác** — Người xem gửi gift, chat, like, follow sẽ xuất hiện trên sàn nhảy với avatar TikTok thật, hiệu ứng ánh sáng, DJ booth và pháo hoa.

---

## 🎬 Video demo thực tế

[![Xem demo ÔNG CHÚ MMO trên TikTok LIVE](docs/media/live-demo.png)](https://www.facebook.com/100004611824062/videos/pcb.3562459197251106/37620662724214707)

> Nhấn vào ảnh để xem video demo đầy đủ trên Facebook.

---

## ✨ Tính năng

- 🕺 **Sàn nhảy 3D realtime** — Người xem TikTok tham gia sàn nhảy với nhân vật 3D
- 🎁 **Gift → Hiệu ứng** — Mỗi gift kích hoạt hiệu ứng riêng (zoom camera, pháo hoa, VIP spotlight...)
- 🏆 **Bảng xếp hạng Top 3** — Top gifter đứng trên bục DJ
- 🎵 **DJ Booth** — Phát nhạc + video nền tùy chỉnh
- ⚙️ **Master Rules** — Tùy chỉnh luật game qua giao diện web, không cần code
- 🧪 **Test Lab** — Demo mode với người xem giả để test trước khi live
- 🎨 **Chroma Key** — Bấm F2 để bật nền xanh, ghép vào OBS dễ dàng

---

## 📋 Yêu cầu hệ thống

| Phần mềm | Phiên bản |
|-----------|-----------|
| **Node.js** | ≥ 20.x |
| **Unity** | 6000.x (Unity 6) |
| **TikFinity Desktop** | Phiên bản mới nhất |
| **OBS Studio** | Khuyến nghị cho streaming |
| **Windows** | 10 / 11 (64-bit) |

---

## 🚀 Cài đặt & Chạy

### Cách nhanh nhất trên Windows

Nhấp đúp `run.bat`. Launcher sẽ kiểm tra Node.js, tự cài thư viện khi cần,
khởi động TikTok Bridge, mở game và Control Panel.

> `run.bat` không tự tắt chương trình khác đang dùng cổng 3000. Nếu launcher báo
> xung đột cổng, hãy đóng đúng chương trình được báo rồi chạy lại để tránh mất dữ liệu.

### Bước 1 — Clone repo

```bash
git clone https://github.com/<your-username>/ongchummo-tiktok-live.git
cd ongchummo-tiktok-live
```

### Chạy thủ công — Cài đặt Node Bridge

```bash
cd TikTokBridge
copy .env.example .env  # Tùy chỉnh nếu cần (Windows)
npm ci
npm start
```

### Mở Control Panel

Truy cập [http://127.0.0.1:3000/control.html](http://127.0.0.1:3000/control.html) trên trình duyệt.

### Chạy Unity Game

- **Nếu có file build:** Chạy `run.bat`
- **Nếu muốn build từ source:** Mở `UnityProject/` trong Unity Hub → Build

### Kết nối TikTok LIVE

1. Mở TikFinity Desktop → đăng nhập → bật LIVE
2. Trong Control Panel, nhập username TikTok đang live → **Kết nối**

---

## 📁 Cấu trúc thư mục

```
├── TikTokBridge/          # Node.js backend — bridge TikTok ↔ Unity
│   ├── server.js          # Server chính
│   ├── config/            # Cấu hình game, gifts, master rules
│   ├── public/            # Control panel (HTML/JS/CSS)
│   ├── src/               # Logic xử lý sự kiện, bảo mật
│   ├── assets/            # Banner, GIF hiệu ứng
│   └── test/              # Unit tests
│
├── UnityProject/          # Unity 6 — Game 3D
│   ├── Assets/Scripts/    # C# scripts (24 files)
│   └── Assets/Editor/     # Editor tools & build script
│
├── DJ_MUSIC/              # 🎵 Thả file nhạc MP3/WAV/OGG vào đây
├── DJ_VIDEO/              # 🎬 Thả file video MP4/PNG vào đây
├── LiveAssets/             # Hình nền, GIF hiệu ứng
├── Build/                 # (Không có trong repo) File build compiled
│
├── build.bat              # Script build Unity → EXE
├── run.bat                # Script chạy Node + Game
├── LICENSE                # Giấy phép MIT
└── README.md              # File này
```

---

## ⌨️ Phím tắt trong Game

| Phím | Chức năng |
|------|-----------|
| `F1` | Ẩn / hiện bảng điều khiển |
| `F2` | Bật / tắt nền xanh Chroma Key |
| `F11` | Toàn màn hình |

---

## 💬 Lệnh chat người xem

| Lệnh | Hiệu ứng |
|-------|-----------|
| `nhảy` / `dance` | Nhân vật nhảy |
| `đi vòng` / `walk` | Nhân vật đi bước tại chỗ |
| `đổi nv` | Đổi nhân vật ngẫu nhiên |

---

## 🎁 Hệ thống Gift

| Mức gift | Kim cương | Hiệu ứng |
|----------|-----------|-----------|
| Gift nhỏ | 1–9 💎 | Nhân vật nhảy, vào sàn |
| Gift trung | 10–99 💎 | Zoom camera, đổi nhân vật |
| Gift VIP | 100+ 💎 | Spotlight, pháo hoa, top DJ |

> Tùy chỉnh qua **Master Rules** trong Control Panel → tab ⚙️ Master Rules.

---

## 🎵 Thêm nhạc & video

- **Nhạc nền DJ:** Thả file `.mp3`, `.wav`, `.ogg` vào thư mục `DJ_MUSIC/`
- **Video nền:** Thả file `.mp4`, `.mov`, `.webm` hoặc ảnh `.png`, `.jpg` vào `DJ_VIDEO/`
- Game tự phát lặp và tắt tiếng video

> ⚠️ Hãy sử dụng nhạc và video có bản quyền hợp lệ.

---

## 🔧 Tùy chỉnh nâng cao

### Master Rules (không cần code)

Mở Control Panel → tab **⚙️ Master Rules** để:
- Thêm/sửa luật: Gift nào → hiệu ứng gì
- Chọn chế độ tham gia sàn (chat keyword hoặc mọi tương tác)
- Bật/tắt tự động vào sàn khi tặng gift

### Cấu hình Node Bridge

Sửa file `TikTokBridge/.env`:

```env
NODE_ENV=production
HOST=127.0.0.1
PORT=3000
LIVE_PROVIDER=tikfinity
TIKFINITY_WS_URL=ws://127.0.0.1:21213/
ALLOW_LAN=0
```

---

## 🧪 Test

```bash
cd TikTokBridge
npm test                    # Chạy unit tests
npm run security:smoke      # Test bảo mật WebSocket
```

Hoặc dùng **Test Lab** trong Control Panel để tạo người xem giả.

---

## 📺 Ghép vào OBS

1. Thêm source **Game Capture** → chọn cửa sổ ÔNG CHÚ MMO Live
2. Bấm **F2** trong game để bật Chroma Key (nền xanh)
3. Trong OBS: thêm filter **Chroma Key** → chọn màu xanh

---

## 📜 Giấy phép

Dự án được phát hành theo [Giấy phép MIT](LICENSE).

Tài nguyên bên thứ ba (GIF, hình ảnh) có thể có giấy phép riêng — xem `sources.json` trong từng thư mục assets.

---

## 📞 Liên hệ

- 🌐 Website: [ongchummo.com](https://ongchummo.com)
- 📱 Zalo: 0977.896.644
- 📧 Email: toanhvan90@gmail.com

---

<p align="center">
  Made with ❤️ by <strong>ÔNG CHÚ MMO</strong>
</p>
