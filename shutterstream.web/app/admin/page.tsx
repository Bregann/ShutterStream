import AdminComponent from '@/components/pages/AdminComponent'
import { doQueryGet } from '@/helpers/apiClient'
import type { GetAdminAlbumsDto } from '@/interfaces/api/admin/GetAdminAlbumsDto'
import type { GetLibraryStatusDto } from '@/interfaces/api/admin/GetLibraryStatusDto'
import { dehydrate, HydrationBoundary, QueryClient } from '@tanstack/react-query'
import { cookies } from 'next/headers'
import { QueryKeys } from '@/helpers/QueryKeys'

export default async function AdminPage() {
  const queryClient = new QueryClient()
  const cookieStore = await cookies()

  if (cookieStore.has('accessToken')) {
    const cookieHeader = cookieStore
      .getAll()
      .map(c => `${c.name}=${c.value}`)
      .join('; ')

    await queryClient.prefetchQuery({
      queryKey: [QueryKeys.AdminAlbums],
      queryFn: async () => await doQueryGet<GetAdminAlbumsDto>('/api/Admin/GetAdminAlbums', { cookieHeader })
    })

    await queryClient.prefetchQuery({
      queryKey: [QueryKeys.LibraryStatus],
      queryFn: async () => await doQueryGet<GetLibraryStatusDto>('/api/Admin/GetLibraryStatus', { cookieHeader })
    })
  }

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <AdminComponent />
    </HydrationBoundary>
  )
}
