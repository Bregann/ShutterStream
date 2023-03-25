import { CreateInfoToastNotification, CreateWarningToastNotification } from '@/helpers/toastHelper';
import { DoFormDataPost, DoPost } from '@/helpers/webFetchHelper';
import { CreateNewAlbumReq } from '@/types/Albums/CreateNewAlbumReq';
import { Button, Container, Grid, Group, Progress, rem, Text, TextInput, useMantineTheme } from '@mantine/core';
import { Dropzone, DropzoneProps, IMAGE_MIME_TYPE, FileWithPath } from '@mantine/dropzone';
import { IconUpload, IconX, IconPhoto } from '@tabler/icons-react';
import { useSession } from 'next-auth/react';
import { useRef, useState } from 'react';

const NewAlbum = () => {
    const theme = useMantineTheme();
    const [filesUploaded, setFilesUploaded] = useState<FileWithPath[] | null>(null);
    const [amountOfFilesUploaded, setAmountOfFilesUploaded] = useState(0);
    const [percentageUploaded, setPercentageUploaded] = useState(0);
    const [uploadInProgress, setUploadInProgress] = useState(false);
    const albumNameRef = useRef<HTMLInputElement>(null);
    const locationNameRef = useRef<HTMLInputElement>(null);
    const { data: session } = useSession();

    const SetFilesToUpload = (files: FileWithPath[]) => {
        setFilesUploaded(files);

        const formData = new FormData();
        formData.append('file', files[0]);

        //const res = DoFormDataPost('/api/Albums/UploadNewAlbum', formData);
    }

    const UploadFiles = async () => {
        if(uploadInProgress){
            CreateWarningToastNotification("Your files are already uploading!");
            return;
        }

        if(filesUploaded === null){
            CreateWarningToastNotification("You need to upload some files first!");
            return;
        }

        if(albumNameRef.current?.value === "" || albumNameRef.current?.value === undefined){
            CreateWarningToastNotification("You need to enter an album name!");
            return;
        }

        setUploadInProgress(true);
        
        const albumData: CreateNewAlbumReq = {
            albumName: albumNameRef.current?.value,
            location: locationNameRef.current?.value ? locationNameRef.current?.value : null
        };

        //Create the album
        const res = await DoPost('/api/albums/CreateNewAlbum', albumData);

        if(!res.ok){
            CreateWarningToastNotification("Error creating new album " + res.status);
            setUploadInProgress(false);
            return;
        }
        else{
            CreateInfoToastNotification("Album created, starting image upload...");
        }

    }

    if(session){
        return (
            <>
                <Container size={'xl'}>
                    <Text size={60} weight={500} align='center'>Upload New Album</Text>
    
                    <Dropzone
                        mt={20}
                        onDrop={(files) => SetFilesToUpload(files)}
                        disabled={uploadInProgress}
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
                            <TextInput ref={albumNameRef} label="Album name" maxLength={50}/>
                        </Grid.Col>
                        <Grid.Col span={6}>
                            <TextInput ref={locationNameRef} label="Location" maxLength={100}/>
                        </Grid.Col>
                        <Grid.Col span={1}>
                            <Button onClick={() => UploadFiles()} disabled={uploadInProgress} loading={uploadInProgress}>Submit</Button>
                        </Grid.Col>
                    </Grid>
                </Container>
    
            </>
        );
    }
    else{
        return(
        <>
            <Text>You need to be logged in to view this page!</Text>
        </>);
    }

}

export default NewAlbum;