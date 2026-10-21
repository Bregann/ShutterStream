export interface GetPhotoDto {
  photoId: number
  title: string | null
  description: string | null
  albumId: number
  albumTitle: string
  width: number
  height: number
  version: number
  positionInAlbum: number
  albumPhotoCount: number
  previousPhotoId: number | null
  nextPhotoId: number | null
  dateTaken: string | null
  cameraMake: string | null
  cameraModel: string | null
  lensModel: string | null
  focalLength: number | null
  aperture: number | null
  exposureTime: string | null
  iso: number | null
}
