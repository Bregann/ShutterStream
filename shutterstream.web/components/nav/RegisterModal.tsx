import { Modal, PasswordInput, TextInput, Text, Group, Button, Alert } from "@mantine/core";
import { useState } from "react";
import { useForm } from "@mantine/form";
import { signIn } from "next-auth/react";
import { ModalProps } from "@/types/ModalProps";
import { DoPost } from "@/helpers/webFetchHelper";
import { IconAlertCircle, IconLock } from "@tabler/icons-react";
import { RegiserUserData } from "@/types/Registration/RegisterUserData";
import { CreateSuccessToastNotification } from "@/helpers/toastHelper";

interface FormValues {
    username: string;
    password: string;
    email: string;
}

const RegisterModal = (props: ModalProps) => {
    const form = useForm<FormValues>({
        initialValues: {
            username: '',
            password: '',
            email: ''
        },
        validate: {
            email: (value) => (/^\S+@\S+$/.test(value) ? null : 'Invalid email')
        }
    });

    const RegisterUser = async (values: FormValues) => {
        setErrorMessage(null);

        let spChars = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]+/;

        if (values.password.length < 6 || !spChars.test(values.password)) {
            setErrorMessage("Password must be at least 6 characters and contain a special character!");
            return;
        }

        setRegisterButtonLoading(true);
        const res = await DoPost('/api/auth/RegisterUser', values);

        if (!res.ok) {
            setErrorMessage('There has been an unexpected error. Please try again shortly');
            setRegisterButtonLoading(false);
            return;
        }

        const data: RegiserUserData = await res.json();

        console.log(data);

        //Check if there's any error
        if (!data.success) {
            setErrorMessage(data.reason);
            setRegisterButtonLoading(false);
            return;
        }

        //If the registration was successful then login the user
        if (data.success && !data.reason) {
            //Send the signin request
            const res = await signIn('credentials', {
                username: values.username,
                password: values.password,
                redirect: false
            });

            if (res?.ok) {
                //Close the menu
                setRegisterButtonLoading(false);
                props.setOpened(false);

                CreateSuccessToastNotification("Account created and successfully logged in!");
            }
            else {
                setRegisterButtonLoading(false);
                props.setOpened(false);

                CreateSuccessToastNotification("Account created! You can now login");
            }
        }

        setRegisterButtonLoading(false);
    }

    const [registerButtonLoading, setRegisterButtonLoading] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    return (
        <Modal
            opened={props.openedState}
            onClose={() => props.setOpened(false)}
            title="Register">
            {errorMessage &&
                <Alert
                    icon={<IconAlertCircle size={16}/>}
                    title="Error registering user"
                    color="red"
                    sx={{ marginTop: 20 }}>
                    {errorMessage}
                </Alert>}

            <form onSubmit={form.onSubmit((values) => RegisterUser(values))}>
                <TextInput
                    label="ShutterStream Username"
                    placeholder="ShutterStream Username"
                    withAsterisk
                    required
                    {...form.getInputProps('username')}
                />

                <TextInput
                    label="Email address"
                    placeholder="Email address"
                    withAsterisk
                    required
                    sx={{ marginTop: 15, marginBottom: 10 }}
                    {...form.getInputProps('email')}
                />
                <Text fz="sm">This is required incase you forget your password.</Text>

                <PasswordInput
                    placeholder="Password"
                    label="Password"
                    description="Password must contain at least 6 characters and 1 special character"
                    required
                    withAsterisk
                    icon={<IconLock size={16} />}
                    sx={{ marginTop: 15, marginBottom: 10 }}
                    {...form.getInputProps('password')}
                />

                <Group position="right" sx={{ marginTop: 20 }}>
                    <Button
                        type="submit"
                        variant="gradient"
                        gradient={{ from: 'teal', to: 'lime', deg: 105 }}
                        loading={registerButtonLoading}>
                        Register
                    </Button>
                </Group>
            </form>
        </Modal>
    );
}

export default RegisterModal;