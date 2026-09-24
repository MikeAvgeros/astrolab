# AstroLab Web

A small React single-page application for using the AstroLab API from a browser.

It is a client of the HTTP API only and contains no scientific logic. Every value it shows comes from an API response.

## Features

- **Staged files**: upload a FITS file (streamed as the raw request body to `POST /api/fits/upload`), or add an existing file ID. The API has no endpoint that lists staged files, so the browser remembers the file IDs it has seen in `localStorage`. **Forget** only removes an ID from that list; it does not delete the file on the server.
- **Image viewer**: renders the active file through `/render`, `/render/overlay` or `/render/wcs-grid` with stretch and colour-map controls, alongside `/statistics`.
- **Archive search**: searches ESO or MAST via `/api/archives/search` and stages a result with `/api/archives/download`.
- **All endpoints**: builds a form for every operation in the API's OpenAPI document (`/openapi/v1.json`), so new endpoints appear without changes to the SPA. Path and query parameters get typed inputs, `fileId` fields default to the active file, and JSON request bodies start from a template built from the schema.
  - PNG responses are shown as images.
  - JSON responses get a summary of the scalar values and tables for arrays of objects.
  - Numeric arrays (spectra, light curves, histograms) can be plotted.
  - The raw response is always available.

## Running

### With Docker Compose (API + web UI)

From the repository root:

```bash
docker compose up -d
```

Open `http://localhost:3000`. The `web` container is nginx serving the built bundle. It forwards `/api` and `/openapi` to the `astrolab` API container, streaming large FITS uploads straight through with no nginx body-size limit (the API enforces `Storage:MaxUploadSizeBytes`). Set `ASTROLAB_API_URL` on the `web` service to point it at a different API.

### For development

Requirements: Node.js 20+ and a running AstroLab API.

```bash
# Terminal 1: API on http://localhost:5279
dotnet run --project src/AstroLab.Api

# Terminal 2: SPA on http://localhost:5173 (hot reload)
cd web
npm install
npm run dev
```

The Vite dev server proxies `/api` and `/openapi` to the API, so the API needs no CORS configuration. To target a different API, e.g. the Docker container, set `ASTROLAB_API_URL`:

```bash
ASTROLAB_API_URL=http://localhost:8080 npm run dev
```

## Scripts

| Command | Purpose |
| --- | --- |
| `npm run dev` | Start the dev server with the API proxy |
| `npm run build` | Type-check and produce a production bundle in `dist/` |
| `npm run typecheck` | Type-check only |
| `npm run preview` | Serve the production bundle, also with the API proxy |
