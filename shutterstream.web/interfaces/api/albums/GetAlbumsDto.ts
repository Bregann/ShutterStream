export interface GetAlbumsDto {
  albums: AlbumSummaryItem[]
}

export interface AlbumSummaryItem {
  albumId: number
  title: string
  description: string
  photoCount: number
  coverPhotoId: number | null
  coverPhotoVersion: number | null
  albumDate: string | null
}
