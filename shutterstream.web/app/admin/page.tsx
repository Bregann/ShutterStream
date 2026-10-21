import AdminComponent from '@/components/pages/AdminComponent'
import { doQueryGet } from '@/helpers/apiClient'
import type { GetAdminAlbumsDto } from '@/interfaces/api/admin/GetAdminAlbumsDto'
import type { GetLibraryStatusDto } from '@/interfaces/api/admin/GetLibraryStatusDto'
import { dehydrate, HydrationBoundary, noop, QueryClient } from '@tanstack/react-query'
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

    // Independent requests, so fetch them in parallel
    await Promise.all([
      queryClient.query({
        queryKey: [QueryKeys.AdminAlbums],
        queryFn: async () => await doQueryGet<GetAdminAlbumsDto>('/api/Admin/GetAdminAlbums', { cookieHeader })
      }).catch(noop),
      queryClient.query({
        queryKey: [QueryKeys.LibraryStatus],
        queryFn: async () => await doQueryGet<GetLibraryStatusDto>('/api/Admin/GetLibraryStatus', { cookieHeader })
      }).catch(noop)
    ])
  }

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <AdminComponent />
    </HydrationBoundary>
  )
}
