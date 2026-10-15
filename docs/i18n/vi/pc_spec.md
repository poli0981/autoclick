# Cấu Hình Máy Phát Triển

Bản tiếng Việt của [docs/pc_spec.md](../../pc_spec.md). Tài liệu mô tả công khai máy chính của developer — máy dùng để build và kiểm thử dự án. Kết quả kiểm thử game nằm trong [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md).

> Áp dụng cho **toàn bộ** các dự án đang triển khai của developer, không chỉ riêng AutoClick.

## Máy Phát Triển Chính

| Thành phần | Chi tiết |
|------------|----------|
| **OS** | Windows 11 Pro Insider Preview (kênh Dev) |
| **Build** | 29648.1000 |
| **CPU** | Intel Core i7-14700KF |
| **GPU** | NVIDIA GeForce RTX 5080 (16 GB VRAM) |
| **RAM** | 32 GB DDR5 |
| **Lưu trữ** | SSD 1 TB |
| **IDE** | JetBrains IDEs (bản trả phí) 2026.x + Visual Studio Code |

## Thiết Bị Di Động Để Kiểm Thử

Dùng cho kiểm tra web đa trình duyệt:

- iPhone 14 Pro — iOS 26.x — Chrome, Brave
- iPhone 13 Pro Max — iOS 26.x — Chrome, Brave

## Phiên Bản Toolchain

Chỉ liệt kê các toolchain đang cài cho các dự án hiện hành. Mỗi dự án có thể pin phiên bản chặt hơn (AutoClick pin .NET SDK trong [`global.json`](../../../global.json)).

- **.NET** — SDK 10.0.4xx; runtime 8.0 và 10.0
- **Python** — 3.14.x
- **Node.js** — 24.x
- **Rust** — stable (cài qua `rustup`)
- **Git** — 2.56; bật GPG ký commit (`commit.gpgsign=true`)

Phiên bản mới sẽ được bổ sung khi xuất hiện trong dự án đã phát hành.

## Tài Liệu Liên Quan

- [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md) — build, công cụ kiểm thử và các game đã thử (tiếng Việt)
- [../../SYSTEM_REQUIREMENTS.md](../../SYSTEM_REQUIREMENTS.md) — yêu cầu hệ thống cho người dùng cuối
- [../../pc_spec.md](../../pc_spec.md) — bản gốc tiếng Anh
