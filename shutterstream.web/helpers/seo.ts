import type { Metadata } from 'next'
import { getPhotoImageUrl } from '@/helpers/photoHelper'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'

export const SITE_NAME = 'ShutterStream'
export const SITE_URL = 'https://shutterstream.bregan.me'

export const absoluteUrl = (path: string): string => `${SITE_URL}${path}`

// Matches the Large size the API generates
const LARGE_MAX_DIMENSION = 2048

interface SharedPhoto {
  photoId: number
  version: number
  width: number
  height: number
}

interface ShareMetadataOptions {
  title: string
  description: string
  path: string
  photo?: SharedPhoto
  imageAlt?: string
}

/**
 * Builds the title, description and link preview (Open Graph and Twitter/X card) for a page,
 * so a shared link shows up in Discord, WhatsApp, iMessage, X etc. with its photo.
 *
 * Next replaces rather than merges openGraph between the layout and a page, so everything
 * a preview needs is set here rather than relying on the layout.
 */
export const buildShareMetadata = ({ title, description, path, photo, imageAlt }: ShareMetadataOptions): Metadata => {
  // Width and height let the apps lay out the preview before they download the image
  const scale = photo !== undefined ? Math.min(1, LARGE_MAX_DIMENSION / Math.max(photo.width, photo.height)) : 1
  const image = photo !== undefined
    ? {
      url: getPhotoImageUrl(photo.photoId, PhotoSize.Large, photo.version),
      width: Math.round(photo.width * scale),
      height: Math.round(photo.height * scale),
      alt: imageAlt ?? title
    }
    : undefined

  return {
    title,
    description,
    alternates: { canonical: path },
    openGraph: {
      type: 'website',
      siteName: SITE_NAME,
      title,
      description,
      url: path,
      images: image !== undefined ? [image] : undefined
    },
    twitter: {
      card: image !== undefined ? 'summary_large_image' : 'summary',
      title,
      description,
      images: image !== undefined ? [{ url: image.url, alt: image.alt }] : undefined
    }
  }
}
