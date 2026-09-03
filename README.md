# Exchange Rate API

A production-oriented **.NET 8 Web API** built around a simple exchange-rate use case to demonstrate modern backend architecture, engineering practices, and DevOps tooling.

## What this project demonstrates

- **Clean Architecture** & **CQRS**
- **MediatR** and Dependency Inversion
- **ASP.NET Core Web API**
- Centralized **RFC 7807 ProblemDetails** exception handling
- Application-level validation with **TimeProvider**
- External API integration with typed **HttpClient**
- **Unit & Integration Testing** with xUnit and NSubstitute
- **Code Coverage** with Coverlet
- **SonarCloud** static analysis & Quality Gate
- **Docker** multi-stage builds & non-root containers
- **GitHub Actions** CI/CD
- **GitHub Container Registry (GHCR)**
- Branch protection & automated **AI Code Review** powered by Gemini

## Architecture 

    API
     │
     ▼
    Application
    (CQRS / MediatR)
     │
     ▼
    Domain
     ▲
     │
    Infrastructure
     │
     ▼
    Frankfurter API

## Live Demo

🚀 **Swagger:**  
https://exchangerateapi-rzyg.onrender.com/swagger/index.html

> The exchange-rate functionality is intentionally simple. The main goal of this project is to demonstrate how a simple business problem can be implemented with production-oriented architecture, testing, security, CI/CD, AI-assisted development, and code-quality practices.
