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
| **Unity** | Chỉ cần khi tự build source; dùng đúng 6000.2.10f1 |
| **TikFinity Desktop** | Phiên bản mới nhất |
| **OBS Studio** | Khuyến nghị cho streaming |
| **Windows** | 10 / 11 (64-bit) |

---

## 🚀 Cài đặt & Chạy

### Cách nhanh nhất trên Windows

1. Tải [`OngChuMMO-Live-Windows-v1.0.3.zip`](https://github.com/cherry9001/tiktok-live-bar/releases/latest/download/OngChuMMO-Live-Windows-v1.0.3.zip) trong mục **Releases**.
2. Giải nén toàn bộ ZIP ra một thư mục mới. Không chạy trực tiếp bên trong ZIP.
3. Cài [Node.js 20 LTS trở lên](https://nodejs.org/) nếu máy chưa có.
4. Nhấp đúp `run.bat`. Launcher tự cài thư viện, mở Bridge, Game và Control Panel.

> `run.bat` không tự tắt chương trình khác đang dùng cổng 3000. Nếu launcher báo
> xung đột cổng, hãy đóng đúng chương trình được báo rồi chạy lại để tránh mất dữ liệu.

> **Không tải “Source code (zip)” nếu bạn chỉ muốn chơi.** File source tự động của
> GitHub không chứa thư mục `Build`; hãy tải đúng file Windows ở mục Releases.

### Dành cho lập trình viên — Clone source

```bash
git clone https://github.com/cherry9001/tiktok-live-bar.git
cd tiktok-live-bar
```

Source GitHub không chứa game đã biên dịch. Cài Unity `6000.2.10f1`, sau đó chạy
`build.bat` hoặc mở `UnityProject/` bằng Unity Hub để tạo thư mục `Build`.

### Chạy thủ công — Cài đặt Node Bridge

```bash
cd TikTokBridge
copy .env.example .env
npm ci
npm start
```

### Mở Control Panel

Truy cập [http://127.0.0.1:3000/control.html](http://127.0.0.1:3000/control.html) trên trình duyệt.

### Chạy Unity Game

- **Nếu có file build:** Chạy `run.bat`
- **Nếu clone source:** Cài Unity `6000.2.10f1`, rồi chạy `build.bat`

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
├── Build/                 # Có trong gói Release; không có trong source Git
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

Bridge thực sự nạp file `.env` khi khởi động. Biến môi trường của Windows được ưu
tiên nếu cùng tên. Bản game dựng sẵn kết nối cố định tới cổng `3000`; chỉ đổi
`PORT` khi bạn dùng riêng Control Panel/Bridge hoặc đã tự build lại Unity client.

### Xử lý lỗi cài đặt thường gặp

- **`node` hoặc `npm` không được nhận diện:** cài Node.js 20+, đóng cửa sổ cũ rồi chạy lại `run.bat`.
- **Cổng 3000 đang bị chiếm:** đóng đúng ứng dụng/PID được launcher báo; launcher không tự tắt ứng dụng khác.
- **Không tìm thấy game:** bạn đã tải Source ZIP hoặc clone Git. Hãy tải bản Windows trong Releases hoặc tự build bằng Unity.
- **Windows SmartScreen cảnh báo:** chọn **More info → Run anyway** nếu file được tải từ Release chính thức của repo này.
- **TikTok chưa có sự kiện:** mở TikFinity, kiểm tra WebSocket `ws://127.0.0.1:21213/`, sau đó thử **Test Lab** trước.

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
