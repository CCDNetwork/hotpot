# Contributing to Hotpot

Thank you for your interest in contributing to the Hotpot platform! We welcome contributions from everyone. By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Getting Started

The project is split into two main components: a frontend `client` (Vite + React) and a backend `server` (ASP.NET Core).

### Prerequisites

* [Node.js](https://nodejs.org/) 20 LTS (for the frontend client)
* [Yarn](https://yarnpkg.com/) (the repo commits `yarn.lock`; do not mix with npm)
* [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (for the backend server)
* [Docker](https://www.docker.com/) and Docker Compose (required for the local PostgreSQL database, unless you provision one yourself)

### 1. Start the database

From the `server/` directory, bring up PostgreSQL via the bundled compose file:

```bash
cd server
docker compose up -d
```

This starts a Postgres container on localhost:5432 with credentials matching the defaults in `server/Ccd.Server/appsettings.json`. If you provision Postgres another way, update the `ConnectionStrings:CcdServerDB` value to match.

### 2. Run the backend (server)

```bash
cd server/Ccd.Server
dotnet restore
dotnet run
```

The API starts on http://localhost:5153 (and https://localhost:7172), with Swagger at `/swagger`. Database migrations are applied automatically on startup.

> [!NOTE]
> `appsettings.json` contains a hardcoded `StoragePath` pointing at a developer’s local machine. Override it for your environment via `appsettings.Development.json`, an environment variable (`AppSettings__StoragePath` / `StoragePath`), or user secrets before uploading files locally.

### 3. Run the frontend (client)

```bash
cd client
yarn install
yarn dev
```

The dev server starts on http://localhost:3000 and proxies to the backend.

## How to Contribute

### 1. Report a Bug

If you find a bug, please create an issue using the Bug Report template. Provide as much detail as possible, including steps to reproduce, expected behavior, and actual behavior.

### 2. Request a Feature

If you have a feature request, please create an issue using the Feature Request template. Explain the motivation for the feature and how it would benefit the project.

### 3. Submit a Pull Request

If you'd like to contribute code:

1.  **Fork the repository** and clone your fork locally.
2.  **Create a new branch** for your feature or bug fix: `git checkout -b feature/your-feature-name` or `bugfix/issue-number`.
3.  **Make your changes**, ensuring they follow the existing coding style and include tests if applicable.
4.  **Commit your changes** with descriptive commit messages.
5.  **Push to your fork** and submit a **Pull Request** against the `main` branch.

Please ensure your PR description clearly describes the changes and references any related issues.

## Coding Standards

*   For the frontend, we use ESLint and Prettier. Please run the linters before submitting a PR.
*   For the backend, please follow standard C# naming conventions and coding guidelines.

We look forward to your contributions!
