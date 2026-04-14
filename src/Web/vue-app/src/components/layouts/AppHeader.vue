<template>
  <header class="app-header">
    <div class="app-header__left">
      <LangSwitcher v-if="!isMobile" class="app-header__lang" />
      <div v-if="!isMobile" class="app-header__session">
        <label
          v-if="sessions.length"
          class="app-header__session-label"
          for="header-session-select"
        >
          {{ t("navigation.sessions") }}
        </label>
        <select
          v-if="sessions.length"
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
      <button
        v-if="!isMobile"
        type="button"
        class="app-header__grids-btn"
        @click="goToGrids"
      >
        {{ t("routes.grids.name") }}
      </button>
      <button
        v-if="!isMobile"
        type="button"
        class="app-header__grids-btn"
        @click="goToGroupes"
      >
        {{ t("navigation.groups") }}
      </button>
      <button
        v-if="!isMobile && isSuperAdmin"
        type="button"
        class="app-header__grids-btn"
        @click="goToPrograms"
      >
        {{ t("routes.admin.children.programs.name") }}
      </button>
    </div>

    <div class="app-header__right">
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
import { useRouter } from "vue-router";
import { useI18n } from "vue3-i18n";
import IconFaceMan from "vue-material-design-icons/FaceMan.vue";
import IconChevron from "@/assets/icons/icon__chevron.svg";
import LangSwitcher from "@/components/layouts/items/LangSwitcher.vue";
import { usePersonStore } from "@/stores/personStore";
import { useUserStore } from "@/stores/userStore";
import { useAuthenticationService } from "@/inversify.config";
import { useMemberStore } from "@/stores/memberStore";
import { useAdministratorStore } from "@/stores/administratorStore";
import { useSessionsStore } from "@/stores/sessionsStore";
import { Role } from "@/types/enums";

const { t } = useI18n();
const router = useRouter();
const personStore = usePersonStore();
const userStore = useUserStore();
const memberStore = useMemberStore();
const administratorStore = useAdministratorStore();
const authenticationService = useAuthenticationService();
const sessionsStore = useSessionsStore();

const isDropdownOpen = ref(false);
const profileRef = ref<HTMLElement | null>(null);

const isMobile = computed(() => window.innerWidth < 768);
const isSuperAdmin = computed(() => userStore.hasRole(Role.Admin));

const sessions = computed(() => sessionsStore.getSessions);

const selectedSessionId = computed({
  get: () => sessionsStore.getSelectedSessionId ?? sessions.value[0]?.id ?? "",
  set: async (value: string) => {
    if (!value) {
      return;
    }

    // sélectionner la session
    sessionsStore.selectSession(value);

    // trouver le premier cours lié à cette session
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

function toggleDropdown() {
  isDropdownOpen.value = !isDropdownOpen.value;
}

function closeDropdown() {
  isDropdownOpen.value = false;
}

async function logout() {
  closeDropdown();
  await authenticationService.logout().catch(() => {});
  userStore.reset();
  personStore.reset();
  memberStore.reset();
  administratorStore.reset();
  await router.push(t("routes.login.path"));
}

function handleClickOutside(event: MouseEvent) {
  if (profileRef.value && !profileRef.value.contains(event.target as Node)) {
    closeDropdown();
  }
}

onMounted(() => {
  document.addEventListener("click", handleClickOutside);
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
});

async function goToSessions() {
  await router.push({ name: "sessions.index" });
}

async function goToGrids() {
  await router.push({ name: "grids" });
}

async function goToGroupes() {
  await router.push({ name: "groupes" });
}

async function goToPrograms() {
  await router.push({ name: "admin.children.programs" });
}
</script>

<style scoped lang="scss">
.app-header__left {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.app-header__session {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.app-header__session-label {
  font-size: 0.875rem;
  opacity: 0.9;
}

.app-header__session-select {
  min-width: 180px;
  padding: 0.3rem 0.6rem;
  border-radius: 4px;
  border: 1px solid rgba(255, 255, 255, 0.8);
  background-color: #ffffff;
  color: #111827;
}

.app-header__session-add-btn {
  padding: 0.35rem 0.8rem;
  border-radius: 4px;
  border: 1px solid transparent;
  background-color: #4b9e6b;
  color: #ffffff;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  white-space: nowrap;
}

.app-header__session-add-btn:hover {
  background-color: #3b8156;
}

.app-header__grids-btn {
  padding: 0.35rem 0.8rem;
  border-radius: 4px;
  border: 1px solid rgba(255, 255, 255, 0.8);
  background-color: transparent;
  color: #ffffff;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  white-space: nowrap;
}

.app-header__grids-btn:hover {
  background-color: rgba(255, 255, 255, 0.15);
}
</style>
