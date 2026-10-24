'use client'

import { Alert, Button, Center, Collapse, Container, Group, Loader, Stack, Switch, Text, Title, UnstyledButton } from '@mantine/core'
import { useLocalStorage } from '@mantine/hooks'
import { IconArrowLeft, IconChevronDown, IconEyeOff, IconSettings } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import Link from 'next/link'
import { doQueryGet } from '@/helpers/apiClient'
import { QueryKeys } from '@/helpers/QueryKeys'
import { formatAlbumDate } from '@/helpers/dateHelper'
import { formatSubFolder, groupPhotosBySubFolder } from '@/helpers/photoHelper'
import { useAuth } from '@/context/authContext'
import type { GetAlbumDto } from '@/interfaces/api/albums/GetAlbumDto'
import JustifiedPhotoGrid from '@/components/photos/JustifiedPhotoGrid'

interface AlbumComponentProps {
  albumId: number
}

export default function AlbumComponent({ albumId }: AlbumComponentProps) {
  const auth = useAuth()

  const { data: album, isLoading, isError } = useQuery({
    queryKey: [QueryKeys.Album, albumId.toString()],
    queryFn: async () => await doQueryGet<GetAlbumDto>(`/api/Albums/GetAlbum?albumId=${albumId}`)
  })

  // Remembered per browser across every album, so a visitor who turns titles off keeps them off
  const [showTitles, setShowTitles] = useLocalStorage({ key: 'showPhotoTitles', defaultValue: true })
  const hasTitles = album?.photos.some(p => p.title !== null) ?? false

  // Every section starts open, this tracks the ones that have been closed
  const [collapsedSections, setCollapsedSections] = useState<string[]>([])

  const sections = groupPhotosBySubFolder(album?.photos ?? [])
  const subFolders = sections.map(s => s.subFolder).filter(s => s !== '')
  const allCollapsed = subFolders.every(s => collapsedSections.includes(s))

  const toggleSection = (subFolder: string) => {
    setCollapsedSections(current => current.includes(subFolder)
      ? current.filter(s => s !== subFolder)
      : [...current, subFolder])
  }

  if (isLoading) {
    return (
      <Center h={400}>
        <Loader size="lg" />
      </Center>
    )
  }

  if (isError || album === undefined) {
    return (
      <Center h={400}>
        <Text c="dimmed">This album could not be loaded.</Text>
      </Center>
    )
  }

  return (
    <Container size="xl" px={{ base: 'xs', sm: 'md' }}>
      <Stack gap="lg">
        <Group justify="space-between" align="flex-start">
          <div>
            <Button
              component={Link}
              href="/"
              variant="subtle"
              color="gray"
              size="compact-sm"
              leftSection={<IconArrowLeft size="1rem" />}
              mb="xs"
            >
              All albums
            </Button>
            <Title order={1} mb="xs">
              {album.title}
            </Title>
            <Text c="dimmed" size="sm">
              {album.photos.length} {album.photos.length === 1 ? 'photo' : 'photos'}
              {album.albumDate !== null && ` · ${formatAlbumDate(album.albumDate)}`}
            </Text>
          </div>

          <Group gap="md">
            {hasTitles && (
              <Switch
                label="Show titles"
                checked={showTitles}
                onChange={(event) => setShowTitles(event.currentTarget.checked)}
              />
            )}
            {auth.isAuthenticated && (
              <Button
                component={Link}
                href={`/admin/albums/${album.albumId}`}
                variant="light"
                leftSection={<IconSettings size="1rem" />}
              >
                Manage album
              </Button>
            )}
          </Group>
        </Group>

        {!album.isPublic && (
          <Alert color="yellow" variant="light" icon={<IconEyeOff size="1rem" />}>
            This album is private. Only you can see it until it is published.
          </Alert>
        )}

        {album.description !== '' && (
          <Text style={{ whiteSpace: 'pre-line' }} maw={800}>
            {album.description}
          </Text>
        )}

        {album.photos.length === 0 ? (
          <Center py={80}>
            <Text c="dimmed">There are no photos in this album yet.</Text>
          </Center>
        ) : (
          <Stack gap="xl">
            {subFolders.length > 1 && (
              <Group justify="flex-end" mb={-16}>
                <Button
                  variant="subtle"
                  color="gray"
                  size="compact-sm"
                  onClick={() => setCollapsedSections(allCollapsed ? [] : subFolders)}
                >
                  {allCollapsed ? 'Expand all' : 'Collapse all'}
                </Button>
              </Group>
            )}

            {sections.map((section, index) => {
              const isExpanded = !collapsedSections.includes(section.subFolder)

              return (
                <section key={section.subFolder}>
                  {section.subFolder !== '' && (
                    <UnstyledButton
                      onClick={() => toggleSection(section.subFolder)}
                      aria-expanded={isExpanded}
                      w="100%"
                      mb="sm"
                    >
                      <Group justify="space-between" align="center" wrap="nowrap">
                        <Group gap="xs" wrap="nowrap">
                          <IconChevronDown
                            size="1.25rem"
                            style={{ flexShrink: 0, transition: 'transform 200ms ease', transform: isExpanded ? undefined : 'rotate(-90deg)' }}
                          />
                          <Title order={2} size="h3">{formatSubFolder(section.subFolder)}</Title>
                        </Group>
                        <Text c="dimmed" size="sm" style={{ flexShrink: 0 }}>
                          {section.photos.length} {section.photos.length === 1 ? 'photo' : 'photos'}
                        </Text>
                      </Group>
                    </UnstyledButton>
                  )}
                  {/* Collapsed sections stay mounted but hidden, so their photos don't load until opened */}
                  <Collapse expanded={isExpanded}>
                    <JustifiedPhotoGrid photos={section.photos} eagerCount={index === 0 ? 12 : 0} showTitles={showTitles} />
                  </Collapse>
                </section>
              )
            })}
          </Stack>
        )}
      </Stack>
    </Container>
  )
}
