<template>
  <nav class="side-nav side-nav--classes">
    <h2 class="side-nav__title">{{ t("navigation.classes") }}</h2>
    <ul class="side-nav__list">
      <li v-for="cours in filteredClasses" :key="cours.id">
        <RouterLink
          v-if="cours.id"
          :to="{ name: 'classes.detail', params: { classId: cours.id } }"
          class="side-nav__link"
          active-class="side-nav__link--active"
        >
          {{ cours.name }}
        </RouterLink>
      </li>
    </ul>
  </nav>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import { useSessionsStore } from "@/stores/sessionsStore";

const { t } = useI18n();
const classesStore = useClassesStore();
const sessionsStore = useSessionsStore();

const filteredClasses = computed(() => {
  const selectedId = sessionsStore.getSelectedSessionId;
  if (!selectedId) {
    return classesStore.getClasses;
  }

  const selectedSession = sessionsStore.getSelectedSession;
  const classIds = selectedSession?.classIds ?? [];
  if (!classIds.length) {
    return [];
  }

  return classesStore.getClasses.filter(
    (c) => c.id && classIds.includes(c.id)
  );
});

</script>
