<template>
  <header class="app-header">
    <div class="app-header__left">
      <router-link :to="{ name: 'classes' }" class="app-header__logo-link" aria-label="Accueil">
        <img :src="logoGridit" alt="GridIt" class="app-header__logo" />
      </router-link>
    </div>

    <div class="app-header__right">
      <div v-if="!isMobile && sessions.length" class="app-header__session">
        <label class="app-header__session-label" for="header-session-select">
          {{ t("navigation.sessions") }}
        </label>
        <select
          id="header-session-select"
          v-model="selectedSessionId"
          class="app-header__session-select"
        >
          <option
            v-for="session in sessions"
            :key="session.id"
            :value="session.id"
          >
            {{ session.name }}
          </option>
        </select>
        <button
          type="button"
          class="app-header__session-add-btn"
          @click="goToSessions"
        >
          {{ t("navigation.addSession") }}
        </button>
      </div>

      <div v-if="!isMobile" class="app-header__nav-menu" ref="navMenuRef">
        <button
          type="button"
          class="app-header__nav-trigger"
          @click="toggleNavMenu"
          aria-haspopup="true"
          :aria-expanded="isNavMenuOpen"
          aria-label="Navigation principale"
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="3" y1="6" x2="21" y2="6"></line><line x1="3" y1="12" x2="21" y2="12"></line><line x1="3" y1="18" x2="21" y2="18"></line></svg>
          <span>{{ t("global.menu") }}</span>
        </button>
        <Transition name="dropdown">
          <div v-show="isNavMenuOpen" class="app-header__dropdown app-header__dropdown--nav">
            <button
              type="button"
              class="app-header__dropdown-item"
              :class="{ 'app-header__dropdown-item--disabled': isCurrentRoute('grids') }"
              :disabled="isCurrentRoute('grids')"
              @click="goToGrids"
            >
              {{ t("routes.grids.name") }}
            </button>
            <button
              type="button"
              class="app-header__dropdown-item"
              :class="{ 'app-header__dropdown-item--disabled': isCurrentRoute('groupes') }"
              :disabled="isCurrentRoute('groupes')"
              @click="goToGroupes"
            >
              {{ t("navigation.groups") }}
            </button>
            <button
              v-if="isSuperAdmin"
              type="button"
              class="app-header__dropdown-item"
              :class="{ 'app-header__dropdown-item--disabled': isCurrentRoute('admin.children.programs') }"
              :disabled="isCurrentRoute('admin.children.programs')"
              @click="goToPrograms"
            >
              {{ t("routes.admin.children.programs.name") }}
            </button>
          </div>
        </Transition>
      </div>

      <div v-if="!isMobile" class="app-header__settings" ref="settingsRef">
        <button
          type="button"
          class="app-header__settings-trigger"
          @click="toggleSettings"
          aria-haspopup="true"
          :aria-expanded="isSettingsOpen"
          aria-label="Paramètres et langue"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"></path></svg>
        </button>
        <Transition name="dropdown">
          <div v-show="isSettingsOpen" class="app-header__dropdown app-header__dropdown--settings">
            <div class="app-header__lang-section">
              <button
                type="button"
                class="app-header__lang-btn"
                :class="{ 'app-header__lang-btn--active': currentLocale === 'fr' }"
                @click="switchLang('fr')"
              >
                FR
              </button>
              <button
                type="button"
                class="app-header__lang-btn"
                :class="{ 'app-header__lang-btn--active': currentLocale === 'en' }"
                @click="switchLang('en')"
              >
                EN
              </button>
            </div>
          </div>
        </Transition>
      </div>

      <div class="app-header__profile" ref="profileRef">
        <button
          type="button"
          class="app-header__profile-trigger"
          @click="toggleDropdown"
          aria-haspopup="true"
          :aria-expanded="isDropdownOpen"
        >
          <div class="app-header__avatar">
            <IconFaceMan class="icon icon--white"/>
          </div>
          <span class="app-header__profile-name">{{ personStore.person?.fullName || personStore.person?.firstName?.substring(0, 5) || '' }}</span>
          <IconChevron :class="['icon', 'app-header__chevron', { 'icon--rotate-180': isDropdownOpen }]"/>
        </button>
        <Transition name="dropdown">
          <div v-show="isDropdownOpen" class="app-header__dropdown">
            <button type="button" class="app-header__dropdown-item app-header__dropdown-item--action" @click="logout">
              {{ t('navigation.logout') }}
            </button>
          </div>
        </Transition>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import IconFaceMan from "vue-material-design-icons/FaceMan.vue";
