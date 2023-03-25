import { toast } from "react-toastify"

export const CreateSuccessToastNotification = (message: string) => {
    toast.success(message, {
        position: 'bottom-right',
        autoClose: 5000,
        theme: 'colored',
    });
}

export const CreateWarningToastNotification = (message: string) => {
    toast.warn(message, {
        position: 'bottom-right',
        autoClose: 5000,
        theme: 'colored',
        style: {color: 'black'}
    });
}

export const CreateErrorToastNotification = (message: string) => {
    toast.error(message, {
        position: 'bottom-right',
        autoClose: 5000,
        theme: 'colored'
    });
}

export const CreateInfoToastNotification = (message: string) => {
    toast.info(message, {
        position: 'bottom-right',
        autoClose: 5000,
        theme: 'colored'
    });
}