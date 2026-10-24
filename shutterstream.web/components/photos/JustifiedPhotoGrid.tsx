'use client'

import Link from 'next/link'
import classes from '@/css/justifiedGrid.module.css'
import { getPhotoImageUrl } from '@/helpers/photoHelper'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'
import type { AlbumPhotoItem } from '@/interfaces/api/albums/GetAlbumDto'

interface JustifiedPhotoGridProps {
  photos: AlbumPhotoItem[]
  // How many photos at the start load straight away. Sections further down the page pass 0
  eagerCount?: number
}

export default function JustifiedPhotoGrid({ photos, eagerCount = 12 }: JustifiedPhotoGridProps) {
  return (
    <div className={classes.grid}>
      {photos.map((photo, index) => {
        const aspectRatio = photo.width / photo.height

        return (
          <Link
            key={photo.photoId}
            href={`/photo/${photo.photoId}`}
            className={classes.item}
            style={{
              '--aspect-ratio': aspectRatio,
              flexGrow: aspectRatio * 100
            } as React.CSSProperties}
          >
            <i className={classes.spacer} style={{ paddingBottom: `${(1 / aspectRatio) * 100}%` }} />
            {/* Plain img rather than Mantine Image, the grid's CSS needs full control of its sizing */}
            <img
              src={getPhotoImageUrl(photo.photoId, PhotoSize.Small, photo.version)}
              alt={photo.title ?? ''}
              className={classes.image}
              // The first rows are usually on screen straight away, so don't lazy load them
              loading={index < eagerCount ? 'eager' : 'lazy'}
              decoding="async"
            />
          </Link>
        )
      })}
    </div>
  )
}
