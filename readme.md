# EdgeFront Builder

EdgeFront Builder is a split frontend/backend scaffold with a working end-to-end time display:

- `src/backend` is an ASP.NET Core minimal API (`net10.0`)
- `src/frontend` is a Next.js 16 App Router app (React 19 + TypeScript)
- The frontend calls the backend `GET /api/time` endpoint and renders the returned value on the home page

## Current Implementation

### Backend (`src/backend`)

- Runtime: `.NET 10` (`net10.0`)
- API style: Minimal API
- OpenAPI/Swagger enabled in development
- Application Insights telemetry configured
- Implemented endpoint:
	- `GET /api/time`
	- Response shape:

```json
{
	"time": "2026-02-17 14:03:49 GMT-05:00"
}
```

Default local URLs from launch settings:

- `http://localhost:5187`
- `https://localhost:7150`

### Frontend (`src/frontend`)

- Framework: `Next.js 16` (App Router)
- Language: `TypeScript`
- Styling: Tailwind CSS v4 + shadcn styles
- Home page server component fetches backend time via `lib/get-time.ts`

Required environment variable:

- `BACKEND_API_BASE_URL` (example: `http://localhost:5187`)

## Project Structure

```text
docs/
src/
	backend/   # ASP.NET Core minimal API
	frontend/  # Next.js app
tests/
```

## Local Development

### 1) Run the backend

From `src/backend`:

```bash
dotnet restore
dotnet run
```

### 2) Configure frontend environment

From `src/frontend`, create `.env.local`:

```env
BACKEND_API_BASE_URL=http://localhost:5187
```

### 3) Run the frontend

From `src/frontend`:

```bash
npm install
npm run dev
```

Open `http://localhost:3000`.

## Available Frontend Scripts

From `src/frontend`:

- `npm run dev`
- `npm run build`
- `npm run start`
- `npm run lint`
- `npm run lint:fix`

## Notes

- The backend sample request file `src/backend/backend.http` still targets `/weatherforecast`, but the implemented endpoint is `/api/time`.
