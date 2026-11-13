<template>
    <ErrorAlert v-if="error" what="mailboxes" :message="errorMessage(error)" @retry="refetch()" />
    <v-skeleton-loader v-else-if="isLoading" type="table" />
    <NoMailboxes v-else-if="!mailboxes?.length" :admin="auth.isAdministrator" />
    <template v-else>
        <v-text-field
            v-model="search"
            class="mailbox-search"
            placeholder="Find a mailbox"
            prepend-inner-icon="mdi-magnify"
            hide-details
            clearable
        />
        <v-card border flat>
            <v-table v-if="filtered.length">
                <thead>
                    <tr>
                        <th>Mailbox</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="mailbox in filtered" :key="mailbox.id">
                        <td class="font-weight-medium">{{ mailbox.name }}</td>
                    </tr>
                </tbody>
            </v-table>
            <div v-else class="empty-state">
                <v-icon icon="mdi-magnify-close" size="44" />
                <p class="empty-state__text">No mailbox matches "{{ search }}".</p>
            </div>
        </v-card>
    </template>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useQuery } from "@pinia/colada";
import { errorMessage } from "@/api/errors";
import { mailboxesQuery } from "@/queries/mailboxes";
import { useAuthStore } from "@/store/auth";
import ErrorAlert from "@c/ErrorAlert.vue";
import NoMailboxes from "@c/mailboxes/NoMailboxes.vue";

const auth = useAuthStore();
const { data: mailboxes, error, isLoading, refetch } = useQuery(mailboxesQuery);
const search = ref<string | null>("");

const filtered = computed(() => {
    const query = (search.value ?? "").trim().toLowerCase();
    return (mailboxes.value ?? []).filter((m) => m.name.toLowerCase().includes(query));
});
</script>
