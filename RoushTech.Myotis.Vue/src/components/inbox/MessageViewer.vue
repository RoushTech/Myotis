<template>
    <section class="viewer" aria-label="Message">
        <ErrorAlert
            v-if="error"
            class="ma-4"
            what="this message"
            :message="errorMessage(error)"
            @retry="refetch()"
        />
        <v-skeleton-loader v-else-if="!detail" type="heading, paragraph, image" class="ma-4" />
        <template v-else>
            <div class="viewer__head">
                <v-btn
                    class="viewer__back"
                    variant="text"
                    size="small"
                    prepend-icon="mdi-arrow-left"
                    :to="{ name: 'Inbox', params: { mailboxId } }"
                >
                    Messages
                </v-btn>
                <div class="viewer__top">
                    <h2 class="viewer__subject">{{ detail.summary.subject || "(no subject)" }}</h2>
                    <div class="viewer__actions">
                        <v-btn
                            icon="mdi-download"
                            variant="text"
                            size="small"
                            title="Download .eml"
                            aria-label="Download .eml"
                            :href="messageApi.emlUrl(mailboxId, messageId)"
                        />
                        <v-btn
                            icon="mdi-delete-outline"
                            variant="text"
                            size="small"
                            title="Delete message"
                            aria-label="Delete message"
                            :loading="deleting"
                            @click="remove"
                        />
                    </div>
                </div>
                <dl class="key-values viewer__addresses">
                    <dt>From</dt>
                    <dd>{{ detail.summary.from }}</dd>
                    <dt>To</dt>
                    <dd>{{ detail.summary.to || "(none)" }}</dd>
                    <template v-if="detail.summary.cc">
                        <dt>Cc</dt>
                        <dd>{{ detail.summary.cc }}</dd>
                    </template>
                    <dt>Date</dt>
                    <dd>{{ fullDate(detail.sentAt ?? detail.summary.receivedAt) }}</dd>
                </dl>
                <div class="envelope">
                    <div class="envelope__title">
                        <v-icon icon="mdi-email-fast-outline" size="16" /> SMTP envelope
                    </div>
                    <dl class="key-values">
                        <dt>MAIL FROM</dt>
                        <dd class="text-mono">{{ detail.envelopeFrom || "(empty)" }}</dd>
                        <dt>RCPT TO</dt>
                        <dd class="chips">
                            <v-chip
                                v-for="recipient in detail.envelopeTo"
                                :key="recipient"
                                size="small"
                                :color="isBcc(recipient) ? 'warning' : 'default'"
                                :variant="isBcc(recipient) ? 'tonal' : 'flat'"
                            >
                                {{ recipient }}{{ isBcc(recipient) ? " · Bcc" : "" }}
                            </v-chip>
                        </dd>
                    </dl>
                </div>
                <v-tabs v-model="tab" density="compact">
                    <v-tab value="html" :disabled="!detail.hasHtml">HTML</v-tab>
                    <v-tab value="text">Text</v-tab>
                    <v-tab value="headers">Headers</v-tab>
                    <v-tab value="raw">Raw</v-tab>
                    <v-tab value="attachments">Attachments ({{ detail.attachments.length }})</v-tab>
                </v-tabs>
            </div>
            <div class="viewer__body">
                <template v-if="tab === 'html'">
                    <v-alert
                        v-if="detail.remoteImageCount && !remoteImages"
                        type="warning"
                        variant="tonal"
                        icon="mdi-image-off-outline"
                    >
                        <div class="error-alert">
                            <div>
                                <strong>Remote images blocked</strong>
                                <div>
                                    This message loads {{ detail.remoteImageCount }}
                                    {{ detail.remoteImageCount === 1 ? "image" : "images" }} from
                                    other servers, which tells the sender it was opened.
                                </div>
                            </div>
                            <v-btn variant="outlined" size="small" @click="remoteImages = true">
                                Load images
                            </v-btn>
                        </div>
                    </v-alert>
                    <v-alert
                        v-else-if="detail.remoteImageCount"
                        type="info"
                        variant="tonal"
                        icon="mdi-image-outline"
                    >
                        Remote images loaded for this message only.
                    </v-alert>
                    <iframe
                        :key="htmlUrl"
                        class="viewer__frame"
                        title="Message HTML"
                        sandbox="allow-popups allow-popups-to-escape-sandbox"
                        referrerpolicy="no-referrer"
                        :src="htmlUrl"
                    />
                </template>
                <pre v-else-if="tab === 'text'" class="code code--wrap">{{
                    detail.text ?? "This message has no plain text part."
                }}</pre>
                <v-card v-else-if="tab === 'headers'" border flat>
                    <v-table density="compact">
                        <tbody>
                            <tr v-for="(header, index) in detail.headers" :key="index">
                                <td class="text-mono text-medium-emphasis header-name">
                                    {{ header.key }}
                                </td>
                                <td class="text-mono header-value">{{ header.value }}</td>
                            </tr>
                        </tbody>
                    </v-table>
                </v-card>
                <RawSource
                    v-else-if="tab === 'raw'"
                    :mailbox-id="mailboxId"
                    :message-id="messageId"
                />
                <template v-else>
                    <div v-if="!detail.attachments.length" class="empty-state">
                        <v-icon icon="mdi-paperclip" size="44" />
                        <p class="empty-state__text">No attachments.</p>
                    </div>
                    <v-card
                        v-for="attachment in detail.attachments"
                        :key="attachment.index"
                        border
                        flat
                        class="attachment"
                    >
                        <v-icon icon="mdi-file-outline" />
                        <div class="attachment__name">
                            <div>{{ attachment.fileName }}</div>
                            <small class="text-medium-emphasis text-mono">
                                {{ attachment.contentType }} · {{ size(attachment.size) }}
                            </small>
                        </div>
                        <v-btn
                            variant="outlined"
                            size="small"
                            prepend-icon="mdi-download"
                            :href="messageApi.attachmentUrl(mailboxId, messageId, attachment.index)"
                        >
                            Download
                        </v-btn>
                    </v-card>
                </template>
            </div>
        </template>
    </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { useQuery } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import { messageApi, type MessageId } from "@/api/messages";
