export function shortDate(iso: string): string {
    return new Date(iso).toLocaleDateString(undefined, {
        month: "short",
        day: "numeric",
        year: "numeric",
    });
}

export function lastUsed(iso: string | null): string {
    return iso ? new Date(iso).toLocaleString() : "Never";
}

export function lastSignIn(iso: string | null): string {
    return iso ? new Date(iso).toLocaleString() : "Never";
}
