# CreatorFlow

CreatorFlow là hệ thống hỗ trợ Creator và nhóm sáng tạo nội dung quản lý, lập kế hoạch, sản xuất, kiểm duyệt và theo dõi hiệu suất nội dung đa nền tảng.

## Công nghệ

- C# / .NET 10
- Windows Forms
- PostgreSQL
- Npgsql
- REST API
- AI/LLM API

## Kiến trúc

CreatorFlow sử dụng kiến trúc phân tầng:

WinForms UI → Service → Repository → Npgsql → PostgreSQL

- **Forms / Controls:** giao diện và tương tác người dùng.
- **Models:** các model/domain model dùng trong hệ thống.
- **Services:** xử lý nghiệp vụ, phân quyền và workflow.
- **Repositories:** truy cập dữ liệu thông qua PostgreSQL.
- **Data:** cấu hình và quản lý kết nối cơ sở dữ liệu.
- **Helpers:** các tiện ích dùng chung.

## Git Workflow

```text
main
└── develop
    ├── feature/SCRUM-XX-...
    ├── feature/SCRUM-XX-...
    └── feature/SCRUM-XX-...
    