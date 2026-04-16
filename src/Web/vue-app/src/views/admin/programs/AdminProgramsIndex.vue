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
        <p class="program-skills__hint">{{ t("pages.programs.skillsProgramHint") }}</p>

        <div v-if="isSkillsLoading" class="program-skills__status">
          {{ t("pages.programs.skillsLoading") }}
        </div>

        <div v-else-if="programSkills.length === 0" class="program-skills__status">
          {{ t("pages.programs.noProgramSkills") }}
        </div>

        <ul v-else class="program-skills__list">
          <li
            v-for="skill in programSkills"
            :key="skill.id"
            class="program-skills__row"
          >
            <span class="program-skills__name">{{ skill.label }}</span>
            <button
              type="button"
              class="program-skills__delete"
              :disabled="removingSkillId === skill.id"
              :aria-label="t('global.actions.delete')"
              :title="t('global.actions.delete')"
              @click="onRemoveSkill(skill)"
            >
              <IconDelete class="icon icon--black" />
            </button>
          </li>
        </ul>

        <div class="program-skills__actions">
          <button type="button" class="btn" @click="isAddPopupOpen = true">
            {{ t("pages.programs.addSkillButton") }}
          </button>
        </div>
      </div>
    </Card>

    <AddSkillToProgramPopup
      v-if="isAddPopupOpen && selectedProgramId"
      :program-id="selectedProgramId"
      :existing-labels="programSkills.map((s) => s.label)"
      @close="isAddPopupOpen = false"
      @added="onSkillAdded"
    />

    <ConfirmRemoveSkillPopup
      v-if="skillPendingRemoval"
      :skill-label="skillPendingRemoval.label"
      :is-loading="removingSkillId === skillPendingRemoval.id"
      @close="skillPendingRemoval = null"
      @confirm="confirmRemoveSkill"
    />

    <ConfirmDeletePopup
      v-if="programPendingDelete"
      :message="t('pages.programs.deleteConfirm')"
      :is-loading="isDeletingProgram"
      @close="programPendingDelete = null"
      @confirm="confirmDeleteProgram"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue3-i18n";
import type { Header } from "vue3-easy-data-table";
import DataTable from "@/components/layouts/items/DataTable.vue";
import Card from "@/components/layouts/items/Card.vue";
import AddSkillToProgramPopup from "@/components/popups/AddSkillToProgramPopup.vue";
import ConfirmRemoveSkillPopup from "@/components/popups/ConfirmRemoveSkillPopup.vue";
import ConfirmDeletePopup from "@/components/popups/ConfirmDeletePopup.vue";
import IconDelete from "@/assets/icons/icon__delete.svg";
import { useProgramService } from "@/inversify.config";
import { notifyError, notifySuccess } from "@/notify";

type ProgramItem = { id: string; name: string };
type SkillItem = { id: string; label: string };

const { t } = useI18n();
const programService = useProgramService();
const programs = ref<ProgramItem[]>([]);
const newProgramName = ref("");
const selectedProgramId = ref<string | null>(null);
const programSkills = ref<SkillItem[]>([]);
const isSkillsLoading = ref(false);
const isAddPopupOpen = ref(false);
const removingSkillId = ref<string | null>(null);
const skillPendingRemoval = ref<SkillItem | null>(null);
const programPendingDelete = ref<ProgramItem | null>(null);
const isDeletingProgram = ref(false);

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
  await loadPrograms();

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

function onDelete(item: { id: string }) {
  const program = programs.value.find((p) => p.id === item.id);
  if (program) programPendingDelete.value = program;
}

async function confirmDeleteProgram() {
  if (!programPendingDelete.value) return;
  isDeletingProgram.value = true;
  try {
    await programService.deleteProgram(programPendingDelete.value.id);
    programs.value = programs.value.filter((x) => x.id !== programPendingDelete.value!.id);
    if (selectedProgramId.value === programPendingDelete.value.id) {
      selectedProgramId.value = null;
      programSkills.value = [];
    }
    notifySuccess(t("pages.programs.deleted"));
    programPendingDelete.value = null;
  } catch (error) {
    console.error(error);
    notifyError(t("pages.programs.deleteError"));
  } finally {
    isDeletingProgram.value = false;
  }
}

async function onSelectProgram(item: { id: string }) {
  selectedProgramId.value = item.id;
  await loadProgramSkills(item.id);
}

async function loadProgramSkills(programId: string) {
  isSkillsLoading.value = true;
  try {
    programSkills.value = await programService.getProgramSkills(programId);
  } catch (error) {
    console.error(error);
    programSkills.value = [];
    notifyError(t("pages.programs.skillsLoadError"));
  } finally {
    isSkillsLoading.value = false;
  }
}

async function onSkillAdded() {
  if (!selectedProgramId.value) return;
  await loadProgramSkills(selectedProgramId.value);
}

function onRemoveSkill(skill: SkillItem) {
  skillPendingRemoval.value = skill;
}

async function confirmRemoveSkill() {
  const skill = skillPendingRemoval.value;
  if (!skill || !selectedProgramId.value) return;

  removingSkillId.value = skill.id;
  try {
    await programService.removeSkillFromProgram(selectedProgramId.value, skill.id);
    programSkills.value = programSkills.value.filter((x) => x.id !== skill.id);
    notifySuccess(t("pages.programs.skillRemoved"));
    skillPendingRemoval.value = null;
  } catch (error) {
    console.error(error);
    notifyError(t("pages.programs.skillRemoveError"));
  } finally {
    removingSkillId.value = null;
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
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 4px;
}

.program-skills__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 12px;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  background: #fff;
}

.program-skills__name {
  flex: 1;
}

.program-skills__delete {
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 4px;
  border-radius: 4px;
  display: inline-flex;
  align-items: center;
  justify-content: center;

  &:hover:not(:disabled) {
    background: #fee2e2;
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
}

.program-skills__actions {
  margin-top: 8px;
}
</style>
