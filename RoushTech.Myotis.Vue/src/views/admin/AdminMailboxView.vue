<template>
    <v-container class="page">
        <v-btn
            variant="text"
            size="small"
            prepend-icon="mdi-arrow-left"
            class="back-link"
            :to="{ name: 'AdminMailboxes' }"
        >
            Mailboxes
        </v-btn>
        <ErrorAlert
            v-if="error"
            what="this mailbox"
            :message="errorMessage(error)"
            @retry="refetch()"
        />
        <v-skeleton-loader v-else-if="!mailbox" type="heading, card" />
        <template v-else>
            <div class="page-head">
                <div>
                    <h1 class="page-title">{{ mailbox.name }}</h1>
                    <p class="page-subtitle">Created {{ shortDate(mailbox.createdAt) }}</p>
                </div>
            </div>
            <div class="stack">
                <CredentialsCard :mailbox="mailbox" />
                <PeopleCard :mailbox="mailbox" />
                <DeleteMailboxCard :mailbox="mailbox" />
            </div>
        </template>
    </v-container>
</template>

<script setup lang="ts">
import { useQuery } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import { errorMessage } from "@/api/errors";
import { shortDate } from "@/lib/dates";
import { mailboxQuery } from "@/queries/mailboxes";
import CredentialsCard from "@c/admin/CredentialsCard.vue";
import DeleteMailboxCard from "@c/admin/DeleteMailboxCard.vue";
import PeopleCard from "@c/admin/PeopleCard.vue";
import ErrorAlert from "@c/ErrorAlert.vue";

const props = defineProps<{ mailboxId: MailboxId }>();
const { data: mailbox, error, refetch } = useQuery(() => mailboxQuery(props.mailboxId));
</script>
