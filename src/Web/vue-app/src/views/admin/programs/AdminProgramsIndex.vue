<template>
  <div class="content-grid content-grid--subpage content-grid--subpage-table">
    <div class="content-grid__header">
      <h1 class="back-link">{{ t("routes.admin.children.programs.name") }}</h1>
    </div>

    <div class="content-grid__actions program-actions">
      <input
        v-model="newProgramName"
        class="form__input"
        type="text"
        :placeholder="t('pages.programs.newProgramPlaceholder')"
        @keyup.enter="createProgram"
      />
      <button type="button" class="btn" @click="createProgram">
        {{ t("pages.programs.addProgram") }}
      </button>
    </div>

    <Card>
      <DataTable
        :headers="headers"
        :items="items"
        @delete="onDelete"
      />
    </Card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue3-i18n";
import type { Header } from "vue3-easy-data-table";
import DataTable from "@/components/layouts/items/DataTable.vue";
import Card from "@/components/layouts/items/Card.vue";
import { useProgramService } from "@/inversify.config";
import { notifyError, notifySuccess } from "@/notify";

type ProgramItem = { id: string; name: string };

const { t } = useI18n();
const programService = useProgramService();
const programs = ref<ProgramItem[]>([]);
const newProgramName = ref("");

const headers: Header[] = [
  { text: t("pages.programs.columns.name"), value: "name", sortable: true },
  { text: t("global.table.actions"), value: "actions", width: 80 },
];

const items = computed(() =>
  programs.value.map((p) => ({
    id: p.id,
    name: p.name,
    actions: { delete: true },
  }))
);

onMounted(async () => {
  await loadPrograms();
});

async function loadPrograms() {
  try {
    programs.value = await programService.getAllPrograms();
  } catch (error) {
    console.error(error);
    notifyError(t("pages.programs.loadError"));
  }
}

async function createProgram() {
  const trimmed = newProgramName.value.trim();
  if (!trimmed) return;

  try {
    await programService.createProgram(trimmed);
    newProgramName.value = "";
    await loadPrograms();
    notifySuccess(t("pages.programs.created"));
  } catch (error) {
    console.error(error);
    notifyError(t("pages.programs.createError"));
  }
}

async function onDelete(item: { id: string }) {
  if (!confirm(t("pages.programs.deleteConfirm"))) return;

  try {
    await programService.deleteProgram(item.id);
    programs.value = programs.value.filter((x) => x.id !== item.id);
    notifySuccess(t("pages.programs.deleted"));
  } catch (error) {
    console.error(error);
    notifyError(t("pages.programs.deleteError"));
  }
}
</script>

<style scoped lang="scss">
.program-actions {
  display: grid;
  grid-template-columns: minmax(240px, 420px) auto;
  gap: 8px;
}
</style>
