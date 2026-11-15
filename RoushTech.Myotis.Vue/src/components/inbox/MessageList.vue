<template>
    <section class="inbox__list" aria-label="Messages">
        <div class="inbox__head">
            <div class="inbox__title">
                <MailboxSwitcher :current="mailbox" :mailboxes="mailboxes" />
                <span v-if="!error" class="live-dot">Live</span>
            </div>
            <v-text-field
                v-model="search"
                placeholder="Search subject, from or to"
                prepend-inner-icon="mdi-magnify"
                density="compact"
                hide-details
                clearable
            />
            <div class="inbox__meta">
                <span v-if="!error">
                    {{ mailbox.messageCount }}
                    {{ mailbox.messageCount === 1 ? "message" : "messages" }}
                </span>
                <div v-if="mailbox.messageCount && !error" class="inbox__actions">
                    <v-btn
                        v-if="mailbox.unreadCount"
                        variant="text"
                        color="primary"
                        size="small"
                        prepend-icon="mdi-check-all"
                        :loading="markingRead"
                        @click="markAllRead"
                    >
                        Mark all read
                    </v-btn>
                    <v-btn
                        variant="text"
                        color="error"
                        size="small"
                        prepend-icon="mdi-delete-sweep-outline"
                        @click="confirming = true"
                    >
                        Empty mailbox
                    </v-btn>
                </div>
            </div>
            <div v-if="confirming" class="confirm-bar">
                <span>
                    Delete all {{ mailbox.messageCount }} messages in {{ mailbox.name }}? This can't
                    be undone.
                </span>
                <div>
                    <v-btn variant="outlined" size="small" @click="confirming = false"
                        >Cancel</v-btn
                    >
                    <v-btn
                        color="error"
                        variant="flat"
                        size="small"
                        :loading="emptying"
                        @click="empty"
                    >
                        Empty mailbox
                    </v-btn>
                </div>
            </div>
        </div>
        <div class="inbox__messages">
            <ErrorAlert
                v-if="error"
                class="ma-4"
                what="messages"
                :message="errorMessage(error)"
                @retry="refetch()"
            />
            <v-skeleton-loader v-else-if="isLoading && !messages" type="list-item-two-line@4" />
            <div v-else-if="!messages?.length && !query" class="empty-state">
                <v-icon icon="mdi-inbox-outline" size="44" />
                <h2 class="empty-state__title">No mail yet</h2>
                <p class="empty-state__text">
                    Send to this mailbox with one of its SMTP credentials. New mail shows up here
                    straight away.
                </p>
            </div>
            <div v-else-if="!messages?.length" class="empty-state">
                <v-icon icon="mdi-magnify-close" size="44" />
                <h2 class="empty-state__title">No matches</h2>
                <p class="empty-state__text">
                    Nothing in {{ mailbox.name }} has "{{ query }}" in the subject, from or to.
                </p>
            </div>
            <router-link
                v-for="message in messages"
                v-else
                :key="message.id"
                :to="{ name: 'Inbox', params: { mailboxId: mailbox.id, messageId: message.id } }"
                class="message-row"
                :class="{
                    'message-row--unread': !message.isRead,
                    'message-row--current': message.id === currentId,
                }"
            >
                <span class="message-row__to">{{ message.to || "(no recipient)" }}</span>
                <span class="message-row__time">{{ listTime(message.receivedAt) }}</span>
                <span class="message-row__subject">
                    <v-icon v-if="message.attachmentCount" icon="mdi-paperclip" size="16" />
                    <span>{{ message.subject || "(no subject)" }}</span>
                </span>
            </router-link>
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { useQuery } from "@pinia/colada";
import type { Mailbox } from "@/api/mailboxes";
import { messageApi, type MessageId } from "@/api/messages";
import { errorMessage } from "@/api/errors";
import { listTime } from "@/lib/dates";
import { messagesQuery, useInvalidateMessages } from "@/queries/messages";
import ErrorAlert from "@c/ErrorAlert.vue";
import MailboxSwitcher from "@c/inbox/MailboxSwitcher.vue";

const props = defineProps<{
    mailbox: Mailbox;
    mailboxes: Mailbox[];
    currentId: MessageId | undefined;
}>();
const router = useRouter();
const invalidate = useInvalidateMessages();
const confirming = ref(false);
const emptying = ref(false);
const markingRead = ref(false);
const search = ref<string | null>("");
const query = computed(() => (search.value ?? "").trim());
const {
    data: messages,
    error,
    isLoading,
    refetch,
} = useQuery(() => messagesQuery(props.mailbox.id, query.value));

async function markAllRead() {
    markingRead.value = true;
    try {
        await messageApi.markAllRead(props.mailbox.id);
        await invalidate(props.mailbox.id);
    } finally {
        markingRead.value = false;
    }
}

async function empty() {
    emptying.value = true;
    try {
        await messageApi.empty(props.mailbox.id);
        confirming.value = false;
        await router.push({ name: "Inbox", params: { mailboxId: props.mailbox.id } });
        await invalidate(props.mailbox.id);
    } finally {
        emptying.value = false;
    }
}
</script>
