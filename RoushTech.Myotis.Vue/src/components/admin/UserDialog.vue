<template>
    <v-dialog :model-value="open" max-width="560" @update:model-value="emit('close')">
        <v-card :title="user ? `Edit ${user.email}` : 'Add user'">
            <v-form ref="form" @submit.prevent="save">
                <v-card-text class="user-dialog">
                    <v-text-field
                        v-if="!user"
                        v-model="email"
                        label="Email"
                        type="email"
                        autofocus
                        :rules="[required]"
                    />
                    <v-text-field
                        v-if="!user || resetting"
                        v-model="password"
                        :label="user ? 'New password' : 'Password'"
                        hint="Give it to them yourself. Myotis never sends email."
                        persistent-hint
                        class="mono-input"
                        :rules="[long]"
                    />
                    <v-btn
                        v-else
                        variant="text"
                        size="small"
                        class="align-self-start"
                        prepend-icon="mdi-lock-reset"
                        @click="startReset"
                    >
                        Set a new password
                    </v-btn>
                    <v-switch
                        v-model="isAdministrator"
                        color="primary"
                        label="Administrator (sees every mailbox and manages Myotis)"
                        hide-details
                        :disabled="isSelf"
                    />
                    <MailboxPicker
                        v-if="!isAdministrator"
                        v-model="mailboxIds"
                        :mailboxes="mailboxes ?? []"
                    />
                    <v-alert v-if="error" type="error" variant="tonal">{{ error }}</v-alert>
                </v-card-text>
                <v-card-actions>
                    <v-btn
                        v-if="user && !isSelf"
                        variant="text"
                        color="error"
                        @click="deleting = true"
                    >
                        Delete user
                    </v-btn>
                    <v-spacer />
                    <v-btn variant="text" @click="emit('close')">Cancel</v-btn>
                    <v-btn type="submit" color="primary" variant="flat" :loading="busy">
                        {{ user ? "Save" : "Add user" }}
                    </v-btn>
                </v-card-actions>
            </v-form>
        </v-card>
        <ConfirmDialog
            :open="deleting"
            :title="`Delete ${user?.email ?? ''}?`"
            body="They're signed out and lose access to every mailbox. This can't be undone."
            action="Delete user"
            :busy="busy"
            :error="error"
            @cancel="deleting = false"
            @confirm="remove"
        />
    </v-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useQuery } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import { userApi, type User } from "@/api/users";
import { errorMessage } from "@/api/errors";
import { suggestPassword } from "@/lib/passwords";
import { mailboxesQuery } from "@/queries/mailboxes";
import { useInvalidateAccess } from "@/queries/users";
import { useAuthStore } from "@/store/auth";
import ConfirmDialog from "@c/ConfirmDialog.vue";
import MailboxPicker from "@c/admin/MailboxPicker.vue";

const props = defineProps<{ open: boolean; user: User | null }>();
const emit = defineEmits<{ close: [] }>();
const auth = useAuthStore();
const invalidate = useInvalidateAccess();
const { data: mailboxes } = useQuery(mailboxesQuery);
const form = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);
const email = ref("");
const password = ref("");
const resetting = ref(false);
const isAdministrator = ref(false);
const mailboxIds = ref<MailboxId[]>([]);
const busy = ref(false);
const error = ref<string | null>(null);
const deleting = ref(false);
const isSelf = computed(() => props.user?.id === auth.me?.id);

watch(
    () => props.open,
    (isOpen) => {
        if (!isOpen) {
            return;
        }
        email.value = "";
        password.value = props.user ? "" : suggestPassword();
        resetting.value = false;
        isAdministrator.value = props.user?.isAdministrator ?? false;
        mailboxIds.value = [...(props.user?.mailboxIds ?? [])];
        error.value = null;
        deleting.value = false;
    }
);

function required(value: string) {
    return value.trim().length > 0 || "Enter their email address";
}

function long(value: string) {
    return (value.length >= 6 && /\d/.test(value)) || "At least 6 characters, including a digit";
}

function startReset() {
    resetting.value = true;
    password.value = suggestPassword();
}

async function save() {
    if (!(await form.value?.validate())?.valid) {
        return;
    }
    busy.value = true;
    error.value = null;
    try {
        if (props.user) {
            await userApi.update(props.user.id, {
                isAdministrator: isAdministrator.value,
                mailboxIds: mailboxIds.value,
                ...(resetting.value ? { password: password.value } : {}),
            });
        } else {
            await userApi.create({
                email: email.value,
                password: password.value,
                isAdministrator: isAdministrator.value,
                mailboxIds: isAdministrator.value ? [] : mailboxIds.value,
            });
        }
        await invalidate();
        emit("close");
    } catch (e) {
        error.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}

async function remove() {
    if (!props.user) {
        return;
    }
    busy.value = true;
    error.value = null;
    try {
        await userApi.remove(props.user.id);
        await invalidate();
        deleting.value = false;
        emit("close");
    } catch (e) {
        error.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}
</script>
