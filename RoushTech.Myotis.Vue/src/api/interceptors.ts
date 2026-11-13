import axios from "axios";

export function onUnauthorized(handler: () => void): void {
    axios.interceptors.response.use(undefined, (error: unknown) => {
        if (axios.isAxiosError(error) && error.response?.status === 401) {
            handler();
        }
        return Promise.reject(error instanceof Error ? error : new Error(String(error)));
    });
}
