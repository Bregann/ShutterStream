export interface GetAdminAlbumDto {
  albumId: number
  folderName: string
  title: string
  description: string
  isPublic: boolean
  coverPhotoId: number | null
  photos: AdminPhotoItem[]
  captionStatus: AlbumCaptionStatus
}

export interface AlbumCaptionStatus {
  aiCaptionsEnabled: boolean
  isRunning: boolean
  isOtherAlbumRunning: boolean
  processed: number
  failed: number
  total: number
  lastCompletedAt: string | null
  lastMessage: string | null
}

export interface AdminPhotoItem {
  photoId: number
  fileName: string
  width: number
  height: number
  version: number
  isHidden: boolean
  viewCount: number
  title: string | null
  description: string | null
  isAiCaption: boolean
}
