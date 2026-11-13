<template>
    <v-card border flat>
        <v-card-item>
            <v-card-title>People with access</v-card-title>
            <v-card-subtitle class="card-subtitle">
                They can read, delete and empty this mailbox. Administrators always have access.
            </v-card-subtitle>
            <template #append>
                <v-btn
                    color="primary"
                    size="small"
                    prepend-icon="mdi-account-plus-outline"
                    @click="adding = true"
                >
                    Add person
                </v-btn>
            </template>
        </v-card-item>
        <ErrorAlert
            v-if="error"
            class="ma-4"
            what="people"
            :message="errorMessage(error)"
            @retry="refetch()"
        />
        <v-skeleton-loader v-else-if="isLoading" type="table-row@2" />
        <v-table v-else>
            <tbody>
                <tr v-for="person in people" :key="person.userId">
                    <td>
                        {{ person.email }}
                        <v-chip
                            v-if="person.isAdministrator"
                            color="primary"
                            size="small"
                            variant="tonal"
                            class="ml-2"
                        >
                            Administrator
                        </v-chip>
                    </td>
                    <td class="text-end">
                        <v-btn
                            v-if="!person.isAdministrator"
                            variant="text"
                            color="error"
                            size="small"
                            :loading="removing === person.userId"
                            @click="remove(person)"
                        >
                            Remove
                        </v-btn>
                    </td>
                </tr>
            </tbody>
        </v-table>
        <v-dialog v-model="adding" max-width="480">
            <v-card :title="`Add person to ${mailbox.name}`">
                <v-card-text>
                    <v-autocomplete
                        v-if="candidates.length"
                        v-model="chosen"
                        :items="candidates"
                        item-title="email"
                        item-value="id"
                        label="Person"
                        autofocus
                    />
                    <p v-else class="text-medium-emphasis">
                        Everyone already has access. Add a user first from Users.
                    </p>
                    <v-alert v-if="addError" type="error" variant="tonal">{{ addError }}</v-alert>
                </v-card-text>
                <v-card-actions>
                    <v-spacer />
                    <v-btn variant="text" @click="adding = false">Cancel</v-btn>
                    <v-btn
                        color="primary"
                        variant="flat"
                        :disabled="!chosen"
                        :loading="busy"
                        @click="add"
                    >
                        Add
                    </v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
    </v-card>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useQuery } from "@pinia/colada";
import type { UserId } from "@/api/auth";
import type { Mailbox } from "@/api/mailboxes";
import { userApi, type Person } from "@/api/users";
import { errorMessage } from "@/api/errors";
import { peopleQuery, useInvalidateAccess, usersQuery } from "@/queries/users";
import ErrorAlert from "@c/ErrorAlert.vue";

const props = defineProps<{ mailbox: Mailbox }>();
const invalidate = useInvalidateAccess();
const { data: people, error, isLoading, refetch } = useQuery(() => peopleQuery(props.mailbox.id));
const { data: users } = useQuery(usersQuery);
const adding = ref(false);
const chosen = ref<UserId | null>(null);
const busy = ref(false);
const addError = ref<string | null>(null);
const removing = ref<UserId | null>(null);

const candidates = computed(() => {
    const present = new Set((people.value ?? []).map((p) => p.userId));
    return (users.value ?? []).filter((u) => !u.isAdministrator && !present.has(u.id));
});

watch(adding, () => {
    chosen.value = null;
    addError.value = null;
});

async function add() {
    if (!chosen.value) {
        return;
    }
    busy.value = true;
    addError.value = null;
    try {
        await userApi.addPerson(props.mailbox.id, chosen.value);
        await invalidate();
        adding.value = false;
    } catch (e) {
        addError.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}

async function remove(person: Person) {
    removing.value = person.userId;
    try {
        await userApi.removePerson(props.mailbox.id, person.userId);
        await invalidate();
    } finally {
        removing.value = null;
    }
}
</script>
