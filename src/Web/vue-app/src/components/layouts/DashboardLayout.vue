<template>
  <div class="dashboard">
    <AppHeader />

    <div class="dashboard__body">
      <div class="dashboard__nav-wrap" :class="{ 'dashboard__nav-wrap--collapsed': navCollapsed }">
        <ClassesNav />
      </div>
      <button
        class="dashboard__nav-toggle"
        :title="navCollapsed ? 'Ouvrir' : 'Fermer'"
        @click="navCollapsed = !navCollapsed"
      >{{ navCollapsed ? '›' : '‹' }}</button>
      <main class="dashboard__content">
        <LogoutPopup />
        <Notifications />
        <BreadcrumbNav />
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
import { onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { useAdministratorService, useMemberService } from "@/inversify.config";
import AppHeader from "@/components/layouts/AppHeader.vue";
import ClassesNav from "@/components/navigation/ClassesNav.vue";
import LogoutPopup from "@/components/layouts/items/LogoutPopup.vue";
import Notifications from "@/components/layouts/items/Notifications.vue";
import Loader from "@/components/layouts/items/Loader.vue";
import BreadcrumbNav from "@/components/navigation/BreadcrumbNav.vue";
import { Administrator, Member } from "@/types";
import { Role } from "@/types/enums";
import { useAdministratorStore } from "@/stores/administratorStore";
import { useClassesStore } from "@/stores/classesStore";
import { useMemberStore } from "@/stores/memberStore";
import { usePersonStore } from "@/stores/personStore";
import { useUserStore } from "@/stores/userStore";

const route = useRoute();
const navCollapsed = ref(false);
const userStore = useUserStore();
const personStore = usePersonStore();
const memberStore = useMemberStore();
const administratorStore = useAdministratorStore();
const classesStore = useClassesStore();

const memberService = useMemberService();
const administratorService = useAdministratorService();

const userIsLoading = ref(true);

onMounted(async () => {
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
  await classesStore.fetchClasses();
});
</script>