import IconChevron from "@/assets/icons/icon__chevron.svg";
import logoGridit from "@/assets/icons/logo__gridit.png";
import { usePersonStore } from "@/stores/personStore";
import { useUserStore } from "@/stores/userStore";
import { useAuthenticationService } from "@/inversify.config";
import { useMemberStore } from "@/stores/memberStore";
import { useAdministratorStore } from "@/stores/administratorStore";
import { useSessionsStore } from "@/stores/sessionsStore";
import { Role } from "@/types/enums";

const { t, getLocale, setLocale } = useI18n();
const router = useRouter();
const route = useRoute();
const personStore = usePersonStore();
const userStore = useUserStore();
const memberStore = useMemberStore();
const administratorStore = useAdministratorStore();
const authenticationService = useAuthenticationService();
const sessionsStore = useSessionsStore();

const isDropdownOpen = ref(false);
const isNavMenuOpen = ref(false);
const isSettingsOpen = ref(false);
const profileRef = ref<HTMLElement | null>(null);
const navMenuRef = ref<HTMLElement | null>(null);
const settingsRef = ref<HTMLElement | null>(null);

const windowWidth = ref(window.innerWidth);
const isMobile = computed(() => windowWidth.value < 768);
const isSuperAdmin = computed(() => userStore.hasRole(Role.Admin));
const currentLocale = computed(() => getLocale());

const sessions = computed(() => sessionsStore.getSessions);

const selectedSessionId = computed({
  get: () => sessionsStore.getSelectedSessionId ?? sessions.value[0]?.id ?? "",
  set: async (value: string) => {
    if (!value) return;
    sessionsStore.selectSession(value);
    const session = sessions.value.find((s) => s.id === value);
    const firstClassId = session?.classIds && session.classIds[0];
    if (firstClassId) {
      await router.push({
        name: "classes.detail",
        params: { classId: firstClassId },
      });
    }
  },
});

function isCurrentRoute(routeName: string): boolean {
  return route.name === routeName;
}

function toggleDropdown() {
  isDropdownOpen.value = !isDropdownOpen.value;
  isNavMenuOpen.value = false;
  isSettingsOpen.value = false;
}

function toggleNavMenu() {
  isNavMenuOpen.value = !isNavMenuOpen.value;
  isDropdownOpen.value = false;
  isSettingsOpen.value = false;
}

function toggleSettings() {
  isSettingsOpen.value = !isSettingsOpen.value;
  isDropdownOpen.value = false;
  isNavMenuOpen.value = false;
}

function closeAll() {
  isDropdownOpen.value = false;
  isNavMenuOpen.value = false;
  isSettingsOpen.value = false;
}

async function logout() {
  closeAll();
  await authenticationService.logout().catch(() => {});
  userStore.reset();
  personStore.reset();
  memberStore.reset();
  administratorStore.reset();
  await router.push(t("routes.login.path"));
}

function switchLang(locale: string) {
  setLocale(locale);
  document.documentElement.lang = locale;
  document.cookie = "lang=" + locale + ";path=/";
  closeAll();
}

function handleClickOutside(event: MouseEvent) {
  const target = event.target as Node;
  if (profileRef.value && !profileRef.value.contains(target)) {
    isDropdownOpen.value = false;
  }
  if (navMenuRef.value && !navMenuRef.value.contains(target)) {
    isNavMenuOpen.value = false;
  }
  if (settingsRef.value && !settingsRef.value.contains(target)) {
    isSettingsOpen.value = false;
  }
}

function onResize() {
  windowWidth.value = window.innerWidth;
}

onMounted(() => {
  document.addEventListener("click", handleClickOutside);
  window.addEventListener("resize", onResize);
  sessionsStore.fetchSessions();
});

watch(
  sessions,
  (list) => {
    const firstId = list[0]?.id ?? null;
    if (!sessionsStore.getSelectedSessionId && firstId) {
      sessionsStore.selectSession(firstId);
    }
  },
  { immediate: true }
);

onUnmounted(() => {
  document.removeEventListener("click", handleClickOutside);
  window.removeEventListener("resize", onResize);
});

async function goToSessions() {
  closeAll();
  await router.push({ name: "sessions.index" });
}

async function goToGrids() {
  closeAll();
  await router.push({ name: "grids" });
}

async function goToGroupes() {
  closeAll();
  await router.push({ name: "groupes" });
}

async function goToPrograms() {
  closeAll();
  await router.push({ name: "admin.children.programs" });
}
</script>
