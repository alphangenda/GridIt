<template>
  <header class="app-header">
    <div class="app-header__left">
      <LangSwitcher v-if="!isMobile" class="app-header__lang" />
      <div v-if="!isMobile" class="app-header__session">
        <label class="app-header__session-label" for="header-session-select">
          {{ t("navigation.sessions") }}
        </label>
        <select
          id="header-session-select"
          v-model="selectedSessionId"
          class="app-header__session-select"
        >
          <option value="">{{ t("navigation.allSessions") }}</option>
          <option
            v-for="session in sessions"
            :key="session.id"
            :value="session.id"
          >
            {{ session.name }}
          </option>
        </select>
      </div>
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
          <span class="app-header__profile-name">{{ personStore.person?.fullName ?? '' }}</span>
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
import { ref, computed, onMounted, onUnmounted } from "vue";
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

const sessions = computed(() => sessionsStore.getSessions);

const selectedSessionId = computed({
  get: () => sessionsStore.getSelectedSessionId ?? "",
  set: (value: string) => {
    sessionsStore.selectSession(value || null);
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

onUnmounted(() => {
  document.removeEventListener("click", handleClickOutside);
});
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
</style>
