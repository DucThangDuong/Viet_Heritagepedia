# Kiến trúc & DDD - TravelApp Backend

## 1. Clean Architecture Layers

### Domain Layer (`TravelApp.Domain/`)
- **Chứa**: Entities, Value Objects, Aggregates, Domain Events, Repository Interfaces, Domain Services
- **Không phụ thuộc**: Vào bất kỳ layer nào khác
- **Ví dụ**:
  - `Tour.cs` (Aggregate Root)
  - `Money.cs` (Value Object)
  - `BookingCreatedEvent.cs` (Domain Event)
  - `ITourRepository.cs` (Repository Interface)

### Application Layer (`TravelApp.Application/`)
- **Chứa**: Commands, Queries, Handlers, DTOs, Validators, Pipeline Behaviors
- **Chỉ phụ thuộc**: Domain Layer
- **Ví dụ**:
  - `CreateTourCommand.cs`
  - `CreateTourCommandHandler.cs`
  - `CreateTourCommandValidator.cs`
  - `LoggingBehavior.cs`

### Infrastructure Layer (`TravelApp.Infrastructure/`)
- **Chứa**: Persistence (EF Core + Dapper), Repositories, Messaging, Caching, Email, File Storage
- **Phụ thuộc**: Application + Domain
- **Ví dụ**:
  - `TourRepository.cs` (implement ITourRepository)
  - `TravelDbContext.cs` (EF Core)
  - `RedisCacheService.cs`

### API Layer (`TravelApp.API/`)
- **Chứa**: Controllers, Middleware, Filters
- **Phụ thuộc**: Infrastructure + Application + Domain

## 2. DDD Tactical Patterns

### Aggregate Root
- **Đặc điểm**: Là entity gốc, là điểm truy cập duy nhất vào aggregate
- **Ví dụ**: `Tour`, `Booking`, `User`

### Entity
- **Đặc điểm**: Có danh tính (Identity)
- **Ví dụ**: `TourItinerary`, `Payment`

### Value Object
- **Đặc điểm**: Immutable, không có danh tính, so sánh theo giá trị
- **Ví dụ**: `Money`, `Address`, `DateRange`, `Location`

### Domain Event
- **Đặc điểm**: Sự kiện xảy ra trong domain
- **Ví dụ**: `TourBookedEvent`, `PaymentConfirmedEvent`

### Domain Service
- **Đặc điểm**: Business logic không thuộc về một entity cụ thể
- **Ví dụ**: `PricingService`, `AvailabilityService`

## 3. CQRS Implementation

### Commands (Write)
- Sử dụng **EF Core**
- Đảm bảo **transactional consistency**
- Ví dụ: `CreateTourCommand`, `UpdateBookingCommand`

### Queries (Read)
- Sử dụng **Dapper**
- Tối ưu hiệu năng
- Ví dụ: `GetTourByIdQuery`, `SearchToursQuery`

## 4. Modules (Bounded Contexts)

| Module | Trách nhiệm | Aggregates chính |
|--------|-------------|------------------|
| TourManagement | Quản lý tour, itinerary, giá, availability | `Tour`, `TourCategory`, `Destination` |
| Booking | Đặt tour, thanh toán, xác nhận | `Booking`, `BookingItem`, `Payment` |
| User | Quản lý người dùng, profile, preferences | `User`, `UserProfile`, `Wishlist` |
| Content | Quản lý nội dung (bài viết, hình ảnh, review) | `Article`, `Review`, `Media` |
| Notification | Thông báo real-time, email | `Notification`, `NotificationTemplate` |
| Payment | Xử lý thanh toán, invoice | `Invoice`, `Transaction` |

**Mỗi module có DbContext riêng và schema riêng trong database.**