# Cấu Hình Máy Phát Triển

Bản tiếng Việt của [docs/pc_spec.md](../../pc_spec.md). Tài liệu mô tả công khai máy chính của developer — dùng làm tham chiếu "dự án được build và kiểm thử trên máy nào". Kết quả test game/build cụ thể nằm trong [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md).

> Áp dụng cho **toàn bộ** các dự án đang triển khai của developer, không chỉ riêng AutoClick.

## Máy Phát Triển Chính

| Thành phần | Chi tiết |
|------------|----------|
| **OS** | Windows 11 Pro 25H2 Insider Preview (Dev Channel) |
| **Build** | 26300.8376 |
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

Chỉ liệt kê các toolchain đang dùng trong các dự án hiện hành. Mỗi dự án có thể pin mức tối thiểu chặt hơn trong README riêng.

- **Python** — 3.12.x, 3.14.x
- **Node.js** — `>= 25.8.1`
- **Rust** — stable (cài qua `rustup`)
- **Git** — bản gần đây; bật GPG ký commit (`commit.gpgsign=true`)
- **.NET** — 8.x, 9.x, 10.x, 11.x (PREVIEW)

Phiên bản mới sẽ được bổ sung khi xuất hiện trong dự án đã ship.

## Tài Liệu Liên Quan

- [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md) — IDE + toolchain ngôn ngữ + quy trình dev (tiếng Việt)
- [../../SYSTEM_REQUIREMENTS.md](../../SYSTEM_REQUIREMENTS.md) — yêu cầu hệ thống cho người dùng cuối
- [../../pc_spec.md](../../pc_spec.md) — bản gốc tiếng Anh
