<template>
    <v-container class="page">
        <h1 class="page-title">Mailboxes</h1>
        <p class="page-subtitle">
            {{ auth.isAdministrator ? "Every mailbox in Myotis." : "Mailboxes shared with you." }}
        </p>
        <v-card border flat class="mt-5">
            <div class="empty-state">
                <v-icon
                    :icon="auth.isAdministrator ? 'mdi-tray-arrow-down' : 'mdi-lock-outline'"
                    size="44"
                />
                <h2 class="empty-state__title">
                    {{ auth.isAdministrator ? "No mailboxes yet" : "No mailboxes shared with you" }}
                </h2>
                <p class="empty-state__text">
                    {{
                        auth.isAdministrator
                            ? "Create a mailbox and give it an SMTP credential. Apps that send with that credential land here."
                            : "An administrator needs to add you to a mailbox before you can read its mail."
                    }}
                </p>
            </div>
        </v-card>
    </v-container>
</template>

<script setup lang="ts">
import { useAuthStore } from "@/store/auth";

const auth = useAuthStore();
</script>
