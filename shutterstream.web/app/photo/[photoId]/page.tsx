import PhotoComponent from '@/components/pages/PhotoComponent'
import { doQueryGet } from '@/helpers/apiClient'
import { QueryKeys } from '@/helpers/QueryKeys'
import { buildShareMetadata } from '@/helpers/seo'
import type { GetPhotoDto } from '@/interfaces/api/photos/GetPhotoDto'
import { dehydrate, HydrationBoundary, QueryClient } from '@tanstack/react-query'
import { cookies } from 'next/headers'
import { notFound } from 'next/navigation'
import type { Metadata } from 'next'
import { cache } from 'react'

// Cookies are forwarded so the admin can preview hidden photos. Wrapped in cache so
// generateMetadata and the page share a single request
const getPhoto = cache(async (photoId: string): Promise<GetPhotoDto | null> => {
  const cookieStore = await cookies()
  const cookieHeader = cookieStore
    .getAll()
    .map(c => `${c.name}=${c.value}`)
    .join('; ')

  try {
    return await doQueryGet<GetPhotoDto>(`/api/Photos/GetPhoto?photoId=${photoId}`, { cookieHeader })
  } catch {
    return null
  }
})

export async function generateMetadata({ params }: { params: Promise<{ photoId: string }> }): Promise<Metadata> {
  const { photoId } = await params
  const photo = await getPhoto(photoId)

  if (photo === null) {
    return { title: 'Photo not found' }
  }

  const title = photo.title ?? `${photo.albumTitle} (${photo.positionInAlbum} of ${photo.albumPhotoCount})`

  return buildShareMetadata({
    title,
    description: photo.description ?? `Photo ${photo.positionInAlbum} of ${photo.albumPhotoCount} from ${photo.albumTitle}`,
    path: `/photo/${photo.photoId}`,
    photo,
    imageAlt: title
  })
}

export default async function PhotoPage({ params }: { params: Promise<{ photoId: string }> }) {
  const { photoId } = await params
  const photo = await getPhoto(photoId)

  if (photo === null) {
    notFound()
  }

  const queryClient = new QueryClient()
  queryClient.setQueryData([QueryKeys.Photo, photoId], photo)

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <PhotoComponent photoId={parseInt(photoId)} />
    </HydrationBoundary>
  )
}
