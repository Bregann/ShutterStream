import AlbumComponent from '@/components/pages/AlbumComponent'
import { doQueryGet } from '@/helpers/apiClient'
import { getPhotoImageUrl } from '@/helpers/photoHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'
import type { GetAlbumDto } from '@/interfaces/api/albums/GetAlbumDto'
import { dehydrate, HydrationBoundary, QueryClient } from '@tanstack/react-query'
import { cookies } from 'next/headers'
import { notFound } from 'next/navigation'
import type { Metadata } from 'next'
import { cache } from 'react'

// Cookies are forwarded so the admin can preview private albums. Wrapped in cache so
// generateMetadata and the page share a single request
const getAlbum = cache(async (albumId: string): Promise<GetAlbumDto | null> => {
  const cookieStore = await cookies()
  const cookieHeader = cookieStore
    .getAll()
    .map(c => `${c.name}=${c.value}`)
    .join('; ')

  try {
    return await doQueryGet<GetAlbumDto>(`/api/Albums/GetAlbum?albumId=${albumId}`, { cookieHeader })
  } catch {
    return null
  }
})

export async function generateMetadata({ params }: { params: Promise<{ albumId: string }> }): Promise<Metadata> {
  const { albumId } = await params
  const album = await getAlbum(albumId)

  if (album === null) {
    return { title: 'Album not found' }
  }

  const cover = album.photos[0]

  return {
    title: album.title,
    description: album.description !== '' ? album.description : `${album.photos.length} photos`,
    robots: album.isPublic ? undefined : { index: false },
    openGraph: cover !== undefined
      ? { images: [getPhotoImageUrl(cover.photoId, PhotoSize.Large, cover.version)] }
      : undefined
  }
}

export default async function AlbumPage({ params }: { params: Promise<{ albumId: string }> }) {
  const { albumId } = await params
  const album = await getAlbum(albumId)

  if (album === null) {
    notFound()
  }

  const queryClient = new QueryClient()
  queryClient.setQueryData([QueryKeys.Album, albumId], album)

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <AlbumComponent albumId={parseInt(albumId)} />
    </HydrationBoundary>
  )
}
