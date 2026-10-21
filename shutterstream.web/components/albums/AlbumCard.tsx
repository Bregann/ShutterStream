'use client'

import { AspectRatio, Image, Stack, Text } from '@mantine/core'
import { IconPhoto } from '@tabler/icons-react'
import Link from 'next/link'
import classes from '@/css/albumCard.module.css'
import { getPhotoImageUrl } from '@/helpers/photoHelper'
import { formatAlbumDate } from '@/helpers/dateHelper'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'
import type { AlbumSummaryItem } from '@/interfaces/api/albums/GetAlbumsDto'

interface AlbumCardProps {
  album: AlbumSummaryItem
}

export default function AlbumCard({ album }: AlbumCardProps) {
  return (
    <Link href={`/album/${album.albumId}`} className={classes.card}>
      <AspectRatio ratio={4 / 3}>
        {album.coverPhotoId !== null && album.coverPhotoVersion !== null ? (
          <Image
            src={getPhotoImageUrl(album.coverPhotoId, PhotoSize.Small, album.coverPhotoVersion)}
            alt={album.title}
            className={classes.image}
            loading="lazy"
          />
        ) : (
          <div className={classes.placeholder}>
            <IconPhoto size="3rem" />
          </div>
        )}
      </AspectRatio>

      <div className={classes.overlay}>
        <Stack gap={2}>
          <Text fw={600} size="lg" lineClamp={1}>{album.title}</Text>
          <Text size="sm" c="gray.4">
            {album.photoCount} {album.photoCount === 1 ? 'photo' : 'photos'}
            {album.albumDate !== null && ` · ${formatAlbumDate(album.albumDate)}`}
          </Text>
        </Stack>
      </div>
    </Link>
  )
}
