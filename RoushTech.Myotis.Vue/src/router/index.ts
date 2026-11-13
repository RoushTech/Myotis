import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        name: "Mailboxes",
        component: () => import("@v/MailboxesView.vue"),
    },
    {
        path: "/login",
        name: "Login",
        component: () => import("@v/auth/LoginView.vue"),
        meta: { guest: true },
    },
    {
        path: "/setup",
        name: "Setup",
        component: () => import("@v/auth/SetupView.vue"),
        meta: { guest: true },
    },
    { path: "/:pathMatch(.*)*", redirect: { name: "Mailboxes" } },
];

export default createRouter({
    history: createWebHistory(),
    routes,
});
