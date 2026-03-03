# .NET Distributed Warehouse System

A proof-of-concept distributed system built with **.NET**, demonstrating event-driven architecture, clean code principles, and resilient message processing.

---

## 🏗 Architecture & Tech Stack

The project is built following **Clean Architecture** (API -> Application -> Domain -> Infrastructure) to ensure separation of concerns and testability.


### Technology Highlights

| Component | Technology |
| :--- | :--- |
| **Framework** | .NET 10 / C# |
| **Communication** | [Wolverine](https://wolverine.netlify.app/) (Message Bus & Mediator) |
| **Security** | JWT Authentication with **RBAC** (Role-Based Access Control) |
| **Persistence** | EF Core with PostgreSQL |
| **Infrastructure** | Docker & Docker Compose |
| **Testing** | xUnit, TestContainers, WebApplicationFactory |

---

## 🚀 Services

### 1. InventoryService
* **Role**: Manages the source of truth for stock entries.
* **Endpoints**: `POST /inventory` (Requires `write` role).
* **Behavior**: Persists entries to the database and publishes `ProductInventoryAddedEvent`.

### 2. ProductService
* **Role**: Manages product catalog and aggregated stock levels.
* **Endpoints**: 
    * `POST /products` (Requires `write` role)
    * `GET /products` (Requires `read` role)
* **Behavior**: Consumes events from `InventoryService`. Includes **idempotency logic** to ensure consistent state even if messages are retried.

---

## ✨ Key Features

* **Event-Driven Consistency**: Seamless asynchronous communication between microservices using Wolverine.
* **Resilience & Reliability**: Built-in idempotency checks and message tracking to prevent data duplication.
* **Secure by Design**: Role-based security ensuring only authorized users can modify inventory or view products.
* **True E2E Testing**: Automated test suite utilizing **TestContainers** to spin up real PostgreSQL and Message Bus instances during runtime.

---

## 🛠 Getting Started

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Running the System
To spin up all services and the necessary infrastructure (PostgreSQL, etc.):

# Start the system
docker-compose up -d
