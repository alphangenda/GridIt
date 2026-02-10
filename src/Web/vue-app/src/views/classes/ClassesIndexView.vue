<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <h1>{{ t("navigation.classes") }}</h1>
      <div class="content-grid__actions">
        <button type="button" class="btn" @click="onAddClass">
          {{ t("navigation.addClass") }}
        </button>
      </div>
    </div>
    <Card>
      <DataTable
        :headers="classHeaders"
        :items="classItems"
        @delete="onDeleteClass"
      />
    </Card>

    <CreateClassPopup v-if="showCreatePopup" @close="showCreatePopup = false" />
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import Card from "@/components/layouts/items/Card.vue";
import DataTable from "@/components/layouts/items/DataTable.vue";
import CreateClassPopup from "@/components/popups/CreateClassPopup.vue";
import type { Header } from "vue3-easy-data-table";

const { t } = useI18n();
const router = useRouter();
const classesStore = useClassesStore();
const showCreatePopup = ref(false);

onMounted(() => {
  classesStore.fetchClasses();
});

const classHeaders: Header[] = [
  { text: t("navigation.className"), value: "name", sortable: true },
  { text: t("global.table.actions"), value: "actions", width: 120 },
];

const classItems = computed(() =>
  classesStore.getClasses.map((c) => ({
    id: c.id,
    name: c.name,
    actions: {
      view: { name: "classes.detail", params: { classId: c.id } },
      delete: true,
    },
  }))
);

function onAddClass() {
  showCreatePopup.value = true;
}

function onDeleteClass(item: { id: string }) {
  if (window.confirm(t("navigation.deleteClassConfirm"))) {
    classesStore.deleteClass(item.id);
  }
}
</script>
