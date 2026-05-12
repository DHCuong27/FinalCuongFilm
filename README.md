# FinalCuongFilm

Hệ thống quản lý và hiển thị phim (FinalCuongFilm) được xây dựng theo kiến trúc nhiều lớp, bao gồm API, MVC, dịch vụ nghiệp vụ và tầng dữ liệu. Dự án tập trung vào khả năng mở rộng, bảo trì và triển khai thực tế trong môi trường doanh nghiệp.

---

## 📌 Mục tiêu dự án

- Xây dựng hệ thống quản lý phim theo mô hình nhiều tầng (Layered Architecture).
- Cung cấp API phục vụ cho client (web/mobile).
- Tách biệt rõ ràng các tầng nghiệp vụ, dữ liệu và giao diện.
- Dễ dàng triển khai bằng Docker.

---

## 🧱 Kiến trúc tổng thể

Dự án được tổ chức theo mô hình nhiều tầng:

- **FinalCuongFilm.API**: Web API phục vụ dữ liệu.
- **FinalCuongFilm.MVC**: Giao diện MVC.
- **FinalCuongFilm.ApplicationCore**: Chứa logic nghiệp vụ và domain.
- **FinalCuongFilm.Service**: Tầng service xử lý nghiệp vụ.
- **FinalCuongFilm.Datalayer**: Tầng truy cập dữ liệu (DAL).
- **FinalCuongFilm.Common**: Các tiện ích dùng chung.

---

## ⚙️ Công nghệ sử dụng

### Backend
- **.NET / C#**
- **ASP.NET Core Web API**
- **ASP.NET Core MVC**
- **Entity Framework Core** (dự kiến/đề xuất cho tầng DAL)

### Frontend
- **HTML**
- **CSS**

### DevOps / Triển khai
- **Docker** (có sẵn `Dockerfile`)

---

## 📁 Cấu trúc thư mục

```
FinalCuongFilm/
├── FinalCuongFilm.API/              # API layer
├── FinalCuongFilm.MVC/              # MVC UI layer
├── FinalCuongFilm.ApplicationCore/  # Domain & business logic
├── FinalCuongFilm.Service/          # Service layer
├── FinalCuongFilm.Datalayer/        # Data access layer
├── FinalCuongFilm.Common/           # Common utilities
├── FinalCuongFilm.sln               # Solution file
└── Dockerfile                       # Docker build file
```

---

## 🚀 Hướng dẫn chạy dự án

### 1. Clone repository
```bash
git clone https://github.com/DHCuong27/FinalCuongFilm.git
cd FinalCuongFilm
```

### 2. Mở bằng Visual Studio
- Mở file `FinalCuongFilm.sln`.
- Chạy dự án API hoặc MVC tùy mục tiêu.

### 3. Chạy bằng Docker (tùy cấu hình)
```bash
docker build -t finalcuongfilm .
docker run -p 5000:5000 finalcuongfilm
```

---

## ✅ Quy ước & chuẩn nghiệp vụ

- Tách lớp rõ ràng theo kiến trúc nhiều tầng.
- Dùng DTO/Model riêng cho API để tránh rò rỉ Domain.
- Service chịu trách nhiệm xử lý nghiệp vụ, không đặt logic trong Controller.
- Datalayer tập trung truy cập dữ liệu, repository pattern khuyến nghị.

---

## 📌 Định hướng mở rộng

- Tích hợp xác thực (JWT/Identity).
- Thêm phân quyền người dùng.
- Bổ sung caching và logging (Serilog, Redis).
- Viết unit test cho tầng Service và ApplicationCore.

---

## 📄 License

Dự án thuộc quyền sở hữu của **DHCuong27**.  
Vui lòng liên hệ tác giả nếu muốn sử dụng cho mục đích thương mại.
