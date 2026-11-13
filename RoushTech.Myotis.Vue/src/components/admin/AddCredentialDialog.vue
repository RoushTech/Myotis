<template>
    <v-dialog v-model="open" max-width="520">
        <template #activator="{ props: activator }">
            <v-btn v-bind="activator" color="primary" size="small" prepend-icon="mdi-plus">
                Add credential
            </v-btn>
        </template>
        <v-card v-if="!created" title="Add credential">
            <v-form @submit.prevent="create">
                <v-card-text>
                    <p class="text-medium-emphasis mb-4">
                        For {{ mailbox.name }}. Myotis generates the password.
                    </p>
                    <v-text-field
                        v-model="username"
                        label="Username"
                        hint="Must be unique across Myotis."
                        persistent-hint
                        autofocus
                        :error-messages="error"
                    />
                </v-card-text>
                <v-card-actions>
                    <v-spacer />
                    <v-btn variant="text" @click="open = false">Cancel</v-btn>
                    <v-btn
                        type="submit"
                        color="primary"
                        variant="flat"
                        :loading="busy"
                        :disabled="!username.trim()"
                    >
                        Create credential
                    </v-btn>
                </v-card-actions>
            </v-form>
        </v-card>
        <v-card v-else title="Credential created">
            <v-card-text>
                <v-alert type="warning" variant="tonal" class="mb-4">
                    <strong>Copy the password now.</strong> You won't be able to see it again. If
                    it's lost, revoke this credential and add another.
                </v-alert>
                <dl class="key-values key-values--copyable">
                    <template v-if="connection">
                        <dt>Host</dt>
                        <dd>
                            <span class="text-mono">{{ connection.host }}</span>
                            <CopyButton :text="connection.host" label="host" />
                        </dd>
                        <dt>Port</dt>
                        <dd v-if="!connection.ports.length" class="text-medium-emphasis">
                            No SMTP listener is set up yet
                        </dd>
                        <dd v-else class="ports">
                            <span v-for="port in connection.ports" :key="port.port">
                                <span class="text-mono">{{ port.port }}</span>
                                <span class="text-medium-emphasis"> ({{ port.security }})</span>
                                <CopyButton
                                    :text="String(port.port)"
                                    :label="`port ${port.port}`"
                                />
                            </span>
                        </dd>
                    </template>
                    <dt>Username</dt>
                    <dd>
                        <span class="text-mono">{{ created.credential.username }}</span>
                        <CopyButton :text="created.credential.username" label="username" />
                    </dd>
                    <dt>Sign-in</dt>
                    <dd>PLAIN or LOGIN</dd>
                </dl>
                <v-text-field
                    :model-value="created.password"
                    label="Password"
                    readonly
                    class="mono-input mt-4"
                    hide-details
                >
                    <template #append-inner>
                        <CopyButton :text="created.password" label="password" />
                    </template>
                </v-text-field>
            </v-card-text>
            <v-card-actions>
                <CopyButton :text="allSettings" label="all settings" caption="Copy all" />
                <v-spacer />
                <v-btn color="primary" variant="flat" @click="open = false">Done</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useQuery } from "@pinia/colada";
import { mailboxApi, type CreatedCredential, type Mailbox } from "@/api/mailboxes";
import { errorMessage } from "@/api/errors";
import { useInvalidateMailboxes } from "@/queries/mailboxes";
import { smtpConnectionQuery } from "@/queries/smtp";
import CopyButton from "@c/CopyButton.vue";

const props = defineProps<{ mailbox: Mailbox }>();
const invalidate = useInvalidateMailboxes();
const { data: connection } = useQuery(smtpConnectionQuery);
const open = ref(false);
const username = ref("");
const busy = ref(false);
const error = ref<string | null>(null);
const created = ref<CreatedCredential | null>(null);

const allSettings = computed(() => {
    if (!created.value) {
        return "";
    }
    const lines = [];
    if (connection.value) {
        lines.push(`Host: ${connection.value.host}`);
        for (const port of connection.value.ports) {
            lines.push(`Port: ${port.port} (${port.security})`);
        }
    }
    lines.push(`Username: ${created.value.credential.username}`);
    lines.push(`Password: ${created.value.password}`);
    return lines.join("\n");
});

watch(open, (isOpen) => {
    if (isOpen) {
        username.value = props.mailbox.name.toLowerCase().trim().replace(/\s+/g, "-");
        error.value = null;
        created.value = null;
    }
});

async function create() {
    busy.value = true;
    error.value = null;
    try {
        created.value = await mailboxApi.createCredential(props.mailbox.id, username.value);
        await invalidate();
    } catch (e) {
        error.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}
</script>