import { errorMessage } from "@/api/errors";
import { fullDate } from "@/lib/dates";
import { size } from "@/lib/sizes";
import { messageQuery, useInvalidateMessages } from "@/queries/messages";
import ErrorAlert from "@c/ErrorAlert.vue";
import RawSource from "@c/inbox/RawSource.vue";

const props = defineProps<{ mailboxId: MailboxId; messageId: MessageId }>();
const invalidate = useInvalidateMessages();
const {
    data: detail,
    error,
    refetch,
} = useQuery(() => messageQuery(props.mailboxId, props.messageId));
const router = useRouter();
const tab = ref("html");
const deleting = ref(false);
const remoteImages = ref(false);

const htmlUrl = computed(() =>
    messageApi.htmlUrl(props.mailboxId, props.messageId, remoteImages.value)
);

watch(
    () => props.messageId,
    () => {
        remoteImages.value = false;
    }
);

watch(
    detail,
    async (loaded) => {
        if (!loaded) {
            return;
        }
        if (!loaded.hasHtml && tab.value === "html") {
            tab.value = "text";
        }
        if (!loaded.summary.isRead) {
            await messageApi.markRead(props.mailboxId, props.messageId);
            await invalidate(props.mailboxId);
        }
    },
    { immediate: true }
);

async function remove() {
    deleting.value = true;
    try {
        await messageApi.remove(props.mailboxId, props.messageId);
        // Leave first: refreshing while this message is open would refetch it and fail.
        await router.push({ name: "Inbox", params: { mailboxId: props.mailboxId } });
        await invalidate(props.mailboxId);
    } finally {
        deleting.value = false;
    }
}

function headerAddresses(value: string): string[] {
    return value
        .split(",")
        .map((part) => /<([^>]+)>/.exec(part)?.[1] ?? part.trim())
        .map((address) => address.toLowerCase())
        .filter((address) => address.length > 0);
}

const visible = computed(
    () =>
        new Set([
            ...headerAddresses(detail.value?.summary.to ?? ""),
            ...headerAddresses(detail.value?.summary.cc ?? ""),
        ])
);

function isBcc(recipient: string): boolean {
    return !visible.value.has(recipient.toLowerCase());
}
</script>
