<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="handleSubmit">
      <div class="popup__bg" @click="emit('close')"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("navigation.addGroup") }}</p>
          <button v-if="mode === 'new'" class="btn btn--import" type="button" @click="triggerFileInput">
            {{ t("navigation.importGroup") }}
          </button>
          <input
            ref="fileInputRef"
            type="file"
            accept=".json,.csv,.xlsx,.xls"
            style="display: none"
            @change="handleFileSelected"
          />
        </div>

        <!-- Tabs: only when adding to a class (not from exam context, not global) -->
        <div v-if="showTabs" class="popup__tabs">
          <button
            type="button"
            :class="['popup__tab', { 'popup__tab--active': mode === 'new' }]"
            @click="mode = 'new'; errorMessage = ''"
          >
            {{ t("navigation.createNewGroup") }}
          </button>
          <button
            type="button"
            :class="['popup__tab', { 'popup__tab--active': mode === 'existing' }]"
            @click="mode = 'existing'; errorMessage = ''"
          >
            {{ t("navigation.addExistingGroup") }}
          </button>
        </div>

        <div class="popup__content">
          <div class="popup__block">

            <!-- Error message -->
            <div v-if="errorMessage" class="popup__error">{{ errorMessage }}</div>

            <!-- NEW GROUP MODE -->
            <template v-if="mode === 'new'">
              <div class="form__group">
                <label class="form__label" for="group-name">{{ t("navigation.groupName") }}</label>
                <input
                  id="group-name"
                  ref="inputRef"
                  v-model="name"
                  class="form__input"
                  type="text"
                  required
                  @input="errorMessage = ''"
                />
              </div>

              <div v-if="students.length > 0" class="students-accordion">
                <button class="students-accordion__toggle" type="button" @click="expanded = !expanded">
                  <span>{{ t("navigation.studentsImported", { count: students.length }) }}</span>
                  <span class="students-accordion__chevron" :class="{ 'students-accordion__chevron--open': expanded }">&#9660;</span>
                </button>
                <div v-if="expanded" class="students-accordion__body">
                  <table class="students-table">
                    <thead>
                      <tr>
                        <th>{{ t("navigation.studentNumber") }}</th>
                        <th>{{ t("navigation.studentFirstName") }}</th>
                        <th>{{ t("navigation.studentLastName") }}</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(s, i) in students" :key="i">
                        <td>{{ s.number }}</td>
                        <td>{{ s.firstName }}</td>
                        <td>{{ s.lastName }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div class="form__submit">
                <button class="btn btn--fullscreen btn--red" type="button" @click="emit('close')">{{ t("global.cancel") }}</button>
                <button class="btn btn--fullscreen" type="submit" :disabled="!name.trim() || students.length === 0">{{ t("global.add") }}</button>
              </div>
            </template>

            <!-- EXISTING GROUP MODE (only when classId is set) -->
            <template v-else-if="mode === 'existing'">
              <div class="form__group">
                <label class="form__label" for="existing-group">{{ t("navigation.selectGroup") }}</label>
                <select
                  id="existing-group"
                  v-model="selectedGroupId"
                  class="form__input"
                >
                  <option value="">— {{ t("navigation.selectGroup") }} —</option>
                  <option v-for="g in availableGroups" :key="g.id" :value="g.id">{{ g.name }}</option>
                </select>
                <div v-if="availableGroups.length === 0" class="popup__empty">
                  {{ t("navigation.noGroups") }}
                </div>
              </div>

              <div class="form__submit">
                <button class="btn btn--fullscreen btn--red" type="button" @click="emit('close')">{{ t("global.cancel") }}</button>
                <button class="btn btn--fullscreen" type="submit" :disabled="!selectedGroupId">{{ t("global.add") }}</button>
              </div>
            </template>

          </div>
        </div>
      </div>
    </form>
  </Transition>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue3-i18n";
import { notifyError } from "@/notify";
import * as XLSX from "xlsx";

type StudentRow = { number: string; firstName: string; lastName: string };

const props = defineProps<{ examId?: string; classId?: string }>();
const emit = defineEmits<{
  (event: "close"): void;
}>();

const router = useRouter();
const { t } = useI18n();

const name = ref("");
const inputRef = ref<HTMLInputElement | null>(null);
const fileInputRef = ref<HTMLInputElement | null>(null);
const students = ref<StudentRow[]>([]);
const expanded = ref(false);
const errorMessage = ref("");

// Tab state: only relevant when classId is provided (not examId context)
const showTabs = computed(() => !!props.classId && !props.examId);
const mode = ref<"new" | "existing">("new");

// Existing group selection
const selectedGroupId = ref("");
const allGroups = ref<{ id: string; name: string }[]>([]);
const linkedGroupIds = ref<Set<string>>(new Set());

const availableGroups = computed(() =>
  allGroups.value.filter((g) => !linkedGroupIds.value.has(g.id))
);

onMounted(async () => {
  inputRef.value?.focus();
  if (showTabs.value) {
    await Promise.all([fetchAllGroups(), fetchLinkedGroups()]);
  }
});

async function fetchAllGroups() {
  const res = await fetch("/api/groups");
  if (!res.ok) return;
  const data = await res.json();
  allGroups.value = data.map((g: any) => ({ id: String(g.id), name: String(g.name) }));
}

async function fetchLinkedGroups() {
  if (!props.classId) return;
  const res = await fetch(`/api/classes/${props.classId}/groups`);
  if (!res.ok) return;
  const data = await res.json();
  linkedGroupIds.value = new Set(data.map((g: any) => String(g.id)));
}

function triggerFileInput() {
  fileInputRef.value?.click();
}

const CSV_HEADER_MAP: Record<string, keyof StudentRow> = {
  "no de d.a.": "number",
  "numero": "number",
  "number": "number",
  "nom de l'étudiant": "lastName",
  "nom de l'etudiant": "lastName",
  "nom": "lastName",
  "lastname": "lastName",
  "prénom de l'étudiant": "firstName",
  "prenom de l'etudiant": "firstName",
  "prénom": "firstName",
  "prenom": "firstName",
  "firstname": "firstName",
};

function normalizeHeader(h: string): string {
  return h.trim().toLowerCase().replace(/\uFEFF/g, "");
}

function mapHeaders(rawHeaders: string[]): (keyof StudentRow | null)[] {
  return rawHeaders.map((h) => CSV_HEADER_MAP[normalizeHeader(h)] ?? null);
}

function parseCSV(text: string): StudentRow[] {
  const separator = text.includes(";") ? ";" : ",";
  const lines = text.trim().split(/\r?\n/);
  if (lines.length < 2) throw new Error("Empty CSV");
  const headers = lines[0].split(separator);
  const mapped = mapHeaders(headers);
  if (!mapped.includes("number") || !mapped.includes("firstName") || !mapped.includes("lastName"))
    throw new Error("Missing required columns");
  const result: StudentRow[] = [];
  for (let i = 1; i < lines.length; i++) {
    const cols = lines[i].split(separator);
    if (cols.length < headers.length) continue;
    const row: Partial<StudentRow> = {};
    for (let j = 0; j < mapped.length; j++) {
      const key = mapped[j];
      if (key) row[key] = cols[j].trim();
    }
    if (row.number && row.firstName && row.lastName) result.push(row as StudentRow);
  }
  return result;
}

function parseJSON(text: string): StudentRow[] {
  const parsed = JSON.parse(text);
  if (!Array.isArray(parsed)) throw new Error("Not an array");
  for (const item of parsed) {
    if (typeof item.number !== "string" || typeof item.firstName !== "string" || typeof item.lastName !== "string")
      throw new Error("Invalid student object");
  }
  return parsed.map((item: StudentRow) => ({
    number: item.number,
    firstName: item.firstName,
    lastName: item.lastName,
  }));
}

function parseExcel(data: ArrayBuffer): StudentRow[] {
  const workbook = XLSX.read(data, { type: "array" });
  const sheet = workbook.Sheets[workbook.SheetNames[0]];
  let rows: string[][] = XLSX.utils.sheet_to_json(sheet, { header: 1 });
  if (rows.length < 2) throw new Error("Empty spreadsheet");

  // Handle single-column files where data is delimiter-separated within cells (e.g. Omnivox exports)
  if (rows[0].length === 1 && String(rows[0][0]).includes(";")) {
    rows = rows.map((row) => String(row[0] ?? "").split(";"));
  } else if (rows[0].length === 1 && String(rows[0][0]).includes(",")) {
    rows = rows.map((row) => String(row[0] ?? "").split(","));
  }

  const headers = rows[0].map(String);
  const mapped = mapHeaders(headers);
  if (!mapped.includes("number") || !mapped.includes("firstName") || !mapped.includes("lastName"))
    throw new Error("Missing required columns");
  const result: StudentRow[] = [];
  for (let i = 1; i < rows.length; i++) {
    const cols = rows[i];
    if (!cols || cols.length < headers.length) continue;
    const row: Partial<StudentRow> = {};
    for (let j = 0; j < mapped.length; j++) {
      const key = mapped[j];
      if (key) row[key] = String(cols[j] ?? "").trim();
    }
    if (row.number && row.firstName && row.lastName) result.push(row as StudentRow);
  }
  return result;
}

function handleFileSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  const ext = file.name.split(".").pop()?.toLowerCase();
  if (ext === "xlsx" || ext === "xls") {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        students.value = parseExcel(e.target?.result as ArrayBuffer);
        expanded.value = true;
      } catch {
        notifyError(t("navigation.importStudentsFormatError"));
        students.value = [];
        expanded.value = false;
      }
    };
    reader.readAsArrayBuffer(file);
  } else {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const text = e.target?.result as string;
        students.value = ext === "csv" ? parseCSV(text) : parseJSON(text);
        expanded.value = true;
      } catch {
        notifyError(t("navigation.importStudentsFormatError"));
        students.value = [];
        expanded.value = false;
      }
    };
    reader.readAsText(file);
  }
  input.value = "";
}

