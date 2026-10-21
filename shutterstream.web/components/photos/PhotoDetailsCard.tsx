'use client'

import { Card, Divider, Group, Stack, Text, ThemeIcon, Title } from '@mantine/core'
import { IconAperture, IconCalendar, IconCamera, IconLine } from '@tabler/icons-react'
import { formatDateTaken } from '@/helpers/dateHelper'
import { formatCameraName, formatExposureSettings } from '@/helpers/photoHelper'
import type { GetPhotoDto } from '@/interfaces/api/photos/GetPhotoDto'

interface PhotoDetailsCardProps {
  photo: GetPhotoDto
}

export default function PhotoDetailsCard({ photo }: PhotoDetailsCardProps) {
  const cameraName = formatCameraName(photo.cameraMake, photo.cameraModel)
  const exposure = formatExposureSettings(photo.focalLength, photo.aperture, photo.exposureTime, photo.iso)

  const rows = [
    { icon: IconCalendar, label: 'Taken', value: photo.dateTaken !== null ? formatDateTaken(photo.dateTaken) : null },
    { icon: IconCamera, label: 'Camera', value: cameraName },
    { icon: IconLine, label: 'Lens', value: photo.lensModel },
    { icon: IconAperture, label: 'Settings', value: exposure }
  ].filter(row => row.value !== null)

  return (
    <Card withBorder p="lg" radius="md" shadow="sm">
      <Title order={3} size="h5" mb="sm">Photo details</Title>
      <Divider mb="md" />

      {rows.length === 0 ? (
        <Text c="dimmed" size="sm">No camera details were recorded for this photo.</Text>
      ) : (
        <Stack gap="md">
          {rows.map(row => (
            <Group key={row.label} gap="sm" wrap="nowrap" align="flex-start">
              <ThemeIcon variant="light" color="gray" size="md" radius="md">
                <row.icon size="1rem" />
              </ThemeIcon>
              <div>
                <Text size="xs" c="dimmed">{row.label}</Text>
                <Text size="sm">{row.value}</Text>
              </div>
            </Group>
          ))}
        </Stack>
      )}

      <Divider my="md" />
      <Text size="xs" c="dimmed">{photo.width} × {photo.height}</Text>
    </Card>
  )
}
