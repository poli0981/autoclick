# Môi Trường Phát Triển

Cấu hình máy được dùng để phát triển, debug và kiểm thử AutoClick. Cung cấp làm tham chiếu cho cộng tác viên muốn tái lập môi trường build và test. Bản tiếng Anh ở [docs/DEV_ENVIRONMENT.md](../../DEV_ENVIRONMENT.md).

## Thông Số Máy

| Thành phần | Chi tiết |
|------------|----------|
| **OS** | Windows 11 Pro Insider Preview (Dev Channel) |
| **Build** | 26300.8142.ge_prerelease_im.260321-1005 |
| **CPU** | Intel Core i7-14700KF |
| **GPU** | NVIDIA GeForce RTX 5080 (16 GB VRAM) |
| **RAM** | 32 GB DDR5 |
| **Lưu trữ** | SSD 1 TB |
| **IDE** | JetBrains Rider 2026.1 |
| **.NET SDK** | 11.0.100-preview.2 (target `net8.0-windows`) |

## Hướng Dẫn Build

```bash
git clone https://github.com/poli0981/autoclick.git
cd autoclick
dotnet restore
dotnet build
```

Chạy ứng dụng:
```bash
dotnet run --project src/AutoClick.UI
```

> **Lưu ý:** Ứng dụng cần quyền administrator. Khi chạy từ IDE, hãy mở IDE bằng "Run as administrator", hoặc nhấp phải vào `.exe` đã build và chọn "Run as administrator".

## Game Đã Kiểm Thử

Các game sau đã được kiểm thử trong quá trình phát triển. Kết quả phụ thuộc vào máy của developer và có thể khác với máy của bạn tùy hệ thống, phiên bản game, và cấu hình anti-cheat.

### Hoạt Động

| Game | Cửa hàng | Ghi chú |
|------|----------|---------|
| **Bound Between Desks** | [Steam](https://store.steampowered.com/app/3790060/Bound_Between_Desks/) | PostMessage click được nhận đúng |
| **Myosotis: My Life Is Not Yours to Take** | [Steam](https://store.steampowered.com/app/4220830/Myosotis_My_Life_Is_Not_Yours_to_Take/) | Tất cả chế độ click hoạt động |
| **Man in a Suit in a Building in a City** | [Steam](https://store.steampowered.com/app/3292090/Man_in_a_suit_in_a_building_in_a_city/) | Background click ổn định |

### Hoạt Động (với lưu ý)

| Game | Cửa hàng | Anti-Cheat | Ghi chú |
|------|----------|------------|---------|
| **Wuthering Waves** | [Steam](https://store.steampowered.com/app/3513350/Wuthering_Waves/) | ACE (Kernel) | Tạm thời pass trong test. **Anti-cheat kernel-level đang hiện diện** — dùng với rủi ro của bạn. Hành vi phát hiện có thể thay đổi theo bản cập nhật game. |

### Không Hoạt Động

| Game | Cửa hàng | Ghi chú |
|------|----------|---------|
| **Mercury Elopement Syndrome** | [Steam](https://store.steampowered.com/app/4417890/Mercury_Elopement_Syndrome/) | Click PostMessage không được game ghi nhận. Có thể game dùng DirectInput hoặc Raw Input, không phản hồi với message `WM_LBUTTONDOWN`. |

## Môi Trường VM Để Kiểm Thử

Khả năng tương thích OS được kiểm thử bằng máy ảo. Xem [System Requirements](../../SYSTEM_REQUIREMENTS.md) để biết ma trận tương thích đầy đủ.

| Công cụ | Phiên bản | Ghi chú |
|---------|-----------|---------|
| **Oracle VirtualBox** | 7.2.6 r172322 (Qt 6.8.0 trên Windows) | Nền tảng VM chính |
| **Windows Sandbox** | Tích hợp sẵn Windows 10/11 | Test cô lập nhanh |

### Kết Quả Test VM

| OS | RAM | CPU | Kết quả |
|----|-----|-----|---------|
| Windows 10 22H2 (Build 19045.3803) | 4 GB | 2 cores | **Pass** — đầy đủ tính năng kể cả dashboard |
| Windows 8.1 / 8.1 Pro (Build 9600) | 8 GB | 2 cores | **Một phần** — chỉ v1.1.0; yêu cầu [VC++ 2015 Redistributable](https://www.microsoft.com/en-us/download/details.aspx?id=48145); dashboard (LiveChartsCore/SkiaSharp) không tương thích |
| Windows 7 | — | — | **Failed** — .NET 8 không hỗ trợ |

## Ghi Chú Tương Thích

- **Click qua PostMessage** hoạt động với game xử lý message Windows tiêu chuẩn (`WM_LBUTTONDOWN` / `WM_LBUTTONUP`). Game dùng DirectInput, Raw Input, hoặc pipeline input tùy biến có thể không phản hồi.
- **Fullscreen exclusive mode** có thể chặn việc nhận message. Khuyến nghị dùng Borderless Windowed hoặc Windowed.
- **Anti-cheat kernel-level** (EasyAntiCheat, BattlEye, Vanguard, ACE) có thể phát hiện và đánh dấu công cụ automation bất kể phương thức nào. Luôn kiểm tra Terms of Service của game trước khi sử dụng.
- **Kết quả có thể khác nhau** giữa các cấu hình phần cứng, phiên bản driver, và bản vá game.
