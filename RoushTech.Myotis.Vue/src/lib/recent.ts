import { ref } from "vue";
import type { MailboxId } from "@/api/mailboxes";

const KEY = "myotis.recentMailboxes";
const LIMIT = 5;

function load(): MailboxId[] {
    try {
        const stored: unknown = JSON.parse(localStorage.getItem(KEY) ?? "[]");
        return Array.isArray(stored)
            ? stored.filter((v): v is MailboxId => typeof v === "string")
            : [];
    } catch {
        return [];
    }
}

// Remembered per browser, so the nav stays short however many mailboxes there are.
export const recentMailboxes = ref<MailboxId[]>(load());

export function rememberMailbox(id: MailboxId) {
    recentMailboxes.value = [id, ...recentMailboxes.value.filter((m) => m !== id)].slice(0, LIMIT);
    try {
        localStorage.setItem(KEY, JSON.stringify(recentMailboxes.value));
    } catch {
        // Storage can be unavailable in private windows; the list then lasts for this visit only.
    }
}
