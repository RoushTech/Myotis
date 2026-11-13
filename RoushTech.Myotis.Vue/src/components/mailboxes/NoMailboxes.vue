<template>
    <v-card border flat>
        <div class="empty-state">
            <v-icon :icon="admin ? 'mdi-tray-arrow-down' : 'mdi-lock-outline'" size="44" />
            <h2 class="empty-state__title">
                {{ admin ? "No mailboxes yet" : "No mailboxes shared with you" }}
            </h2>
            <p class="empty-state__text">
                {{
                    admin
                        ? "Create a mailbox and give it an SMTP credential. Apps that send with that credential land here."
                        : "An administrator needs to add you to a mailbox before you can read its mail."
                }}
            </p>
            <v-btn
                v-if="admin"
                color="primary"
                prepend-icon="mdi-plus"
                class="mt-2"
                :to="{ name: 'AdminMailboxes' }"
            >
                Create a mailbox
            </v-btn>
        </div>
    </v-card>
</template>

<script setup lang="ts">
defineProps<{ admin: boolean }>();
</script>
