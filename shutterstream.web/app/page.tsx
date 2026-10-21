import HomeComponent from '@/components/pages/HomeComponent'
import { doQueryGet } from '@/helpers/apiClient'
import type { GetAlbumsDto } from '@/interfaces/api/albums/GetAlbumsDto'
import { dehydrate, HydrationBoundary, QueryClient } from '@tanstack/react-query'
import { QueryKeys } from '@/helpers/QueryKeys'

export default async function Home() {
  const queryClient = new QueryClient()

  // The album list is the same for everyone, so no cookies need forwarding
  await queryClient.prefetchQuery({
    queryKey: [QueryKeys.Albums],
    queryFn: async () => await doQueryGet<GetAlbumsDto>('/api/Albums/GetAlbums')
  })

  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <HomeComponent />
    </HydrationBoundary>
  )
}
