<template>
    <v-card border flat>
        <v-card-item>
            <v-card-title>SMTP credentials</v-card-title>
            <v-card-subtitle class="card-subtitle">
                Apps sign in with one of these, and their mail lands in this mailbox. Passwords are
                shown once, when created.
            </v-card-subtitle>
            <template #append>
                <AddCredentialDialog :mailbox="mailbox" />
            </template>
        </v-card-item>
        <ErrorAlert
            v-if="error"
            class="ma-4"
            what="credentials"
            :message="errorMessage(error)"
            @retry="refetch()"
        />
        <v-skeleton-loader v-else-if="isLoading" type="table-row@2" />
        <v-table v-else>
            <thead>
                <tr>
                    <th>Username</th>
                    <th>Created</th>
                    <th>Last used</th>
                    <th />
                </tr>
            </thead>
            <tbody>
                <tr v-if="!credentials?.length">
                    <td colspan="4" class="text-medium-emphasis">
                        No credentials. Nothing can send to this mailbox until you add one.
                    </td>
                </tr>
                <tr v-for="credential in credentials" :key="credential.id">
                    <td class="text-mono">{{ credential.username }}</td>
                    <td class="text-medium-emphasis">{{ shortDate(credential.createdAt) }}</td>
                    <td class="text-medium-emphasis">{{ lastUsed(credential.lastUsedAt) }}</td>
                    <td class="text-end">
                        <v-btn
                            variant="text"
                            color="error"
                            size="small"
                            @click="revoking = credential"
                        >
                            Revoke
                        </v-btn>
                    </td>
                </tr>
            </tbody>
        </v-table>
        <ConfirmDialog
            :open="revoking !== null"
            :title="`Revoke ${revoking?.username ?? ''}?`"
            body="Apps using it can no longer send to this mailbox. Mail already received stays."
            action="Revoke"
            :busy="busy"
            :error="revokeError"
            @cancel="revoking = null"
            @confirm="revoke"
        />
    </v-card>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useQuery } from "@pinia/colada";
import { mailboxApi, type Credential, type Mailbox } from "@/api/mailboxes";
import { errorMessage } from "@/api/errors";
import { lastUsed, shortDate } from "@/lib/dates";
import { credentialsQuery, useInvalidateMailboxes } from "@/queries/mailboxes";
import AddCredentialDialog from "@c/admin/AddCredentialDialog.vue";
import ConfirmDialog from "@c/ConfirmDialog.vue";
import ErrorAlert from "@c/ErrorAlert.vue";

const props = defineProps<{ mailbox: Mailbox }>();
const invalidate = useInvalidateMailboxes();
const {
    data: credentials,
    error,
    isLoading,
    refetch,
} = useQuery(() => credentialsQuery(props.mailbox.id));
const revoking = ref<Credential | null>(null);
const busy = ref(false);
const revokeError = ref<string | null>(null);

async function revoke() {
    if (!revoking.value) {
        return;
    }
    busy.value = true;
    revokeError.value = null;
    try {
        await mailboxApi.revokeCredential(props.mailbox.id, revoking.value.id);
        await invalidate();
        revoking.value = null;
    } catch (e) {
        revokeError.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}
</script>
