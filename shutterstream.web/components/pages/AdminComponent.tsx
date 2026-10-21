'use client'

import { Center, Container, Group, Loader, SegmentedControl, Stack, Text, TextInput, Title } from '@mantine/core'
import { IconSearch } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { doQueryGet } from '@/helpers/apiClient'
import { QueryKeys } from '@/helpers/QueryKeys'
import type { GetAdminAlbumsDto } from '@/interfaces/api/admin/GetAdminAlbumsDto'
import LibraryStatusCard from '@/components/admin/LibraryStatusCard'
import AdminAlbumsTable from '@/components/admin/AdminAlbumsTable'

type VisibilityFilter = 'all' | 'public' | 'private'

export default function AdminComponent() {
  const [searchQuery, setSearchQuery] = useState('')
  const [visibilityFilter, setVisibilityFilter] = useState<VisibilityFilter>('all')

  const { data, isLoading } = useQuery({
    queryKey: [QueryKeys.AdminAlbums],
    queryFn: async () => await doQueryGet<GetAdminAlbumsDto>('/api/Admin/GetAdminAlbums')
  })

  const albums = data?.albums ?? []

  const filteredAlbums = albums.filter(album => {
    const visibilityMatch = visibilityFilter === 'all' ||
      (visibilityFilter === 'public' && album.isPublic) ||
      (visibilityFilter === 'private' && !album.isPublic)
    const searchMatch = searchQuery === '' ||
      album.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      album.folderName.toLowerCase().includes(searchQuery.toLowerCase())
    return visibilityMatch && searchMatch
  })

  if (isLoading) {
    return (
      <Center h={400}>
        <Loader size="lg" />
      </Center>
    )
  }

  return (
    <Container size="xl" px={{ base: 'xs', sm: 'md' }}>
      <Stack gap="xl">
        <div>
          <Title order={1} mb="xs">
            Admin
          </Title>
          <Text c="dimmed" size="sm">
            New albums are private until you publish them
          </Text>
        </div>

        <LibraryStatusCard />

        <Group justify="space-between" wrap="wrap" gap="md">
          <TextInput
            placeholder="Search albums..."
            leftSection={<IconSearch size="1rem" />}
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.currentTarget.value)}
            style={{ flex: 1, minWidth: 220 }}
          />
          <SegmentedControl
            value={visibilityFilter}
            onChange={(value) => setVisibilityFilter(value as VisibilityFilter)}
            data={[
              { label: 'All', value: 'all' },
              { label: 'Public', value: 'public' },
              { label: 'Private', value: 'private' }
            ]}
          />
        </Group>

        <AdminAlbumsTable albums={filteredAlbums} />
      </Stack>
    </Container>
  )
}
