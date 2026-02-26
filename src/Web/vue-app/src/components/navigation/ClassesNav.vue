<template>
  <nav class="side-nav side-nav--classes">
    <h2 class="side-nav__title">{{ t("navigation.classes") }}</h2>
    <ul class="side-nav__list">
      <li v-for="c in classesStore.getClasses" :key="c.id">
        <RouterLink
          v-if="c.id"
          :to="{ name: 'classes.detail', params: { classId: c.id } }"
          class="side-nav__link"
          active-class="side-nav__link--active"
        >
          {{ c.name }}
        </RouterLink>
      </li>
    </ul>
    <div class="side-nav__footer">
      <RouterLink :to="{ name: 'evaluation' }" class="side-nav__add-btn side-nav__add-btn--eval">
        {{ t("evaluation.openEvaluation") }}
      </RouterLink>
      <button type="button" class="side-nav__add-btn" @click="onAddClass">
        {{ t("navigation.addClass") }}
      </button>
    </div>

    <CreateClassPopup v-if="showCreatePopup" @close="showCreatePopup = false" />
  </nav>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import CreateClassPopup from "@/components/popups/CreateClassPopup.vue";

const { t } = useI18n();
const classesStore = useClassesStore();
const showCreatePopup = ref(false);

function onAddClass() {
  showCreatePopup.value = true;
}
</script>
