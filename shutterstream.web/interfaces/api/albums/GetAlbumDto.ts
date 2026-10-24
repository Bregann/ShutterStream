export interface GetAlbumDto {
  albumId: number
  title: string
  description: string
  isPublic: boolean
  albumDate: string | null
  photos: AlbumPhotoItem[]
}

export interface AlbumPhotoItem {
  photoId: number
  width: number
  height: number
  version: number
  title: string | null
  subFolder: string
}
