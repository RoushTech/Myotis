<template>
    <div class="mailbox-picker">
        <div class="mailbox-picker__label">
            Mailboxes <span class="text-medium-emphasis">{{ selected.length }} selected</span>
        </div>
        <v-text-field
            v-model="search"
            placeholder="Find a mailbox"
            prepend-inner-icon="mdi-magnify"
            density="compact"
            hide-details
            clearable
        />
        <div class="mailbox-picker__list">
            <v-checkbox
                v-for="mailbox in filtered"
                :key="mailbox.id"
                v-model="selected"
                :value="mailbox.id"
                :label="mailbox.name"
                density="compact"
                hide-details
            />
            <div v-if="!filtered.length" class="text-medium-emphasis pa-2">
                {{ mailboxes.length ? "No mailbox matches." : "There are no mailboxes yet." }}
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import type { Mailbox, MailboxId } from "@/api/mailboxes";

const props = defineProps<{ mailboxes: Mailbox[] }>();
const selected = defineModel<MailboxId[]>({ required: true });
const search = ref<string | null>("");

const filtered = computed(() => {
    const query = (search.value ?? "").trim().toLowerCase();
    return props.mailboxes.filter((m) => m.name.toLowerCase().includes(query));
});
</script>
