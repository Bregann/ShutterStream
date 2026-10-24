import AlbumComponent from '@/components/pages/AlbumComponent'
import { doQueryGet } from '@/helpers/apiClient'
import { formatAlbumDate } from '@/helpers/dateHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import { buildShareMetadata } from '@/helpers/seo'
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

  const cover = album.photos.find(p => p.photoId === album.coverPhotoId) ?? album.photos.at(0)

  // e.g. "42 photos · March 2025", shown under the description so the preview always says what's in it
  const summary = [
    `${album.photos.length} ${album.photos.length === 1 ? 'photo' : 'photos'}`,
    album.albumDate !== null ? formatAlbumDate(album.albumDate) : null
  ].filter(part => part !== null).join(' · ')

  return {
    ...buildShareMetadata({
      title: album.title,
      description: album.description !== '' ? `${album.description}\n\n${summary}` : summary,
      path: `/album/${album.albumId}`,
      photo: cover,
      imageAlt: cover?.title ?? album.title
    }),
    robots: album.isPublic ? undefined : { index: false }
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
