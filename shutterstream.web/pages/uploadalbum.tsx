import { DoFormDataPost } from '@/helpers/webFetchHelper';
import { Button, Container, Grid, Group, Progress, rem, Text, TextInput, useMantineTheme } from '@mantine/core';
import { Dropzone, DropzoneProps, IMAGE_MIME_TYPE, FileWithPath } from '@mantine/dropzone';
import { IconUpload, IconX, IconPhoto } from '@tabler/icons-react';
import { useRef, useState } from 'react';
import { toast } from 'react-toastify';

const UploadAlbum = () => {
    const SetFilesToUpload = (files: FileWithPath[]) => {
        setFilesUploaded(files);

        const formData = new FormData();
        formData.append('file', files[0]);

        //const res = DoFormDataPost('/api/Albums/UploadNewAlbum', formData);
    }

    const UploadFiles = () => {
        if(uploadInProgress){
            toast.warn("Your files are already uploading!", {
                position: 'bottom-right',
                autoClose: 5000,
                theme: 'colored',
                style: {color: 'black'}
            })
            return;
        }

        if(filesUploaded === null){
            toast.warn("You need to upload some files first!", {
                position: 'bottom-right',
                autoClose: 5000,
                theme: 'colored',
                style: {color: 'black'}
            })
            return;
        }

        if(albumNameRef.current?.value === ""){
            toast.warn("You need to enter an album name!", {
                position: 'bottom-right',
                autoClose: 5000,
                theme: 'colored',
                style: {color: 'black'}
            })
            return;
        }

        setUploadInProgress(true);
        
        //Create the album 
    }

    const theme = useMantineTheme();
    const [filesUploaded, setFilesUploaded] = useState<FileWithPath[] | null>(null);
    const [amountOfFilesUploaded, setAmountOfFilesUploaded] = useState(0);
    const [percentageUploaded, setPercentageUploaded] = useState(0);
    const [uploadInProgress, setUploadInProgress] = useState(false);
    const albumNameRef = useRef<HTMLInputElement>(null);
    const locationNameRef = useRef<HTMLInputElement>(null);

    return (
        <>
            <Container size={'xl'}>
                <Text size={60} weight={500} align='center'>Upload New Album</Text>

                <Dropzone
                    mt={20}
                    onDrop={(files) => SetFilesToUpload(files)}
                >
                    <Group position="center" spacing="xl" style={{ minHeight: rem(220), pointerEvents: 'none' }}>
                        <Dropzone.Accept>
                            <IconUpload
                                size="3.2rem"
                                stroke={1.5}
                                color={theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 6]}
                            />
                        </Dropzone.Accept>
                        <Dropzone.Reject>
                            <IconX
                                size="3.2rem"
                                stroke={1.5}
                                color={theme.colors.red[theme.colorScheme === 'dark' ? 4 : 6]}
                            />
                        </Dropzone.Reject>
                        <Dropzone.Idle>
                            <IconPhoto size="3.2rem" stroke={1.5} />
                        </Dropzone.Idle>

                        <div>
                            <Text size="xl" inline>
                                Drag images here or click to select files
                            </Text>
                            <Text size="sm" color="dimmed" inline mt={7}>
                                Attach as many files as you like, each file should not exceed 5mb
                            </Text>
                        </div>
                    </Group>
                </Dropzone>
                <Grid pt={20}>
                    {filesUploaded &&
                        <>
                            <Grid.Col span={11}>
                                <Progress value={50} animate mt={9}/>
                            </Grid.Col>
                            <Grid.Col span={1}>
                                <Text><>{amountOfFilesUploaded}/{filesUploaded.length}</></Text>
                            </Grid.Col>
                        </>
                    }
                    <Grid.Col span={6}>
                        <TextInput ref={albumNameRef} label="Album name" />
                    </Grid.Col>
                    <Grid.Col span={6}>
                        <TextInput ref={locationNameRef} label="Location" />
                    </Grid.Col>
                    <Grid.Col span={1}>
                        <Button onClick={() => UploadFiles()} disabled={uploadInProgress} loading={uploadInProgress}>Submit</Button>
                    </Grid.Col>
                </Grid>
            </Container>

        </>
    );
}

export default UploadAlbum;