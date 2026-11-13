<template>
    <v-dialog v-model="open" max-width="520">
        <template #activator="{ props: activator }">
            <v-btn v-bind="activator" color="primary" prepend-icon="mdi-plus">New mailbox</v-btn>
        </template>
        <v-card title="New mailbox">
            <v-form @submit.prevent="create">
                <v-card-text>
                    <v-text-field
                        v-model="name"
                        label="Name"
                        placeholder="Billing staging"
                        hint="Shown to everyone with access."
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
                        :disabled="!name.trim()"
                    >
                        Create mailbox
                    </v-btn>
                </v-card-actions>
            </v-form>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import { useRouter } from "vue-router";
import { mailboxApi } from "@/api/mailboxes";
import { errorMessage } from "@/api/errors";
import { useInvalidateMailboxes } from "@/queries/mailboxes";

const router = useRouter();
const invalidate = useInvalidateMailboxes();
const open = ref(false);
const name = ref("");
const busy = ref(false);
const error = ref<string | null>(null);

watch(open, () => {
    name.value = "";
    error.value = null;
});

async function create() {
    busy.value = true;
    error.value = null;
    try {
        const mailbox = await mailboxApi.create(name.value);
        await invalidate();
        open.value = false;
        await router.push({ name: "AdminMailbox", params: { mailboxId: mailbox.id } });
    } catch (e) {
        error.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}
</script>
