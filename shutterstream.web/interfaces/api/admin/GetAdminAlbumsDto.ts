export interface GetAdminAlbumsDto {
  albums: AdminAlbumItem[]
}

export interface AdminAlbumItem {
  albumId: number
  folderName: string
  title: string
  isPublic: boolean
  photoCount: number
  hiddenPhotoCount: number
  coverPhotoId: number | null
  coverPhotoVersion: number | null
  albumDate: string | null
}
