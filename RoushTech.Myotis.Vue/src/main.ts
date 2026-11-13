import { createApp } from "vue";
import { createPinia } from "pinia";
import { PiniaColada } from "@pinia/colada";
import App from "./App.vue";
import vuetify from "./plugins/vuetify";
import router from "./router";
import { useAuthStore } from "./store/auth";
import { onUnauthorized } from "./api/interceptors";

void (async function () {
    const pinia = createPinia();
    const app = createApp(App)
        .use(pinia)
        .use(PiniaColada, { queryOptions: { staleTime: 30_000 } })
        .use(vuetify);
    const auth = useAuthStore(pinia);

    onUnauthorized(() => {
        if (auth.isAuthenticated) {
            auth.me = null;
            void router.push({ name: "Login" });
        }
    });

    router.beforeEach((to) => {
        if (auth.setupRequired) {
            return to.name === "Setup" ? true : { name: "Setup" };
        }
        const guest = to.matched.some((m) => m.meta["guest"]);
        if (!guest && !auth.isAuthenticated) {
            auth.route = to.fullPath;
            return { name: "Login" };
        }
        if (guest && auth.isAuthenticated) {
            return "/";
        }
        if (to.matched.some((m) => m.meta["admin"]) && !auth.isAdministrator) {
            return "/";
        }
        return true;
    });

    await auth.check();

    // Installing the router starts the first navigation, so it waits until the guard has what it needs.
    app.use(router);
    app.mount("#app");
})();
