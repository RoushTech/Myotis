<template>
    <div class="page-head">
        <div>
            <h1 class="page-title">Access tokens</h1>
            <p class="page-subtitle">
                For test suites and agents. A token reads the same mailboxes you can, through
                <span class="text-mono">/api/v1</span> and <span class="text-mono">/mcp</span>.
            </p>
        </div>
        <v-btn color="primary" prepend-icon="mdi-plus" @click="startCreate">New token</v-btn>
    </div>
    <div class="stack">
        <ErrorAlert
            v-if="error"
            what="access tokens"
            :message="errorMessage(error)"
            @retry="refetch()"
        />
        <v-skeleton-loader v-else-if="isLoading" type="table" />
        <v-card v-else border flat>
            <v-table v-if="tokens?.length">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Token</th>
                        <th>Created</th>
                        <th>Last used</th>
                        <th />
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="token in tokens" :key="token.id">
                        <td class="font-weight-medium">{{ token.name }}</td>
                        <td class="text-mono text-medium-emphasis">{{ token.prefix }}…</td>
                        <td class="text-medium-emphasis">{{ shortDate(token.createdAt) }}</td>
                        <td class="text-medium-emphasis">{{ lastUsed(token.lastUsedAt) }}</td>
                        <td class="text-end">
                            <v-btn
                                variant="text"
                                color="error"
                                size="small"
                                @click="revoking = token"
                            >
                                Revoke
                            </v-btn>
                        </td>
                    </tr>
                </tbody>
            </v-table>
            <div v-else class="empty-state">
                <v-icon icon="mdi-key-outline" size="44" />
                <h2 class="empty-state__title">No tokens</h2>
                <p class="empty-state__text">
                    Create one to read mail from a test suite or connect an agent over MCP.
                </p>
            </div>
        </v-card>
        <v-card border flat>
            <v-card-item><v-card-title>Using a token</v-card-title></v-card-item>
            <v-card-text class="stack">
                <pre class="code">
curl -H "Authorization: Bearer myotis_pat_..." \
  {{ origin }}/api/v1/mailboxes/{mailbox}/messages</pre>
                <pre class="code">
claude mcp add --transport http myotis {{ origin }}/mcp \
  --header "Authorization: Bearer myotis_pat_..."</pre>
            </v-card-text>
        </v-card>
    </div>
    <v-dialog v-model="creating" max-width="520">
        <v-card v-if="!created" title="New access token">
            <v-form @submit.prevent="create">
                <v-card-text>
                    <v-text-field
                        v-model="name"
                        label="Name"
                        placeholder="Playwright suite"
                        hint="So you can tell your tokens apart later."
                        persistent-hint
                        autofocus
                        :error-messages="createError"
                    />
                </v-card-text>
                <v-card-actions>
                    <v-spacer />
                    <v-btn variant="text" @click="creating = false">Cancel</v-btn>
                    <v-btn
                        type="submit"
                        color="primary"
                        variant="flat"
                        :loading="busy"
                        :disabled="!name.trim()"
                    >
                        Create token
                    </v-btn>
                </v-card-actions>
            </v-form>
        </v-card>
        <v-card v-else title="Token created">
            <v-card-text>
                <v-alert type="warning" variant="tonal" class="mb-4">
                    <strong>Copy the token now.</strong> You won't be able to see it again.
                </v-alert>
                <v-text-field :model-value="created" readonly hide-details class="mono-input">
                    <template #append-inner>
                        <CopyButton :text="created" label="token" />
                    </template>
                </v-text-field>
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn color="primary" variant="flat" @click="creating = false">Done</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
    <ConfirmDialog
        :open="revoking !== null"
        :title="`Revoke ${revoking?.name ?? ''}?`"
        body="Anything using it stops working straight away."
        action="Revoke"
        :busy="busy"
        @cancel="revoking = null"
        @confirm="revoke"
    />
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useQuery } from "@pinia/colada";
import { tokenApi, type Token } from "@/api/tokens";
import { errorMessage } from "@/api/errors";
import { lastUsed, shortDate } from "@/lib/dates";
import { tokensQuery, useInvalidateTokens } from "@/queries/tokens";
import ConfirmDialog from "@c/ConfirmDialog.vue";
import CopyButton from "@c/CopyButton.vue";
import ErrorAlert from "@c/ErrorAlert.vue";

const invalidate = useInvalidateTokens();
const { data: tokens, error, isLoading, refetch } = useQuery(tokensQuery);
const origin = window.location.origin;
const creating = ref(false);
const name = ref("");
const created = ref<string | null>(null);
const createError = ref<string | null>(null);
const busy = ref(false);
const revoking = ref<Token | null>(null);

function startCreate() {
    name.value = "";
    created.value = null;
    createError.value = null;
    creating.value = true;
}

async function create() {
    busy.value = true;
    createError.value = null;
    try {
        created.value = (await tokenApi.create(name.value)).plainText;
        await invalidate();
    } catch (e) {
        createError.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}

async function revoke() {
    if (!revoking.value) {
        return;
    }
    busy.value = true;
    try {
        await tokenApi.revoke(revoking.value.id);
        await invalidate();
        revoking.value = null;
    } finally {
        busy.value = false;
    }
}
</script>
