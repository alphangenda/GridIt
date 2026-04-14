<template>
  <nav class="side-nav side-nav--classes">
    <RouterLink :to="{ name: 'classes.index' }" class="side-nav__title side-nav__title--link">
      {{ t("navigation.classes") }}
    </RouterLink>
    <ul class="side-nav__list">
      <li v-for="cours in filteredClasses" :key="cours.id" class="side-nav__item">
        <button
          v-if="cours.id"
          class="side-nav__link side-nav__link--expandable"
          :class="{ 'side-nav__link--expanded': expandedClassId === cours.id }"
          @click="toggleClass(cours.id!)"
        >
          <span class="side-nav__link-text">{{ cours.name }}</span>
          <span
            class="side-nav__chevron"
            :class="{ 'side-nav__chevron--open': expandedClassId === cours.id }"
          >&#9660;</span>
        </button>
        <Transition name="slide">
          <div v-if="expandedClassId === cours.id" class="side-nav__dropdown">
            <ul class="side-nav__sub-list">
              <li v-for="group in getGroups(cours.id!)" :key="group.id">
                <RouterLink
                  :to="{ name: 'classes.groupExams', params: { classId: cours.id, groupId: group.id } }"
                  class="side-nav__sub-link"
                  active-class="side-nav__sub-link--active"
                >
                  {{ group.name }}
                </RouterLink>
              </li>
              <li v-if="getGroups(cours.id!).length === 0" class="side-nav__sub-empty">
                {{ t("navigation.noGroups") }}
              </li>
            </ul>
            <button
              type="button"
              class="side-nav__add-exam-btn"
              @click.stop="openAddGroup(cours.id!)"
            >
              + {{ t("navigation.addGroup") }}
            </button>
          </div>
        </Transition>
      </li>
    </ul>

    <ImportGroupPopup
      v-if="showAddPopup && addGroupClassId"
      :class-id="addGroupClassId"
      @close="onPopupClose"
    />
  </nav>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRoute, useRouter, onBeforeRouteUpdate } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import { useSessionsStore } from "@/stores/sessionsStore";
import ImportGroupPopup from "@/components/popups/ImportGroupPopup.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const classesStore = useClassesStore();
const sessionsStore = useSessionsStore();

const expandedClassId = ref<string | null>(null);
const showAddPopup = ref(false);
const addGroupClassId = ref<string | null>(null);
const initialized = ref(false);

// groups cache: classId -> list of groups
const groupsCache = ref<Record<string, { id: string; name: string }[]>>({});

const activeClassId = computed(() => route.params.classId as string | undefined);

const filteredClasses = computed(() => {
  const selectedId = sessionsStore.getSelectedSessionId;
  if (!selectedId) return classesStore.getClasses;

  const selectedSession = sessionsStore.getSelectedSession;
  const classIds = selectedSession?.classIds ?? [];
  if (!classIds.length) return [];

  return classesStore.getClasses.filter((c) => c.id && classIds.includes(c.id));
});

function getGroups(classId: string) {
  return groupsCache.value[classId] ?? [];
}

async function fetchGroups(classId: string) {
  const res = await fetch(`/api/classes/${classId}/groups`);
  if (!res.ok) return;
  const data = await res.json();
  groupsCache.value[classId] = data.map((g: any) => ({ id: String(g.id), name: String(g.name) }));
}

function toggleClass(classId: string) {
  if (expandedClassId.value === classId) {
    expandedClassId.value = null;
  } else {
    expandedClassId.value = classId;
    fetchGroups(classId);
    router.push({ name: "classes.detail", params: { classId } });
  }
}

function openAddGroup(classId: string) {
  addGroupClassId.value = classId;
  showAddPopup.value = true;
}

async function onPopupClose() {
  showAddPopup.value = false;
  if (addGroupClassId.value) {
    await fetchGroups(addGroupClassId.value);
  }
}

// Auto-expand once when classes are first loaded and a classId is in the URL
watch(
  filteredClasses,
  (classes) => {
    if (!initialized.value && classes.length > 0 && activeClassId.value) {
      expandedClassId.value = activeClassId.value;
      fetchGroups(activeClassId.value);
      initialized.value = true;
    }
  },
  { immediate: true }
);

// Auto-expand when navigating to a different class from elsewhere
onBeforeRouteUpdate((to, from) => {
  const newId = to.params.classId as string | undefined;
  const oldId = from.params.classId as string | undefined;
  if (newId && newId !== oldId) {
    expandedClassId.value = newId;
    fetchGroups(newId);
  }
});
</script>