async function handleSubmit() {
  errorMessage.value = "";

  // EXISTING GROUP MODE
  if (mode.value === "existing" && props.classId) {
    if (!selectedGroupId.value) return;
    const res = await fetch(`/api/classes/${props.classId}/groups`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ groupId: selectedGroupId.value }),
    });
    if (res.status === 409) {
      errorMessage.value = t("navigation.groupAlreadyLinked");
      return;
    }
    if (!res.ok) {
      errorMessage.value = t("validation.errorOccured");
      return;
    }
    emit("close");
    await router.push({
      name: "classes.groupExams",
      params: { classId: props.classId, groupId: selectedGroupId.value },
    });
    return;
  }

  // NEW GROUP MODE
  const trimmed = name.value.trim();
  if (!trimmed) return;
  if (students.value.length === 0) return;

  let res: Response;

  if (props.examId) {
    res = await fetch(`/api/exams/${props.examId}/groups`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: trimmed,
        classId: props.classId,
        students: students.value,
      }),
    });
  } else if (props.classId) {
    res = await fetch(`/api/classes/${props.classId}/groups`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: trimmed,
        students: students.value,
      }),
    });
  } else {
    res = await fetch(`/api/groups`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: trimmed,
        students: students.value,
      }),
    });
  }

  if (res.status === 409) {
    errorMessage.value = t("navigation.groupNameExists");
    return;
  }

  if (!res.ok) {
    console.error("Erreur création groupe:", res.status, await res.text());
    errorMessage.value = t("validation.errorOccured");
    return;
  }

  const created = await res.json();
  emit("close");

  if (props.classId) {
    await router.push({
      name: "classes.groupExams",
      params: { classId: props.classId, groupId: created.id },
    });
  }
}
</script>

