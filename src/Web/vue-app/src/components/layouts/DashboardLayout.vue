<template>
  <div class="dashboard" :class="{ 'dashboard--with-exams': hasClassSelected }">
    <AppHeader />

    <div class="dashboard__body">
      <ClassesNav />
      <ExamsNav v-if="hasClassSelected" />
      <main class="dashboard__content">
        <LogoutPopup />
        <Notifications />
        <RouterView v-slot="{ Component }">
          <template v-if="Component">
            <Suspense>
              <component :is="Component" />
              <template #fallback>
                <Loader />
              </template>
            </Suspense>
          </template>
        </RouterView>
      </main>
    </div>
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { useAdministratorService, useMemberService } from "@/inversify.config";
import AppHeader from "@/components/layouts/AppHeader.vue";
import ClassesNav from "@/components/navigation/ClassesNav.vue";
import ExamsNav from "@/components/navigation/ExamsNav.vue";
import LogoutPopup from "@/components/layouts/items/LogoutPopup.vue";
import Notifications from "@/components/layouts/items/Notifications.vue";
import Loader from "@/components/layouts/items/Loader.vue";
import { Administrator, Member } from "@/types";
import { Role } from "@/types/enums";
import { useAdministratorStore } from "@/stores/administratorStore";
import { useClassesStore } from "@/stores/classesStore";
import { useMemberStore } from "@/stores/memberStore";
import { usePersonStore } from "@/stores/personStore";
import { useUserStore } from "@/stores/userStore";

const route = useRoute();
const userStore = useUserStore();
const personStore = usePersonStore();
const memberStore = useMemberStore();
const administratorStore = useAdministratorStore();
const classesStore = useClassesStore();

const memberService = useMemberService();
const administratorService = useAdministratorService();

const userIsLoading = ref(true);

const hasClassSelected = computed(() => !!route.params.classId);

onMounted(async () => {
  // #region agent log
  fetch("http://127.0.0.1:7246/ingest/30a6a8d6-c62f-4864-9278-26401a3dafa6", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      location: "DashboardLayout.vue:onMounted:start",
      message: "DashboardLayout onMounted",
      data: { routeName: route.name },
      timestamp: Date.now(),
      hypothesisId: "H3",
    }),
  }).catch(() => {});
  // #endregion
  userIsLoading.value = true;
  if (userStore.hasRole(Role.Member)) {
    const member = (await memberService.getAuthenticated()) as Member;
    personStore.setPerson(member);
    memberStore.setMember(member);
  } else {
    const administrator = (await administratorService.getAuthenticated()) as Administrator;
    personStore.setPerson(administrator);
    administratorStore.setAdministrator(administrator);
  }
  userIsLoading.value = false;
  // Load classes after auth is ready so GET /classes has a valid token (fixes empty list on refresh)
  if (route.name === "classes" || route.name === "classes.index" || (typeof route.name === "string" && route.name.startsWith("classes."))) {
    await classesStore.fetchClasses();
  }
  // #region agent log
  fetch("http://127.0.0.1:7246/ingest/30a6a8d6-c62f-4864-9278-26401a3dafa6", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ location: "DashboardLayout.vue:onMounted:end", message: "DashboardLayout onMounted end", timestamp: Date.now(), hypothesisId: "H3" }),
  }).catch(() => {});
  // #endregion
});
</script>
