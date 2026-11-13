<template>
    <v-menu v-model="open" :close-on-content-click="false" location="bottom start">
        <template #activator="{ props: activator }">
            <button v-bind="activator" class="switcher" type="button" aria-label="Switch mailbox">
                <h1 class="switcher__name">{{ current.name }}</h1>
                <v-icon icon="mdi-chevron-down" size="20" />
            </button>
        </template>
        <v-card class="switcher__menu" border>
            <v-text-field
                v-model="search"
                placeholder="Find a mailbox"
                prepend-inner-icon="mdi-magnify"
                density="compact"
                hide-details
                autofocus
                class="ma-2"
            />
            <v-list density="compact" class="switcher__list" nav>
                <v-list-item
                    v-for="mailbox in filtered"
                    :key="mailbox.id"
                    :title="mailbox.name"
                    :active="mailbox.id === current.id"
                    color="primary"
                    @click="go(mailbox.id)"
                >
                    <template #append>
                        <v-badge
                            v-if="mailbox.unreadCount"
                            :content="mailbox.unreadCount"
                            color="primary"
                            inline
                        />
                    </template>
                </v-list-item>
                <div v-if="!filtered.length" class="text-medium-emphasis pa-3">
                    No mailbox matches "{{ search }}".
                </div>
            </v-list>
            <v-divider />
            <v-btn variant="text" color="primary" block :to="{ name: 'Mailboxes' }">
                See all {{ mailboxes.length }} mailboxes
            </v-btn>
        </v-card>
    </v-menu>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRouter } from "vue-router";
import type { Mailbox, MailboxId } from "@/api/mailboxes";

const props = defineProps<{ current: Mailbox; mailboxes: Mailbox[] }>();
const router = useRouter();
const open = ref(false);
const search = ref<string | null>("");

watch(open, () => (search.value = ""));

const filtered = computed(() => {
    const query = (search.value ?? "").trim().toLowerCase();
    return props.mailboxes.filter((m) => m.name.toLowerCase().includes(query));
});

function go(mailboxId: MailboxId) {
    open.value = false;
    void router.push({ name: "Inbox", params: { mailboxId } });
}
</script>
