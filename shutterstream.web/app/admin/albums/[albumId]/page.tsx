import AdminAlbumComponent from '@/components/pages/AdminAlbumComponent'
import { doQueryGet } from '@/helpers/apiClient'
import type { GetAdminAlbumDto } from '@/interfaces/api/admin/GetAdminAlbumDto'
import { dehydrate, HydrationBoundary, noop, QueryClient } from '@tanstack/react-query'
import { cookies } from 'next/headers'
import { QueryKeys } from '@/helpers/QueryKeys'

export default async function AdminAlbumPage({ params }: { params: Promise<{ albumId: string }> }) {
  const { albumId } = await params
  const queryClient = new QueryClient()
  const cookieStore = await cookies()

  if (cookieStore.has('accessToken')) {
    const cookieHeader = cookieStore
      .getAll()
      .map(c => `${c.name}=${c.value}`)
      .join('; ')

    await queryClient.query({
      queryKey: [QueryKeys.AdminAlbum, albumId],
      queryFn: async () => await doQueryGet<GetAdminAlbumDto>(`/api/Admin/GetAdminAlbum?albumId=${albumId}`, { cookieHeader })
    }).catch(noop)
  }

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <AdminAlbumComponent albumId={parseInt(albumId)} />
    </HydrationBoundary>
  )
}
