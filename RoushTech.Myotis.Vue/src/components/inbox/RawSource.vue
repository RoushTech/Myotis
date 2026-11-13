<template>
    <ErrorAlert
        v-if="error"
        what="the raw source"
        :message="errorMessage(error)"
        @retry="refetch()"
    />
    <v-skeleton-loader v-else-if="raw === undefined" type="paragraph" />
    <pre v-else class="code">{{ raw }}</pre>
</template>

<script setup lang="ts">
import { useQuery } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import type { MessageId } from "@/api/messages";
import { errorMessage } from "@/api/errors";
import { rawQuery } from "@/queries/messages";
import ErrorAlert from "@c/ErrorAlert.vue";

const props = defineProps<{ mailboxId: MailboxId; messageId: MessageId }>();
const { data: raw, error, refetch } = useQuery(() => rawQuery(props.mailboxId, props.messageId));
</script>
