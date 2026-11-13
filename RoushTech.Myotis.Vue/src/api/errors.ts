import { isAxiosError } from "axios";

export function errorMessage(error: unknown): string {
    if (isAxiosError<{ title?: string }>(error)) {
        return error.response?.data.title ?? error.message;
    }
    return error instanceof Error ? error.message : String(error);
}

export function isUnauthorized(error: unknown): boolean {
    return isAxiosError(error) && error.response?.status === 401;
}
