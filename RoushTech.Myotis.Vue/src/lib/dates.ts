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

export function listTime(iso: string): string {
    const date = new Date(iso);
    const now = new Date();
    if (date.toDateString() === now.toDateString()) {
        return date.toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" });
    }
    const yesterday = new Date(now);
    yesterday.setDate(now.getDate() - 1);
    if (date.toDateString() === yesterday.toDateString()) {
        return "Yesterday";
    }
    return date.toLocaleDateString(undefined, { month: "short", day: "numeric" });
}

export function fullDate(iso: string): string {
    return new Date(iso).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
}
