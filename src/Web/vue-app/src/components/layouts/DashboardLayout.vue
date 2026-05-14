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
        <button
          type="button"
          class="theme-toggle"
          :title="themeStore.isDark ? t('navigation.lightMode') : t('navigation.darkMode')"
          @click="themeStore.toggle()"
        >
          <svg v-if="!themeStore.isDark" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path></svg>
          <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"></circle><line x1="12" y1="1" x2="12" y2="3"></line><line x1="12" y1="21" x2="12" y2="23"></line><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line><line x1="1" y1="12" x2="3" y2="12"></line><line x1="21" y1="12" x2="23" y2="12"></line><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line><line x1="18.36" y1="4.22" x2="19.78" y2="5.64"></line></svg>
        </button>
        <LogoutPopup />
        <Notifications />
        <BreadcrumbNav />
        <RouterView v-slot="{ Component, route: viewRoute }">
          <Transition name="page" mode="out-in">
            <div :key="viewRoute.name" v-if="Component">
              <Suspense>
                <component :is="Component" />
                <template #fallback>
                  <Loader />
                </template>
              </Suspense>
            </div>
          </Transition>
        </RouterView>
      </main>
    </div>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref, computed } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
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
import { useThemeStore } from "@/stores/themeStore";
import { useUserStore } from "@/stores/userStore";

const { t } = useI18n();
const route = useRoute();
const themeStore = useThemeStore();
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
