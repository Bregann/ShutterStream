import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'

/**
 * Builds the url for a resized photo. The version comes from the file's modified time on the
 * server, so the image can be cached forever and still refreshes if the file changes.
 */
export const getPhotoImageUrl = (photoId: number, size: PhotoSize, version: number): string => {
  return `/api/Photos/GetPhotoImage?photoId=${photoId}&size=${size}&v=${version}`
}

/**
 * Builds the url that saves the full resolution photo as a file rather than opening it
 */
export const getPhotoDownloadUrl = (photoId: number, version: number): string => {
  return `${getPhotoImageUrl(photoId, PhotoSize.Full, version)}&download=true`
}

export interface PhotoSection<T> {
  subFolder: string
  photos: T[]
}

/**
 * Splits an album's photos into a section per sub folder (e.g. one per day of a holiday).
 * The API already returns them grouped and in order, so each run of the same folder is a section.
 */
export const groupPhotosBySubFolder = <T extends { subFolder: string }>(photos: T[]): PhotoSection<T>[] => {
  const sections: PhotoSection<T>[] = []

  for (const photo of photos) {
    const current = sections.at(-1)

    if (current !== undefined && current.subFolder === photo.subFolder) {
      current.photos.push(photo)
    } else {
      sections.push({ subFolder: photo.subFolder, photos: [photo] })
    }
  }

  return sections
}

/**
 * Turns a nested sub folder path into a heading, e.g. "Day 2/Evening" into "Day 2 / Evening"
 */
export const formatSubFolder = (subFolder: string): string => {
  return subFolder.split('/').join(' / ')
}

/**
 * Builds a readable camera name. Most makers repeat the brand in the model
 * (e.g. "NIKON CORPORATION" + "NIKON Z 6_2"), so the make is dropped when that happens.
 */
export const formatCameraName = (make: string | null, model: string | null): string | null => {
  if (model === null) {
    return make
  }

  if (make === null) {
    return model
  }

  const brand = make.split(' ')[0].toLowerCase()
  return model.toLowerCase().startsWith(brand) ? model : `${make} ${model}`
}

/**
 * Builds the exposure summary shown under a photo, e.g. "35mm · f/5.6 · 1/250s · ISO 200".
 * Returns null when the photo has no exposure data at all.
 */
export const formatExposureSettings = (
  focalLength: number | null,
  aperture: number | null,
  exposureTime: string | null,
  iso: number | null
): string | null => {
  const parts = [
    focalLength !== null ? `${focalLength}mm` : null,
    aperture !== null ? `f/${aperture}` : null,
    exposureTime !== null ? `${exposureTime}s` : null,
    iso !== null ? `ISO ${iso}` : null
  ].filter((part): part is string => part !== null)

  return parts.length > 0 ? parts.join(' · ') : null
}
