export interface GetLibraryStatusDto {
  libraryPath: string
  libraryPathExists: boolean
  totalAlbums: number
  publicAlbums: number
  totalPhotos: number
  pendingThumbnails: number
  isScanRunning: boolean
  scanProcessed: number
  scanTotal: number
  lastScanStartedAt: string | null
  lastScanCompletedAt: string | null
  lastScanMessage: string | null
}
