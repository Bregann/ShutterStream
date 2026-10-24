# ShutterStream

> A self-hosted, Flickr style photo gallery built with .NET and Next.js

ShutterStream turns a folder of photo folders into a public gallery. Every top level folder in the photo library becomes an album. Albums are **private when first found** and only appear on the site once they're published from the admin area.

---

## ✨ Features

### 📷 Gallery
- **Albums**: Cover photo grid, ordered by the most recent photo taken
- **Justified photo grid**: Flickr style rows that keep every photo's aspect ratio
- **Photo viewer**: Large photo with previous/next buttons, arrow key navigation and preloading
- **Photo details**: Camera, lens, focal length, aperture, shutter speed, ISO and date taken from EXIF
- **Privacy**: Originals are never served and generated images have all metadata (including GPS) stripped

### 🔐 Admin
- **Library scanning**: Hourly scan plus an on-demand button with live progress
- **Publishing**: Make albums public or private with a single switch
- **Album details**: Edit the title and description, pick the cover photo
- **Hide photos**: Hide individual photos within a public album
- **Preview**: Private albums and hidden photos are viewable while logged in
- **AI captions (optional)**: One button per album has Google Gemini write a title and description for every photo. Captions can be edited by hand, and hand edited captions are never overwritten

---

## 🏗️ Architecture

### Tech Stack

**Backend (ShutterStream.Core & ShutterStream.Domain)**
- **Framework**: .NET 10
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10
- **API**: RESTful with JWT authentication
- **Background Jobs**: Hangfire for library scans
- **Images**: SkiaSharp for thumbnails, MetadataExtractor for EXIF
- **AI captions**: Google Gemini (`generateContent` REST API)
- **Logging**: Serilog with Seq integration

**Frontend (shutterstream.web)**
- **Framework**: Next.js 16 (App Router)
- **Language**: TypeScript
- **UI Library**: Mantine v9
- **State**: TanStack Query (React Query)

### How the library works

```
/photos                         <- PhotoLibraryPath (mounted read only)
├── 2024-06 Lake District       <- album
│   ├── DSC_0001.jpg
│   └── DSC_0002.jpg
└── 2025-01 Iceland             <- album
    └── ...
```

- The folder name becomes the album title, which can be renamed in the admin area. The folder itself is never modified.
- Supported formats: JPEG, PNG, WebP, GIF and BMP. RAW, HEIC and TIFF files are ignored, so RAW + JPEG folders work fine.
- Two sizes are generated per photo into `PhotoCachePath`: 1024px for grids and 2048px for the viewer.
- Changed files are picked up by size and modified time, and their thumbnails are regenerated.
- Deleting a folder removes its album. If the **whole** library looks empty the scan refuses to run, so an unmounted network drive can't wipe every album and its settings.

---

## 🚦 Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 26+
- Docker (the debug API and the tests start PostgreSQL in a container)

### Running locally

1. **Start the API** (`ShutterStream.Core`, profile `ShutterStreamApi`). In debug it:
   - starts a throwaway PostgreSQL container and seeds an admin login of `admin` / `test123!`
   - creates a sample library of generated photos in `ShutterStream.Core/DevPhotoLibrary` **only if that folder doesn't exist**
   - runs a library scan straight away

   To use real photos, point `PhotoLibraryPath` in `launchSettings.json` at a copy of your library.
   To try AI captions locally, set `GEMINI_API_KEY` before starting the API.

2. **Start the website**:
   ```bash
   cd shutterstream.web
   npm install
   npm run dev
   ```

3. Open [http://localhost:3000](http://localhost:3000). The sample albums start private, so log in via the padlock and publish them from the admin area.

### Tests

```bash
dotnet test ShutterStream.Tests/ShutterStream.Tests.csproj
```

### Adding migrations

```bash
dotnet ef migrations add <name> --project ShutterStream.Domain --startup-project ShutterStream.Domain -o Database/Migrations
```

Release builds apply pending migrations on startup.

---

## 🐳 Deployment

### API environment variables

| Variable | Description |
|---|---|
| `ShutterStreamConnString` | PostgreSQL connection string |
| `JwtKey`, `JwtValidIssuer`, `JwtValidAudience` | JWT signing settings |
| `PhotoLibraryPath` | Photo library folder, defaults to `/photos` (mount read only) |
| `PhotoCachePath` | Generated thumbnails, defaults to `/app/PhotoCache` (needs to be persisted) |
| `AdminUsername`, `AdminPassword` | Creates the admin login on first start. Ignored once a user exists |

Migrations run automatically on start, but the environmental settings are added to the database by hand before the first start: rows in `EnvironmentalSettings` for `HangfireUsername`, `HangfirePassword`, `GeminiApiKey` (leave the value empty to turn AI captions off) and `GeminiModel` (e.g. `gemini-3.8-flash`). The API won't start without all four

### How AI captions work

- Captions are only generated when you press the button on an album's admin page - nothing is sent to Gemini automatically.
- Each photo's 1024px thumbnail is sent (never the original), so EXIF and GPS data never leave the server. The album title and date taken are sent as hints.
- One album is captioned at a time, 4 photos in parallel, and progress shows live on the album page.
- By default only photos without a caption are sent. "Regenerate AI captions" redoes the AI written ones but still skips any you've edited.

### Website environment variables

| Variable | Description |
|---|---|
| `API_BASE_URL` | The API's base url including `/api`, e.g. `http://shutterstream-api:8080/api` |
