<template>
  <div class="content-grid content-grid--subpage content-grid--subpage-table">
    <div class="content-grid__header program-header">
      <h1 class="back-link">{{ t("routes.admin.children.programs.name") }}</h1>
      <div class="program-actions">
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
    </div>

    <Card>
      <DataTable
        :headers="headers"
        :items="items"
        @delete="onDelete"
        @row-click="onSelectProgram"
      />
    </Card>

    <Card v-if="selectedProgram">
      <div class="program-skills">
        <h2>{{ t("pages.programs.skillsTitle", { name: selectedProgram.name }) }}</h2>
        <p class="program-skills__hint">{{ t("pages.programs.skillsHint") }}</p>

        <div v-if="isSkillsLoading" class="program-skills__status">
          {{ t("pages.programs.skillsLoading") }}
        </div>

        <div v-else-if="allSkills.length === 0" class="program-skills__status">
          {{ t("pages.programs.noSkillsAvailable") }}
        </div>

        <div v-else class="program-skills__list">
          <label
            v-for="skill in allSkills"
            :key="skill.id"
            class="program-skills__item"
          >
            <input
              :checked="selectedSkillIds.includes(skill.id)"
              type="checkbox"
              @change="onSkillCheckboxChange(skill.id, $event)"
            />
            <span>{{ skill.label }}</span>
          </label>
        </div>

        <div v-if="isSavingSkills" class="program-skills__status">
          {{ t("pages.programs.skillsSaving") }}
        </div>
      </div>
    </Card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref } from "vue";
import { useI18n } from "vue3-i18n";
import type { Header } from "vue3-easy-data-table";
import DataTable from "@/components/layouts/items/DataTable.vue";
import Card from "@/components/layouts/items/Card.vue";
import { useProgramService } from "@/inversify.config";
import { notifyError, notifySuccess } from "@/notify";

type ProgramItem = { id: string; name: string };
type SkillItem = { id: string; label: string };

const { t } = useI18n();
const programService = useProgramService();
const programs = ref<ProgramItem[]>([]);
const newProgramName = ref("");
const selectedProgramId = ref<string | null>(null);
const allSkills = ref<SkillItem[]>([]);
const selectedSkillIds = ref<string[]>([]);
const isSkillsLoading = ref(false);
const isSavingSkills = ref(false);

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
const selectedProgram = computed(() =>
  programs.value.find((p) => p.id === selectedProgramId.value) ?? null
);

onMounted(async () => {
  await Promise.all([loadPrograms(), loadSkills()]);

  if (programs.value.length === 0) {
    await new Promise((r) => setTimeout(r, 500));
    await loadPrograms();
  }
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
    const created = await programService.createProgram(trimmed);
    newProgramName.value = "";
    await loadPrograms();
    selectedProgramId.value = created.id;
    await loadProgramSkills(created.id);
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
    if (selectedProgramId.value === item.id) {
      selectedProgramId.value = null;
      selectedSkillIds.value = [];
    }
    notifySuccess(t("pages.programs.deleted"));
  } catch (error) {
    console.error(error);
    notifyError(t("pages.programs.deleteError"));
  }
}

async function loadSkills() {
  try {
    const response = await fetch("/api/skills");
    if (!response.ok) throw new Error("Failed to load skills");
    const data = (await response.json()) as SkillItem[];
    allSkills.value = Array.isArray(data) ? data : [];
  } catch (error) {
    console.error(error);
    allSkills.value = [];
    notifyError(t("pages.programs.skillsLoadError"));
  }
}

async function onSelectProgram(item: { id: string }) {
  selectedProgramId.value = item.id;
  await loadProgramSkills(item.id);
}

async function loadProgramSkills(programId: string) {
  isSkillsLoading.value = true;
  try {
    const skills = await programService.getProgramSkills(programId);
    selectedSkillIds.value = skills.map((x) => x.id);
  } catch (error) {
    console.error(error);
    selectedSkillIds.value = [];
    notifyError(t("pages.programs.skillsLoadError"));
  } finally {
    isSkillsLoading.value = false;
  }
}

function toggleSkill(skillId: string, checked: boolean) {
  if (checked) {
    if (!selectedSkillIds.value.includes(skillId)) {
      selectedSkillIds.value = [...selectedSkillIds.value, skillId];
    }
    return;
  }

  selectedSkillIds.value = selectedSkillIds.value.filter((id) => id !== skillId);
}

function onSkillCheckboxChange(skillId: string, event: Event) {
  const target = event.target as HTMLInputElement | null;
  toggleSkill(skillId, !!target?.checked);
  scheduleAutoSave();
}

let autoSaveTimer: ReturnType<typeof setTimeout> | null = null;

function scheduleAutoSave() {
  if (autoSaveTimer) clearTimeout(autoSaveTimer);
  autoSaveTimer = setTimeout(() => saveProgramSkills(), 400);
}

onBeforeUnmount(() => {
  if (autoSaveTimer) clearTimeout(autoSaveTimer);
});

async function saveProgramSkills() {
  if (!selectedProgramId.value) return;

  isSavingSkills.value = true;
  try {
    await programService.saveProgramSkills(selectedProgramId.value, selectedSkillIds.value);
  } catch (error) {
    console.error(error);
    notifyError(t("pages.programs.skillsSaveError"));
  } finally {
    isSavingSkills.value = false;
  }
}
</script>

<style scoped lang="scss">
.program-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.program-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.program-skills {
  display: grid;
  gap: 12px;
}

.program-skills__hint,
.program-skills__status {
  margin: 0;
  color: #6b7280;
}

.program-skills__list {
  display: grid;
  gap: 8px;
}

.program-skills__item {
  display: flex;
  gap: 8px;
  align-items: center;
}

.program-skills__actions {
  margin-top: 8px;
}
</style>
