# FinalCuongFilm - Movie Streaming Platform

[![Live Demo](https://img.shields.io/badge/Live_Demo-cuongfilm.site-e50914?style=for-the-badge&logo=google-chrome&logoColor=white)](https://www.cuongfilm.site/)

Hệ thống quản lý và hiển thị phim trực tuyến (FinalCuongFilm) được xây dựng theo kiến trúc N-Tier (Nhiều tầng), bao gồm Web API, MVC Client, Business Service và Data Access Layer. Dự án tập trung vào khả năng mở rộng, hiệu suất xử lý luồng video và tính sẵn sàng khi triển khai thực tế trên môi trường Cloud.

---

## 📌 Mục tiêu dự án

- Xây dựng nền tảng xem phim trực tuyến với trải nghiệm mượt mà, áp dụng các nguyên tắc thiết kế UI/UX và tương tác người máy (HCI).
- Áp dụng triệt để mô hình kiến trúc nhiều tầng (Layered Architecture) để phân tách trách nhiệm (Separation of Concerns).
- Cung cấp hệ thống RESTful API chuẩn mực phục vụ đa nền tảng (Web/Mobile).
- Xử lý các tác vụ nền phức tạp (Background Jobs) và tích hợp thanh toán điện tử.

---

## ⚙️ Công nghệ & Hệ sinh thái sử dụng

### Backend & Architecture
- **Framework:** .NET / C#
- **API & UI:** ASP.NET Core Web API, ASP.NET Core MVC
- **ORM:** Entity Framework Core
- **Background Processing:** Hangfire (Xử lý mã hóa video sang định dạng HLS m3u8)

### Database & Cloud Storage
- **Database:** Supabase (PostgreSQL) với Connection Pooling.
- **Storage:** Azure Blob Storage (Lưu trữ và phân phối nội dung Video Streaming).

### Frontend
- **UI Framework:** Bootstrap 5, FontAwesome
- **Ngôn ngữ:** HTML5, CSS3, JavaScript/jQuery

### Tích hợp bên thứ ba (Third-party)
- **Payment Gateway:** ZaloPay API (Tích hợp cơ chế Webhook & Polling).

### DevOps & Deployment
- **Containerization:** Docker (`Dockerfile` tích hợp sẵn).
- **Hosting:** Railway App.

---

## 🧱 Kiến trúc tổng thể & Cấu trúc thư mục

Dự án được tổ chức và liên kết chặt chẽ giữa các Project để đảm bảo tính đóng gói:

```text
FinalCuongFilm/
├── FinalCuongFilm.API/              # Điểm cuối API (Endpoints) cung cấp dữ liệu
├── FinalCuongFilm.MVC/              # Client giao diện người dùng (Web App)
├── FinalCuongFilm.ApplicationCore/  # Chứa Domain Models, DTOs và Interfaces
├── FinalCuongFilm.Service/          # Tầng xử lý Logic nghiệp vụ chính (Business Logic)
├── FinalCuongFilm.Datalayer/        # Tầng thao tác với cơ sở dữ liệu (Repositories/DbContext)
├── FinalCuongFilm.Common/           # Các tiện ích dùng chung (Constants, Helpers, Extensions)
├── FinalCuongFilm.sln               # Solution file
└── Dockerfile                       # Cấu hình đóng gói Docker image
