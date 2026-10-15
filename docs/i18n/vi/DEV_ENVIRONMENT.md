# Môi Trường Phát Triển

Cách AutoClick được build và kiểm thử, dành cho cộng tác viên muốn tái lập môi trường. Thông số máy nằm trong [pc_spec.md](pc_spec.md). Bản tiếng Anh ở [docs/DEV_ENVIRONMENT.md](../../DEV_ENVIRONMENT.md).

## Toolchain

| Công cụ | Phiên bản | Ghi chú |
|---------|-----------|---------|
| **.NET SDK** | 10.0.4xx | Pin trong [`global.json`](../../../global.json) (`rollForward: latestFeature`) |
| **Target framework** | `net10.0` (Core), `net10.0-windows10.0.19041.0` (các project còn lại) | |
| **Tool cục bộ** | [`.config/dotnet-tools.json`](../../../.config/dotnet-tools.json) | `vpk` (Velopack), ReSharper InspectCode, nuget-license, sbom-tool, ReportGenerator |
| **IDE** | JetBrains Rider 2026.x hoặc Visual Studio Code | Trình soạn thảo nào hỗ trợ `.editorconfig` đều được |

## Build và kiểm thử

```bash
git clone https://github.com/poli0981/autoclick.git
cd autoclick
dotnet tool restore
dotnet build AutoClick.slnx
dotnet test --solution AutoClick.slnx
dotnet run --project src/AutoClick.UI
```

AutoClick chạy với quyền người dùng thường; chỉ cần quyền quản trị khi click vào game cũng chạy với quyền quản trị.

Các bước kiểm tra CI chạy cho mỗi pull request, và cách chạy chúng trên máy, nằm trong [CONTRIBUTING.md](../../../CONTRIBUTING.md).

## Công cụ kiểm thử

| Công cụ | Mục đích |
|---------|----------|
| **`tools/AutoClick.ClickTarget`** | Cửa sổ ghi lại mọi message chuột và bàn phím nhận được (toạ độ client, mã phím ảo và scan code). `--dpi unaware\|system\|pmv2` đổi chế độ DPI để kiểm tra quy đổi toạ độ, `--log <file>` ghi log ra file. |
| **`AutoClick.UI.exe --smoke-test`** | Mở mọi trang ở mọi theme và ngôn ngữ, các hộp thoại và lớp heatmap, báo lỗi nếu có binding hỏng. Dùng `%TEMP%\AutoClick-smoke` thay cho thư mục dữ liệu thật. |
| **Windows Sandbox** | Kiểm thử cài đặt và cập nhật từ bản cũ (ví dụ 1.3.3 → 2.0.0) mà không ảnh hưởng máy phát triển. |

## Game Đã Kiểm Thử

Kết quả với AutoClick 1.x trên máy của developer; có thể khác tuỳ hệ thống, phiên bản game và cấu hình anti-cheat của bạn.

### Đạt

| Game | Liên kết | Ghi chú |
|------|----------|---------|
| **Bound Between Desks** | [Steam](https://store.steampowered.com/app/3790060/Bound_Between_Desks/) | Click PostMessage được nhận đúng |
| **Myosotis: My Life Is Not Yours to Take** | [Steam](https://store.steampowered.com/app/4220830/Myosotis_My_Life_Is_Not_Yours_to_Take/) | Mọi chế độ click đều hoạt động |
| **Man in a Suit in a Building in a City** | [Steam](https://store.steampowered.com/app/3292090/Man_in_a_suit_in_a_building_in_a_city/) | Click nền ổn định |

### Đạt (có lưu ý)

| Game | Liên kết | Anti-Cheat | Ghi chú |
|------|----------|------------|---------|
| **Wuthering Waves** | [Steam](https://store.steampowered.com/app/3513350/Wuthering_Waves/) | ACE (Kernel) | Tạm thời đạt khi thử. **Có anti-cheat cấp kernel**: tự chịu rủi ro khi dùng. Cách phát hiện có thể thay đổi theo bản cập nhật game. |

### Không đạt

| Game | Liên kết | Ghi chú |
|------|----------|---------|
| **Mercury Elopement Syndrome** | [Steam](https://store.steampowered.com/app/4417890/Mercury_Elopement_Syndrome/) | Game không nhận click PostMessage. Có thể game dùng DirectInput hoặc Raw Input, không phản hồi message `WM_LBUTTONDOWN`. |

## Lưu Ý Tương Thích

- **Click bằng PostMessage** hoạt động với game xử lý message cửa sổ chuẩn (`WM_LBUTTONDOWN` / `WM_LBUTTONUP`, `WM_KEYDOWN` / `WM_KEYUP`). Game đọc DirectInput, Raw Input hoặc luồng input riêng có thể không phản hồi.
- **Game chạy với quyền quản trị** chỉ nhận input từ chương trình có cùng quyền; AutoClick phát hiện điều này và đề nghị khởi động lại với quyền quản trị.
- **Chế độ toàn màn hình độc quyền (fullscreen exclusive)** có thể chặn message và lớp phủ. Nên dùng chế độ cửa sổ hoặc không viền.
- **Anti-cheat cấp kernel** (EasyAntiCheat, BattlEye, Vanguard, ACE) có thể phát hiện công cụ tự động bất kể cách làm. Hãy đọc điều khoản của game trước khi dùng.
