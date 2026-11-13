<template>
    <v-card border flat>
        <v-card-item>
            <v-card-title>Delete mailbox</v-card-title>
            <v-card-subtitle class="card-subtitle">
                Deletes its messages and credentials. Apps using those credentials start getting
                sign-in failures.
            </v-card-subtitle>
            <template #append>
                <v-btn variant="text" color="error" size="small" @click="open = true">
                    Delete mailbox
                </v-btn>
            </template>
        </v-card-item>
        <v-dialog v-model="open" max-width="480">
            <v-card :title="`Delete ${mailbox.name}?`">
                <v-form @submit.prevent="remove">
                    <v-card-text>
                        <p class="mb-4">This can't be undone. Type the mailbox name to confirm.</p>
                        <v-text-field
                            v-model="typed"
                            :label="mailbox.name"
                            autofocus
                            :error-messages="error"
                        />
                    </v-card-text>
                    <v-card-actions>
                        <v-spacer />
                        <v-btn variant="text" @click="open = false">Cancel</v-btn>
                        <v-btn
                            type="submit"
                            color="error"
                            variant="flat"
                            :loading="busy"
                            :disabled="typed.trim() !== mailbox.name"
                        >
                            Delete mailbox
                        </v-btn>
                    </v-card-actions>
                </v-form>
            </v-card>
        </v-dialog>
    </v-card>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import { useRouter } from "vue-router";
import { mailboxApi, type Mailbox } from "@/api/mailboxes";
import { errorMessage } from "@/api/errors";
import { useInvalidateMailboxes } from "@/queries/mailboxes";

const props = defineProps<{ mailbox: Mailbox }>();
const router = useRouter();
const invalidate = useInvalidateMailboxes();
const open = ref(false);
const typed = ref("");
const busy = ref(false);
const error = ref<string | null>(null);

watch(open, () => {
    typed.value = "";
    error.value = null;
});

async function remove() {
    busy.value = true;
    error.value = null;
    try {
        await mailboxApi.remove(props.mailbox.id);
        open.value = false;
        // Leave first: refreshing while this page is open would refetch the deleted mailbox and fail.
        await router.push({ name: "AdminMailboxes" });
        await invalidate();
    } catch (e) {
        error.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}
</script>
