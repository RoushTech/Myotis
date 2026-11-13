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
                <dl class="key-values">
                    <template v-if="connection">
                        <dt>Host</dt>
                        <dd class="text-mono">{{ connection.host }}</dd>
                        <dt>Port</dt>
                        <dd>
                            <span v-if="!connection.ports.length" class="text-medium-emphasis">
                                No SMTP listener is set up yet
                            </span>
                            <template v-for="(port, index) in connection.ports" :key="port.port">
                                <span v-if="index > 0"> or </span>
                                <span class="text-mono">{{ port.port }}</span> ({{ port.security }})
                            </template>
                        </dd>
                    </template>
                    <dt>Username</dt>
                    <dd class="text-mono">{{ created.credential.username }}</dd>
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
                        <v-btn
                            :icon="copied ? 'mdi-check' : 'mdi-content-copy'"
                            variant="text"
                            size="small"
                            aria-label="Copy password"
                            @click="copy(created.password)"
                        />
                    </template>
                </v-text-field>
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn color="primary" variant="flat" @click="open = false">Done</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import { useQuery } from "@pinia/colada";
import { mailboxApi, type CreatedCredential, type Mailbox } from "@/api/mailboxes";
import { errorMessage } from "@/api/errors";
import { useInvalidateMailboxes } from "@/queries/mailboxes";
import { smtpConnectionQuery } from "@/queries/smtp";

const props = defineProps<{ mailbox: Mailbox }>();
const invalidate = useInvalidateMailboxes();
const { data: connection } = useQuery(smtpConnectionQuery);
const open = ref(false);
const username = ref("");
const busy = ref(false);
const error = ref<string | null>(null);
const created = ref<CreatedCredential | null>(null);
const copied = ref(false);

watch(open, (isOpen) => {
    if (isOpen) {
        username.value = props.mailbox.name.toLowerCase().trim().replace(/\s+/g, "-");
        error.value = null;
        created.value = null;
        copied.value = false;
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

async function copy(text: string) {
    await navigator.clipboard.writeText(text);
    copied.value = true;
}
</script>
