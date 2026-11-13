<template>
    <v-container v-if="mailboxesError" class="page">
        <ErrorAlert what="mailboxes" :message="errorMessage(mailboxesError)" @retry="refetch()" />
    </v-container>
    <v-container v-else-if="mailboxes && !mailbox" class="page">
        <v-card border flat>
            <div class="empty-state">
                <v-icon icon="mdi-inbox-remove-outline" size="44" />
                <h2 class="empty-state__title">Mailbox not found</h2>
                <p class="empty-state__text">
                    It may have been deleted, or you no longer have access to it.
                </p>
                <v-btn color="primary" :to="{ name: 'Mailboxes' }" class="mt-2">
                    All mailboxes
                </v-btn>
            </div>
        </v-card>
    </v-container>
    <div v-else-if="mailbox" class="inbox" :class="{ 'inbox--reading': messageId }">
        <MessageList :mailbox="mailbox" :mailboxes="mailboxes ?? []" :current-id="messageId" />
        <MessageViewer v-if="messageId" :mailbox-id="mailbox.id" :message-id="messageId" />
        <div v-else class="viewer viewer--empty">
            <div class="empty-state">
                <v-icon icon="mdi-email-open-outline" size="44" />
                <p class="empty-state__text">
                    {{
                        mailbox.messageCount ? "Pick a message to read it." : "Nothing to read yet."
                    }}
                </p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, watch } from "vue";
import { useQuery } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import type { MessageId } from "@/api/messages";
import { errorMessage } from "@/api/errors";
import { rememberMailbox } from "@/lib/recent";
import { mailboxesQuery } from "@/queries/mailboxes";
import ErrorAlert from "@c/ErrorAlert.vue";
import MessageList from "@c/inbox/MessageList.vue";
import MessageViewer from "@c/inbox/MessageViewer.vue";

const props = defineProps<{ mailboxId: MailboxId; messageId?: MessageId | undefined }>();
const { data: mailboxes, error: mailboxesError, refetch } = useQuery(mailboxesQuery);
const mailbox = computed(() => mailboxes.value?.find((m) => m.id === props.mailboxId));

watch(
    mailbox,
    (found) => {
        if (found) {
            rememberMailbox(found.id);
        }
    },
    { immediate: true }
);
</script>
