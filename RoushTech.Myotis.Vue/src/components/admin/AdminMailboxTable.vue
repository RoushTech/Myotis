<template>
    <ErrorAlert v-if="error" what="mailboxes" :message="errorMessage(error)" @retry="refetch()" />
    <v-skeleton-loader v-else-if="isLoading" type="table" />
    <v-card v-else-if="!mailboxes?.length" border flat>
        <div class="empty-state">
            <v-icon icon="mdi-inbox-multiple-outline" size="44" />
            <h2 class="empty-state__title">No mailboxes yet</h2>
            <p class="empty-state__text">
                Create one for each environment that sends mail, such as staging or a hotfix branch.
            </p>
        </div>
    </v-card>
    <template v-else>
        <div class="toolbar">
            <v-text-field
                v-model="search"
                class="mailbox-search"
                placeholder="Find a mailbox"
                prepend-inner-icon="mdi-magnify"
                hide-details
                clearable
            />
            <span class="toolbar__count">{{ filtered.length }} of {{ mailboxes.length }}</span>
        </div>
        <v-card border flat>
            <v-table hover>
                <thead>
                    <tr>
                        <th>Name</th>
                        <th class="text-end">Credentials</th>
                        <th class="text-end">People</th>
                        <th>Created</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-if="!filtered.length">
                        <td colspan="4" class="text-medium-emphasis">
                            No mailbox matches "{{ search }}".
                        </td>
                    </tr>
                    <tr
                        v-for="mailbox in filtered"
                        :key="mailbox.id"
                        class="clickable-row"
                        @click="open(mailbox.id)"
                    >
                        <td class="font-weight-medium">{{ mailbox.name }}</td>
                        <td class="text-end">{{ mailbox.credentialCount }}</td>
                        <td class="text-end">{{ mailbox.peopleCount }}</td>
                        <td class="text-medium-emphasis">{{ shortDate(mailbox.createdAt) }}</td>
                    </tr>
                </tbody>
            </v-table>
        </v-card>
    </template>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { useQuery } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import { errorMessage } from "@/api/errors";
import { shortDate } from "@/lib/dates";
import { mailboxesQuery } from "@/queries/mailboxes";
import ErrorAlert from "@c/ErrorAlert.vue";

const router = useRouter();
const { data: mailboxes, error, isLoading, refetch } = useQuery(mailboxesQuery);
const search = ref<string | null>("");

const filtered = computed(() => {
    const query = (search.value ?? "").trim().toLowerCase();
    return (mailboxes.value ?? []).filter((m) => m.name.toLowerCase().includes(query));
});

function open(mailboxId: MailboxId) {
    void router.push({ name: "AdminMailbox", params: { mailboxId } });
}
</script>