<style scoped lang="scss">
.popup__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.btn--import {
  font-size: 0.85rem;
  padding: 4px 10px;
  cursor: pointer;
  flex-shrink: 0;
}

.popup__tabs {
  display: flex;
  gap: 0;
  border-bottom: 2px solid #e0e0e0;
  margin-bottom: 16px;
}

.popup__tab {
  background: none;
  border: none;
  padding: 8px 16px;
  cursor: pointer;
  font-size: 0.9rem;
  font-weight: 500;
  opacity: 0.5;
  border-bottom: 2px solid transparent;
  margin-bottom: -2px;
  transition: opacity 0.15s;

  &--active {
    opacity: 1;
    font-weight: 700;
    border-bottom-color: #4caf50;
    color: #4caf50;
  }

  &:hover:not(&--active) {
    opacity: 0.75;
  }
}

.popup__error {
  background: #fff3f3;
  border: 1px solid #f5c2c2;
  color: #c0392b;
  border-radius: 6px;
  padding: 8px 12px;
  font-size: 0.88rem;
  margin-bottom: 12px;
}

.popup__empty {
  font-size: 0.85rem;
  opacity: 0.6;
  margin-top: 8px;
}

.students-accordion {
  margin: 12px 0;

  &__toggle {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    background: none;
    border: 1px solid #e0e0e0;
    border-radius: 6px;
    padding: 8px 12px;
    cursor: pointer;
    font-size: 0.9rem;
    font-weight: 500;
    color: #4caf50;

    &:hover {
      background-color: #f5f5f5;
    }
  }

  &__chevron {
    font-size: 0.7rem;
    transition: transform 0.2s ease;

    &--open {
      transform: rotate(180deg);
    }
  }

  &__body {
    border: 1px solid #e0e0e0;
    border-top: none;
    border-radius: 0 0 6px 6px;
    max-height: 200px;
    overflow-y: auto;
  }
}

.students-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;

  th, td {
    padding: 6px 10px;
    text-align: left;
  }

  th {
    background-color: #f9f9f9;
    font-weight: 600;
    position: sticky;
    top: 0;
  }

  tbody tr:not(:last-child) {
    border-bottom: 1px solid #f0f0f0;
  }

  tbody tr:hover {
    background-color: #fafafa;
  }
}

.fade-leave-active,
.fade-enter-active {
  transition: opacity 0.2s cubic-bezier(0.69, 0.33, 0.16, 0.97);
}

.fade-enter-to,
.fade-leave-from {
  opacity: 1;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
